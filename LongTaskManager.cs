using System.Collections.Concurrent;

namespace MyApi;

internal class LongTaskManager
{
    private ConcurrentDictionary<Guid, (InMemoryLogger<Worker> Logger, Task Task)> Tasks = new ConcurrentDictionary<Guid, (InMemoryLogger<Worker>, Task)>();

    public Guid CreateTask()
    {
        var guid = Guid.NewGuid();
        var logger = new InMemoryLogger<Worker>();
        var task = Task.Run(() => new Worker(logger).Run());
        Tasks.TryAdd(guid, (logger, task));
        return guid;
    }

    public async Task<TaskOutput> TaskOutput(Guid guid)
    {
        var (logger, task) = Tasks[guid];
        var output = logger.FlushLogs();
        if (task.IsCompleted)
        {
            Tasks.TryRemove(guid, out var _); 
            await task;
        }
        return new TaskOutput(output, task.IsCompleted);
    }
}

internal record TaskOutput(IReadOnlyCollection<string> Logs, bool IsCompleted);

internal class Worker 
{
    private readonly ILogger<Worker> logger;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    public async Task Run() 
    {
        for(var i = 0; i < 3; i++)
        {
            await Task.Delay(1 * 1000);
            logger.LogInformation("Doing work");
        }
    }
}

internal class InMemoryLogger<T> : ILogger<T>
{
    private readonly List<string> _logEntries = [];
    private readonly object _lock = new object(); 
    
    private readonly LogLevel _logLevel;

    public InMemoryLogger(LogLevel logLevel = LogLevel.Information)
    {
        _logLevel = logLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _logLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var logMessage = formatter(state, exception);
            if (!string.IsNullOrEmpty(logMessage))
            {
                lock(_lock)
                {
                    _logEntries.Add($"[{logLevel}] {logMessage}");
                }
            }
        }
    }

    public IReadOnlyList<string> FlushLogs()
    {
        lock(_lock)
        {
            var logList = _logEntries.ToList();
            _logEntries.Clear();
            return logList;
        }
    }
}