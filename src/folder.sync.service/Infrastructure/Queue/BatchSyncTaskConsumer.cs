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
    private readonly string _replicaPath;
    private Task? _worker;
    
    // Configurable
    private const int BatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(50);

    public BatchSyncTaskConsumer(ISyncCommandFactory syncCommandFactory,
        FolderSyncServiceConfig config, ILogger<BatchSyncTaskConsumer> logger, ICommandExecutor executor)
    {
        _syncCommandFactory = syncCommandFactory;
        _logger = logger;
        _executor = executor;
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
                if (buffer.Count >= BatchSize || (buffer.Count > 0 && await flushTimer.WaitForNextTickAsync(cancellationToken)))
                {
                    var sw = Stopwatch.StartNew();
                    var tasks = buffer.Select(syncTask =>
                    {
                        var command = _syncCommandFactory.CreateFor(syncTask, _replicaPath);
                        return _executor.ExecuteAsync(command, cancellationToken);
                    });
                    await Task.WhenAll(tasks);
                    sw.Stop();
                    
                    _logger.LogInformation("Batch processed {Count} items in {ElapsedMs}ms", buffer.Count, sw.ElapsedMilliseconds);
                    
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