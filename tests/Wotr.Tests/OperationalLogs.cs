using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Wotr.Tests;

internal sealed class OperationalLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> messages = new();
    public string Text => string.Join('\n', messages);
    public ILogger CreateLogger(string categoryName) => new Capture(messages);
    public void Dispose() { }

    private sealed class Capture(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception)
                + (exception is null ? "" : "\n" + exception));
    }
}
