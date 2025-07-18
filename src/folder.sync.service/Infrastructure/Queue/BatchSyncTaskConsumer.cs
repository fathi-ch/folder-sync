using System.Threading.Channels;
using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure.Commanding;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public class BatchSyncTaskConsumer : ISyncTaskConsumer
{
    private readonly ISyncCommandFactory _syncCommandFactory;
    private readonly string _replicaPath;
    private Task? _worker;

    // Configurable
    private const int BatchSize = 10;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(200);

    public BatchSyncTaskConsumer(ISyncCommandFactory syncCommandFactory,
        FolderSyncServiceConfig config)
    {
        _syncCommandFactory = syncCommandFactory;
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
                while (reader.Reader.TryRead(out var task))
                {
                    buffer.Add(task);

                    if (buffer.Count >= BatchSize)
                        break;
                }

                // Flush either on interval or batch full
                if (buffer.Count > 0 && (!await flushTimer.WaitForNextTickAsync(cancellationToken) ||
                                         buffer.Count >= BatchSize))
                {
                    foreach (var syncTask in buffer)
                    {
                        var command = _syncCommandFactory.CreateFor(syncTask, _replicaPath);
                        await command.ExecuteAsync(cancellationToken);
                    }

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