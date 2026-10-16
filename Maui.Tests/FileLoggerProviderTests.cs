using InvestissementsDashboard.Maui.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Tests;

public sealed class FileLoggerProviderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "file-logger-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Log_Message_AppendsLineWithLevelAndCategory()
    {
        var path = Path.Combine(_directory, "logs", "app.log");
        using var provider = new FileLoggerProvider(path);

        provider.CreateLogger("Cat").LogInformation("hello {Name}", "world");

        var content = File.ReadAllText(path);
        Assert.Contains("Information", content);
        Assert.Contains("Cat: hello world", content);
    }

    [Fact]
    public void Log_WithException_AppendsExceptionDetails()
    {
        var path = Path.Combine(_directory, "app.log");
        using var provider = new FileLoggerProvider(path);

        provider.CreateLogger("Cat").LogError(new InvalidOperationException("boom"), "failed");

        Assert.Contains("InvalidOperationException: boom", File.ReadAllText(path));
    }

    [Fact]
    public void Constructor_OversizedFile_StartsOver()
    {
        var path = Path.Combine(_directory, "app.log");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, new string('x', 1_100_000));

        using var provider = new FileLoggerProvider(path);
        provider.CreateLogger("Cat").LogInformation("fresh");

        Assert.True(new FileInfo(path).Length < 1000);
    }

    [Fact]
    public void Log_WhenFileCannotBeWritten_DoesNotThrow()
    {
        Directory.CreateDirectory(_directory);
        // A directory in place of the file makes every write fail.
        var path = Path.Combine(_directory, "blocked");
        Directory.CreateDirectory(path);
        using var provider = new FileLoggerProvider(path);

        var exception = Record.Exception(() => provider.CreateLogger("Cat").LogInformation("ignored"));

        Assert.Null(exception);
    }
}
