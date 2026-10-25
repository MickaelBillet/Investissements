using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace AgentAI;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

/// <summary>
/// Creates <see cref="AIAgent"/> instances backed by Azure AI Foundry, one configuration per <see cref="AgentKind"/>.
/// </summary>
public sealed class AgentFactory : IAgentFactory
{
    #region Fields
    private readonly IReadOnlyDictionary<AgentKind, IAgentDefinitionBuilder> _builders;
    private readonly AIProjectClient _projectClient;
    private readonly string _model;
    private readonly InvestZaptoMcpToolSource _investZaptoTools;
    private readonly AgentAIOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    #endregion

    #region Constructor

    /// <param name="mcpTools">Owned by the factory: released by <see cref="DisposeAsync"/>.</param>
    /// <param name="builders">One per <see cref="AgentKind"/>; built by <see cref="AgentFactoryBuilder"/>.</param>
    internal AgentFactory(AgentAIOptions options, AIProjectClient projectClient, InvestZaptoMcpToolSource mcpTools, IEnumerable<IAgentDefinitionBuilder> builders, ILoggerFactory loggerFactory)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
        this._projectClient = projectClient ?? throw new ArgumentNullException(nameof(projectClient));
        this._investZaptoTools = mcpTools ?? throw new ArgumentNullException(nameof(mcpTools));
        this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        ArgumentNullException.ThrowIfNull(builders);

        // ToDictionary throws on a duplicate kind: two builders for one agent is a wiring mistake, not a choice.
        this._builders = builders.ToDictionary(b => b.Kind);
        this._model = options.Model;
    }

    #endregion

    #region Methods

    public async Task<(AIAgent Agent, ICustomChatHistoryProvider HistoryProvider)> CreateAsync(AgentKind kind, string? contextInput = null, string? optionalInput = null)
    {
        // Only the requested builder runs: the others never connect to the MCP server or validate inputs they do not need.
        var builder = this._builders.GetValueOrDefault(kind)
            ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown agent kind.");
        var definition = await builder.BuildAsync(contextInput, optionalInput).ConfigureAwait(false);

        var historyProvider = this.CreateHistoryProvider(definition);

        var agent = this._projectClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    ModelId = this._model,
                    Instructions = definition.Instructions,
                    Tools = definition.Tools,
                },
                ChatHistoryProvider = historyProvider,
                AIContextProviders = definition.AIContextProviders is null
                    ? null
                    : [definition.AIContextProviders],
            },
            clientFactory: chatClient => this.ConfigureChatClient(chatClient, kind, definition));

        return (agent, historyProvider);
    }

    public ValueTask DisposeAsync() => this._investZaptoTools.DisposeAsync();

    private CustomChatHistoryProvider CreateHistoryProvider(AgentDefinition definition)
    {
        Directory.CreateDirectory(this._options.HistoryDirectory);
        var chatHistoryFilePath = Path.Combine(this._options.HistoryDirectory, definition.HistoryFileName);
        return new CustomChatHistoryProvider(chatHistoryFilePath, this._loggerFactory.CreateLogger<CustomChatHistoryProvider>(), definition.KeepHistory);
    }

    private IChatClient ConfigureChatClient(IChatClient chatClient, AgentKind kind, AgentDefinition definition)
    {
        return chatClient
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                var previousFactory = options.RawRepresentationFactory;
                options.RawRepresentationFactory = client =>
                {
                    var responseOptions = previousFactory?.Invoke(client) as CreateResponseOptions ?? new CreateResponseOptions();
                    // Disable server-side response storage: the Foundry Responses API stores output and returns a
                    // conversation id by default, which conflicts with using a client-side ChatHistoryProvider.
                    responseOptions.StoredOutputEnabled = false;
                    if (definition.ReasoningEffort is { } effort)
                    {
                        responseOptions.ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = effort };
                    }

                    return responseOptions;
                };
            })
            .Use(inner => new ModelCallLoggingChatClient(inner, kind, this._loggerFactory.CreateLogger<ModelCallLoggingChatClient>()))
            .Build();
    }

    #endregion
}
