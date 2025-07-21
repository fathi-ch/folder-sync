using System.Diagnostics;
using System.Diagnostics.Metrics;
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

    //Tracing and Metrics
    private static readonly ActivitySource ActivitySource = new("FolderSync.BatchConsumer");
    private static readonly Meter Meter = new("FolderSync.Metrics", "1.0.0");

    private static readonly Counter<long> TasksProcessed = Meter.CreateCounter<long>("sync_tasks_processed");
    private static readonly Counter<long> TasksFailed = Meter.CreateCounter<long>("sync_tasks_failed");
    private static readonly Histogram<long> BatchDuration = Meter.CreateHistogram<long>("sync_batch_duration_ms");

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

                using (var activity = ActivitySource.StartActivity("ConsumeBatch", ActivityKind.Internal))
                {
                    var sw = Stopwatch.StartNew();
                    await ExecuteBatchWithRetriesAsync(buffer, cancellationToken, AppConstants.IsRetryEnabled);
                    sw.Stop();

                    var success = _batchState.GetSuccessCount();
                    var failure = _batchState.GetFailureCount();
                    TasksProcessed.Add(success);
                    TasksFailed.Add(failure);
                    BatchDuration.Record(sw.ElapsedMilliseconds);

                    if (AppConstants.IsMetricsEnabled)
                        _logger.LogInformation(
                            "[Metrics] Tasks={Total} Success={Success} Failed={Failed} Duration={Elapsed}ms",
                            buffer.Count, success, failure, sw.ElapsedMilliseconds);

                    if (AppConstants.IsTracingEnabled)
                        _logger.LogDebug(
                            "[Trace] TraceId={TraceId} SpanId={SpanId} DisplayName={Name} Kind={Kind} Duration={Duration} Start={StartTime} Service={ServiceName} Instance={InstanceId}",
                            activity.TraceId,
                            activity.SpanId,
                            activity.DisplayName,
                            activity.Kind,
                            activity.Duration,
                            activity.StartTimeUtc,
                            activity.GetTagItem("service.name"),
                            activity.GetTagItem("service.instance.id"));

                    _logger.LogInformation("Batch processed {Count} in {ElapsedMs}ms. Success={Success} Failed={Fail}",
                        buffer.Count, sw.ElapsedMilliseconds,
                        _batchState.GetSuccessCount(), _batchState.GetFailureCount());
                }

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

    private async Task ExecuteBatchWithRetriesAsync(List<SyncTask> tasks, CancellationToken cancellationToken,
        bool enableRetry)
    {
        const int maxAttempts = AppConstants.MaxBatchSize;
        const int delayMs = 200;

        _logger.LogInformation("[Sync] Executing initial batch with {Count} tasks", tasks.Count);
        var initialExecutions = tasks.Select(task => ExecuteTaskAsync(task, cancellationToken));
        await Task.WhenAll(initialExecutions);

        if (!enableRetry || _batchState.GetFailureCount() == 0)
            return;

        for (var attempt = 2; attempt <= maxAttempts; attempt++)
        {
            var toRetry = _batchState.GetFailedTasks().ToList();
            if (toRetry.Count == 0)
                return;

            _logger.LogWarning("[Retry] Attempt {Attempt}/{Max} for {Count} failed tasks", attempt, maxAttempts,
                toRetry.Count);
            var retryExecutions = toRetry.Select(task => ExecuteTaskAsync(task, cancellationToken));
            await Task.WhenAll(retryExecutions);

            if (attempt < maxAttempts)
                await Task.Delay(delayMs, cancellationToken);
        }
    }
}