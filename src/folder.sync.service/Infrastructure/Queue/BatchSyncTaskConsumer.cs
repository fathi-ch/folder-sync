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
                {
                    buffer.Add(task);
                }

                // Wait only if we haven't filled the batch
                if (buffer.Count < BatchSize)
                {
                    await flushTimer.WaitForNextTickAsync(cancellationToken);
                }

                if (buffer.Count == 0)
                    continue;

                var sw = Stopwatch.StartNew();

                // Execute all tasks concurrently
                var executionTasks = buffer.Select(task => ExecuteWithRetryAsync(task, cancellationToken));
                await Task.WhenAll(executionTasks);

                sw.Stop();
                _logger.LogInformation(
                    "Batch processed {Count} in {ElapsedMs}ms. Success={Success} Failed={Fail}",
                    buffer.Count, sw.ElapsedMilliseconds, _batchState.SuccessCount, _batchState.FailedCount);

                buffer.Clear();
            }
        }
        catch (ChannelClosedException) { /* graceful exit */ }
    }

    private async Task ExecuteWithRetryAsync(SyncTask task, CancellationToken cancellationToken)
    {
        const int maxRetries = 2;
        const int delayMs = 500;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var command = _syncCommandFactory.CreateFor(task, _replicaPath);

            try
            {
                await _executor.ExecuteAsync(command, cancellationToken);
                _batchState.MarkSuccess(task);
                return;
            }
            catch (IOException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("File locked. Retrying in {Delay}ms: {Path}", delayMs, task.Entry.Path);
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                _batchState.MarkFailure(task, ex);
                _logger.LogWarning(ex, "Sync task failed permanently: {Path}", task.Entry.Path);
                return;
            }
        }
    }

}