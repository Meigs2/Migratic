using System;
using Microsoft.Extensions.Logging;

namespace Migratic.Core;

public class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) { return true; }
    public IDisposable BeginScope<TState>(TState state) { return new NoopDisposable(); }
}

public class NoopDisposable : IDisposable
{
    public void Dispose() { }
}