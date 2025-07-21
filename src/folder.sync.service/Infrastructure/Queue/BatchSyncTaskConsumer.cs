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

                // Execute all tasks concurrently
                var executionTasks = buffer.Select(task => ExecuteWithRetryAsync(task, cancellationToken));
                await Task.WhenAll(executionTasks);

                sw.Stop();
                _logger.LogInformation(
                    "Batch processed {Count} in {ElapsedMs}ms. Success={Success} Failed={Fail}",
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

    private async Task ExecuteWithRetryAsync(SyncTask task, CancellationToken cancellationToken)
    {
        const int delayMs = 200;

        for (var attempt = 1; attempt <= AppConstants.MaxAttempts; attempt++)
        {
            if (attempt < AppConstants.MaxAttempts)
            {
                _logger.LogInformation("[Retry]  Starting attempt {Attempt}/{MaxAttempts} for {Path}", attempt, AppConstants.MaxAttempts, task.Entry.Path);
            }
            else
            {
                _logger.LogError("[Retry] Last attempt reached {MaxAttempts} for {Path}", AppConstants.MaxAttempts, task.Entry.Path);
            }

            var command = _syncCommandFactory.CreateFor(task, _replicaPath);

            try
            {
                await _executor.ExecuteAsync(command, cancellationToken);
            }
            catch
            {
                await Task.Delay(delayMs);
            }

            _batchState.FlushFailures(task);
        }

        _logger.LogInformation(
            "[Retry] Batch summary: Success={SuccessCount}, Failed={FailedCount}, Retried={RetriedCount}",
            _batchState.GetSuccessCount(),
            _batchState.GetFailureCount(),
            _batchState.GetRetryCount());
    }
}