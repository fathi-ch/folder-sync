using System.Threading.Channels;
using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure.Commanding;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public class SyncTaskConsumer : ISyncTaskConsumer
{
    private readonly ISyncCommandFactory _syncCommandFactory;
    private readonly string _replicaPath;
    private Task? _worker;

    public SyncTaskConsumer(ISyncCommandFactory syncCommandFactory,
        FolderSyncServiceConfig config)
    {
        _syncCommandFactory = syncCommandFactory;
        _replicaPath = config.ReplicaPath;
    }

    public Task StartAsync(Channel<SyncTask> reader, CancellationToken cancellationToken)
    {
        // Ensure only one worker is started
        _worker ??= Task.Run(() => ConsumeLoopAsync(reader, cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeLoopAsync(Channel<SyncTask> reader, CancellationToken cancellationToken)
    {
        await foreach (var task in reader.Reader.ReadAllAsync(cancellationToken))
            try
            {
                var command = _syncCommandFactory.CreateFor(task, _replicaPath);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Failed to execute task: {ex.Message}");
                // Optional: push to stagingQueue for retry
            }
    }
}