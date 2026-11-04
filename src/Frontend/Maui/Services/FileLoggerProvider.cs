using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Appends log lines to a text file. A packaged or windowed app has no console, so this is the only way to see
/// what an agent was doing when it stalled. Logging must never break the app: write failures are only traced.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const long MaxBytes = 1_000_000;

    private readonly string _path;
    private readonly object _gate = new();

    public FileLoggerProvider(string path)
    {
        _path = path;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Bounded growth: start over rather than rotate, the file is a diagnostic aid, not an audit trail.
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"File logger could not prepare '{path}': {ex.Message}");
        }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

    public void Dispose()
    {
    }

    private void Append(string line)
    {
        lock (_gate)
        {
            try
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"File logger could not write to '{_path}': {ex.Message}");
            }
        }
    }

    private sealed class FileLogger(string category, FileLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTime.Now:HH:mm:ss.fff} {logLevel,-11} {category}: {formatter(state, exception)}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            owner.Append(line);
        }
    }
}
