using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MyApi;

internal class LongTaskManager
{
    private readonly Channel<Func<Task>> _taskChannel;
    private readonly ConcurrentDictionary<Guid, InMemoryLogger<Worker>> _taskLogs = new();

    public LongTaskManager()
    {
        _taskChannel = Channel.CreateBounded<Func<Task>>(10);
        
        _ = Task.Run(async () =>
        {
            await foreach (var workItem in _taskChannel.Reader.ReadAllAsync())
            {
                try
                {
                    await workItem();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        });
    }

    public Guid CreateTask()
    {
        var guid = Guid.NewGuid();
        var logger = new InMemoryLogger<Worker>();
        var cancellationTokenSource = new CancellationTokenSource(2000);
        var workItem = new Func<Task>(async () => {
            try 
            {
                await new Worker(logger).Run(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                throw;
            }
            finally 
            {
                logger.Completed();
            }
        });

        _taskLogs[guid] = logger;
        _taskChannel.Writer.TryWrite(workItem);
        return guid;
    }

    public async Task<TaskOutput> TaskOutput(Guid guid)
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
