using System.Text.Json;
using AgentAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public sealed class CustomChatHistoryProviderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"history-tests-{Guid.NewGuid():N}");
    private readonly string _filePath;

    public CustomChatHistoryProviderTests()
    {
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "chat-history.json");

        var previousRun = new List<ChatMessage> { new(ChatRole.User, "question"), new(ChatRole.Assistant, "réponse") };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(previousRun, AIJsonUtilities.DefaultOptions));
    }

    private CustomChatHistoryProvider Create(bool loadExisting) =>
        new(_filePath, NullLogger<CustomChatHistoryProvider>.Instance, loadExisting);

    [Fact]
    public void Constructor_LoadExistingTrue_LoadsFile()
    {
        Assert.Equal(2, Create(loadExisting: true).GetMessages().Count);
    }

    [Fact]
    public void Constructor_LoadExistingFalse_StartsEmpty()
    {
        Assert.Empty(Create(loadExisting: false).GetMessages());
    }

    [Fact]
    public void Constructor_LoadExistingFalse_LeavesFileAndCreatesNoBackup()
    {
        Create(loadExisting: false);

        Assert.True(File.Exists(_filePath));
        Assert.Single(Directory.GetFiles(_directory));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
