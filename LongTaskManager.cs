using System.Collections.Concurrent;

namespace MyApi;

internal class LongTaskManager
{
    private readonly ConcurrentDictionary<Guid, InMemoryLogger<Worker>> _taskLogs = new();

    public async Task RunTask(Guid guid, CancellationToken token)
    {
        var logger = _taskLogs[guid];
        await new Worker(logger).Run(token);
        logger.Completed();
    }

    public Guid CreateTask() 
    {
        var guid = Guid.NewGuid();
        var logger = new InMemoryLogger<Worker>();
        _taskLogs[guid] = logger;
        return guid;
    }

    public TaskOutput TaskOutput(Guid guid)
    {
        var logger = _taskLogs[guid];
        var output = logger.FlushLogs();
        if (logger.IsCompleted)
        {
            _taskLogs.TryRemove(guid, out var _);
        }
        return new TaskOutput(output, logger.IsCompleted);
    }
}

internal record TaskOutput(IReadOnlyCollection<string> Logs, bool IsCompleted);

internal class Worker
{
    private readonly ILogger logger;

    public Worker(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task Run(CancellationToken token)
    {
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(1000, token).ConfigureAwait(false);
            logger.LogInformation("Doing work");
        }
    }
}

internal class InMemoryLogger<T> : ILogger<T>
{
    private readonly List<string> _logEntries = new List<string>();
    private readonly object _lock = new object();
    private readonly LogLevel _logLevel;
    internal bool IsCompleted { get; private set; } = false;

    public InMemoryLogger(LogLevel logLevel = LogLevel.Information)
    {
        _logLevel = logLevel;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var logMessage = formatter(state, exception);
            if (!string.IsNullOrEmpty(logMessage))
            {
                lock (_lock)
                {
                    _logEntries.Add($"[{logLevel}] {logMessage}");
                }
            }
        }
    }

    public void Completed()
    {
        IsCompleted = true;
    }

    public IReadOnlyList<string> FlushLogs()
    {
        lock (_lock)
        {
            var logList = _logEntries.ToList();
            _logEntries.Clear();
            return logList;
        }
    }
}
