using System.Diagnostics;
using System.Threading.Channels;
using folder.sync.service.Common;
using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure.Commanding;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public class BatchSyncTaskConsumer : ISyncTaskConsumer
{
    private readonly ISyncCommandFactory _syncCommandFactory;
    private readonly ILogger<BatchSyncTaskConsumer> _logger;
    private readonly ICommandExecutor _executor;
    private readonly IBatchState _batchState;
    private readonly string _replicaPath;
    private Task? _worker;

    // Configurable
    private const int BatchSize = AppConstants.MaxBatchSize;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(50);

    public BatchSyncTaskConsumer(ISyncCommandFactory syncCommandFactory,
        FolderSyncServiceConfig config, ILogger<BatchSyncTaskConsumer> logger, ICommandExecutor executor,
        IBatchState batchState)
    {
        _syncCommandFactory = syncCommandFactory;
        _logger = logger;
        _executor = executor;
        _batchState = batchState;
        _replicaPath = config.ReplicaPath;
    }

    public Task StartAsync(Channel<SyncTask> reader, CancellationToken cancellationToken)
    {
        _worker ??= Task.Run(() => ConsumeBatchesAsync(reader, cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeBatchesAsync(Channel<SyncTask> reader, CancellationToken cancellationToken)
    {
        var buffer = new List<SyncTask>();
        using var flushTimer = new PeriodicTimer(FlushInterval);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Collect items until BatchSize or timer elapses
                while (buffer.Count < BatchSize &&
                       reader.Reader.TryRead(out var task))
                    buffer.Add(task);

                // Wait only if we haven't filled the batch
                if (buffer.Count < BatchSize) await flushTimer.WaitForNextTickAsync(cancellationToken);

                if (buffer.Count == 0)
                    continue;

                var sw = Stopwatch.StartNew();
                await ExecuteBatchWithRetriesAsync(buffer, cancellationToken);
                sw.Stop();
                
                _logger.LogInformation("Batch processed {Count} in {ElapsedMs}ms. Success={Success} Failed={Fail}",
                    buffer.Count, sw.ElapsedMilliseconds,
                    _batchState.GetSuccessCount(), _batchState.GetFailureCount());

                buffer.Clear();
            }
        }
        catch (ChannelClosedException)
        {
            /* graceful exit */
        }
    }
    
    private async Task ExecuteTaskAsync(SyncTask task, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Sync] Executing task for {Path}", task.Entry.Path);
        var command = _syncCommandFactory.CreateFor(task, _replicaPath);

        try
        {
            await _executor.ExecuteAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Sync] Initial execution failed for {Path}", task.Entry.Path);
        }
    }

    private async Task ExecuteBatchWithRetriesAsync(List<SyncTask> tasks, CancellationToken cancellationToken)
    {
        const int maxAttempts = AppConstants.MaxAttempts;
        const int delayMs = 200;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var toRun = (attempt == 1)
                ? tasks
                : _batchState.GetFailedTasks().ToList();

            if (toRun.Count == 0)
                return;

            _logger.LogInformation("[Retry] Attempt {Attempt}/{Max} for {Count} failed tasks", attempt, maxAttempts, toRun.Count);

            var executions = toRun.Select(task => ExecuteTaskAsync(task, cancellationToken));
            await Task.WhenAll(executions);

            if (attempt < maxAttempts)
                await Task.Delay(delayMs, cancellationToken);
        }
    }



    // private async Task ExecuteWithRetryAsync(SyncTask task, CancellationToken cancellationToken)
    // {
    //     int maxAttempts = AppConstants.MaxAttempts;
    //     const int delayMs = 200;
    //
    //     var command = _syncCommandFactory.CreateFor(task, _replicaPath);
    //     // try
    //     // {
    //         _logger.LogInformation("[Retry] Starting attempt {MaxAttempts} for {Path}" , maxAttempts, task.Entry.Path);
    //         await _executor.ExecuteAsync(command, cancellationToken);
    //         _batchState.FlushFailures(task);
    //     // }
    //     // catch (Exception ex)
    //     // {
    //     //     _logger.LogWarning(ex, "[Retry] First attempt failed for {Path}, initiating retries...", task.Entry.Path);
    //     //     _batchState.MarkFailure(task, ex); // Optional for diagnostics
    //     // }
    //
    //     // if (_batchState.GetFailureCount() > 0)
    //     // {
    //     //     for (int attempt = 2; attempt <= maxAttempts; attempt++)
    //     //     {
    //     //         _logger.LogInformation("[Retry] Starting attempt {Attempt}/{MaxAttempts} for {Path}", attempt, maxAttempts, task.Entry.Path);
    //     //         command = _syncCommandFactory.CreateFor(task, _replicaPath);
    //     //
    //     //         try
    //     //         {
    //     //             await _executor.ExecuteAsync(command, cancellationToken);
    //     //             _batchState.FlushFailures(task); // ✅ success clears failures
    //     //             return;
    //     //         }
    //     //         catch (Exception ex)
    //     //         {
    //     //             _batchState.MarkFailure(task, ex);
    //     //
    //     //             if (attempt == maxAttempts)
    //     //                 _logger.LogError(ex, "[Retry] Final attempt {MaxAttempts} failed for {Path}", maxAttempts, task.Entry.Path);
    //     //             else
    //     //                 await Task.Delay(delayMs);
    //     //         }
    //     //     }
    //     // }
    //
    //     _batchState.FlushFailures(task);
    // }
}