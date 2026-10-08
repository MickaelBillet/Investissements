using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenAI.Responses;

namespace AgentAI;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

/// <summary>
/// Creates <see cref="AIAgent"/> instances backed by Azure AI Foundry, one configuration per <see cref="AgentKind"/>.
/// </summary>
public sealed class AgentFactory : IAgentFactory
{
    #region Fields
    private const int NewsLookbackDays = 7;

    private readonly INewsService _newsService;
    private readonly IWeatherService _weatherService;
    private readonly AIProjectClient _projectClient;
    private readonly string _model;
    private readonly InvestZaptoMcpClient _investZaptoConnector;
    private readonly AgentAIOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private McpClient? _investZaptoMcpClient;
    #endregion

    #region Constructor

    public AgentFactory(AgentAIOptions options, InvestZaptoMcpClient investZaptoConnector, INewsService newsService, ILoggerFactory loggerFactory)
        : this(options, investZaptoConnector, newsService, new WeatherService(), loggerFactory)
    {
    }

    public AgentFactory(AgentAIOptions options, InvestZaptoMcpClient investZaptoConnector, INewsService newsService, IWeatherService weatherService, ILoggerFactory loggerFactory)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
        this._investZaptoConnector = investZaptoConnector ?? throw new ArgumentNullException(nameof(investZaptoConnector));
        this._newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
        this._weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        this._model = options.Model;
        this._projectClient = new AIProjectClient(new Uri(options.FoundryProjectEndpoint), new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));
    }

    #endregion

    #region Methods

    public async Task<(AIAgent Agent, ICustomChatHistoryProvider HistoryProvider)> CreateAsync(AgentKind kind, string? contextInput = null, string? optionalInput = null)
    {
        var definition = await this.BuildAgentDefinitionAsync(kind, contextInput, optionalInput).ConfigureAwait(false);

        Directory.CreateDirectory(this._options.HistoryDirectory);
        var chatHistoryFilePath = Path.Combine(this._options.HistoryDirectory, definition.HistoryFileName);
        var historyProvider = new CustomChatHistoryProvider(chatHistoryFilePath, this._loggerFactory.CreateLogger<CustomChatHistoryProvider>());

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
            // Disable server-side response storage: the Foundry Responses API stores output and returns a
            // conversation id by default, which conflicts with using a client-side ChatHistoryProvider.
            clientFactory: chatClient => chatClient
                .AsBuilder()
                .ConfigureOptions(options =>
                {
                    var previousFactory = options.RawRepresentationFactory;
                    options.RawRepresentationFactory = client =>
                    {
                        var responseOptions = previousFactory?.Invoke(client) as CreateResponseOptions ?? new CreateResponseOptions();
                        responseOptions.StoredOutputEnabled = false;
                        return responseOptions;
                    };
                })
                .Build());

        return (agent, historyProvider);
    }

    public async ValueTask DisposeAsync()
    {
        if (this._investZaptoMcpClient is not null)
        {
            await this._investZaptoMcpClient.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds the <see cref="AgentDefinition"/> for exactly the requested <paramref name="kind"/>, and
    /// nothing else. Every kind goes through this same async path for consistency, even though only
    /// <see cref="AgentKind.Portfolio"/> actually needs to await anything (connecting to the InvestZapto
    /// MCP server) — this keeps <see cref="CreateAsync"/> from ever building agents nobody asked for.
    /// </summary>
    /// <param name="kind">Which agent configuration to build.</param>
    /// <param name="contextInput">
    /// The single piece of user-provided data the agent's context provider needs (a city for
    /// <see cref="AgentKind.Weather"/>, a stock name for <see cref="AgentKind.Stock"/>); required for
    /// those two kinds (and the company for <see cref="AgentKind.News"/>), ignored otherwise.
    /// </param>
    /// <param name="optionalInput">The ticker for <see cref="AgentKind.News"/>; ignored otherwise.</param>
    private async Task<AgentDefinition> BuildAgentDefinitionAsync(AgentKind kind, string? contextInput, string? optionalInput)
    {
        return kind switch
        {
            AgentKind.Chat => new AgentDefinition(
                Instructions: "You are a helpful assistant.",
                Tools: null,
                HistoryFileName: "chat-history-chat.json"),

            AgentKind.Weather => new AgentDefinition(
                Instructions: "You are a weather assistant. Use the GetWeather tool to answer questions about weather.",
                Tools: new[] { AIFunctionFactory.Create(this._weatherService.GetWeather) },
                HistoryFileName: "chat-history-weather.json",
                AIContextProviders: new CompositeContextProvider(new AIContextProvider[]
                {
                    new WeatherContextProvider(RequireContextInput(contextInput, "une ville")),
                })),

            AgentKind.Stock => new AgentDefinition(
                Instructions: InstructionLoader.Load("AnalyseAction.md"),
                Tools: null,
                HistoryFileName: "chat-history-stock.json",
                AIContextProviders: new CompositeContextProvider(new AIContextProvider[]
                {
                    new DateContextProvider(TimeProvider.System),
                    new StockContextProvider(RequireContextInput(contextInput, "un nom d'action")),
                })),

            AgentKind.Portfolio => await this.BuildPortfolioDefinitionAsync().ConfigureAwait(false),

            AgentKind.News => new AgentDefinition(
                Instructions: InstructionLoader.Load("ActualitesSociete.md"),
                Tools: new[] { this.CreateNewsTool() },
                HistoryFileName: "chat-history-news.json",
                AIContextProviders: new CompositeContextProvider(new AIContextProvider[]
                {
                    new NewsContextProvider(RequireContextInput(contextInput, "un nom de société"), optionalInput, NewsLookbackDays, TimeProvider.System),
                })),

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown agent kind."),
        };
    }

    private string RequireContextInput(string? contextInput, string expected)
    {
        return string.IsNullOrWhiteSpace(contextInput)
            ? throw new ArgumentException($"Cet agent nécessite {expected}.", nameof(contextInput))
            : contextInput;
    }

    private AITool CreateNewsTool()
    {
        // The look-back window is fixed here rather than exposed to the model, so the agent cannot
        // silently widen it beyond what the context provider announces.
        return AIFunctionFactory.Create(
            async (
                [Description("Nom de la société à rechercher.")] string company,
                [Description("Ticker de la société, ou vide s'il n'est pas connu.")] string? ticker,
                CancellationToken cancellationToken) =>
                    await this._newsService.GetLatestNewsAsync(company, ticker, NewsLookbackDays, cancellationToken).ConfigureAwait(false),
            name: "GetLatestNews",
            description: "Récupère les actualités publiées sur une société durant la dernière semaine (titre, source, date, lien), de la plus récente à la plus ancienne.");
    }

    private async Task<AgentDefinition> BuildPortfolioDefinitionAsync()
    {
        var endpoint = this._options.InvestZaptoMcpUrl
            ?? throw new InvalidOperationException($"{nameof(AgentAIOptions.InvestZaptoMcpUrl)} is not set.");

        this._investZaptoMcpClient ??= await this._investZaptoConnector.ConnectAsync(endpoint).ConfigureAwait(false);
        var mcpTools = await this._investZaptoMcpClient.ListToolsAsync().ConfigureAwait(false);

        return new AgentDefinition(
            Instructions: InstructionLoader.Load("PortfolioAllocation.md"),
            Tools: mcpTools.Cast<AITool>().ToList(),
            HistoryFileName: "chat-history-portfolio.json");
    }

    #endregion
}
