using System.Diagnostics;
using System.Threading.Channels;
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
    private const int BatchSize = 50;
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
        var flushTimer = new PeriodicTimer(FlushInterval);


        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read all available items or until batch size is reached
                while (reader.Reader.TryRead(out var task))
                {
                    buffer.Add(task);

                    if (buffer.Count >= BatchSize)
                        break;
                }


                // If buffer has items and either the batch size is met or flush interval elapsed
                if (buffer.Count >= BatchSize ||
                    (buffer.Count > 0 && await flushTimer.WaitForNextTickAsync(cancellationToken)))
                {
                    var sw = Stopwatch.StartNew();


                    var tasks = buffer.Select(async syncTask =>
                    {
                        var command = _syncCommandFactory.CreateFor(syncTask, _replicaPath);
                        try
                        {
                            await _executor.ExecuteAsync(command, cancellationToken);
                            _batchState.MarkSuccess(syncTask);
                        }
                        catch (Exception ex)
                        {
                            _batchState.MarkFailure(syncTask, ex);
                            _logger.LogWarning(ex, "Sync task failed for {Path}", syncTask.Entry.Path);
                        }
                    });

                    await Task.WhenAll(tasks);
                    sw.Stop();

                    var retryCount = 0;
                    const int maxRetries = 2;
                    while (_batchState.FailedCount > 0 && retryCount < maxRetries)
                    {
                        retryCount++;
                        var retryTasks = _batchState.GetFailedTasks().Select(async failedTask =>
                        {
                            var retryCommand = _syncCommandFactory.CreateFor(failedTask, _replicaPath);
                            try
                            {
                                await _executor.ExecuteAsync(retryCommand, cancellationToken);
                                _batchState.MarkSuccess(failedTask);
                            }
                            catch (Exception ex)
                            {
                                // Do not remove from failure list; keep it for logging or metrics
                            }
                        });

                        await Task.WhenAll(retryTasks);
                    }

                    _logger.LogInformation(
                        "Batch processed {Count} items in {ElapsedMs}ms. Success: {Ok}, Failed: {Fail}",
                        buffer.Count, sw.ElapsedMilliseconds, _batchState.SuccessCount, _batchState.FailedCount);

                    buffer.Clear();
                }
            }
        }
        catch (ChannelClosedException)
        {
            // gracefully stop
        }
        finally
        {
            flushTimer.Dispose();
        }
    }
}