using System.Threading.Channels;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;
using folder.sync.service.Infrastructure.Queue;
using folder.sync.service.Infrastructure.State;

namespace folder.sync.service.Infrastructure.Pipeline;

public class FolderSyncPipeline : IFolderSyncPipeline
{
    private readonly ILogger<FolderSyncPipeline> _logger;

    private readonly IFileLoader _syncEntryFileLoader;
    private readonly ISyncLabeler _syncLabeler;
    private readonly IFolderStateCache _syncFolderStateCache;
    private readonly ISyncTaskProducer _syncTaskProducer;
    private readonly Channel<SyncTask> _taskQueueChannel;
    private readonly ISyncTaskConsumer _batchSyncTaskConsumer;

    private const string StateFilePath = ".cache/folder_state.json";

    public FolderSyncPipeline(ILogger<FolderSyncPipeline> logger, ISyncLabeler syncLabeler,
        IFolderStateCache syncFolderStateCache, Channel<SyncTask> taskQueueChannel, ISyncTaskProducer syncTaskProducer,
        ISyncTaskConsumer batchSyncTaskConsumer, IFileLoader syncEntryFileLoader)
    {
        _taskQueueChannel = taskQueueChannel;
        _syncTaskProducer = syncTaskProducer;
        _batchSyncTaskConsumer = batchSyncTaskConsumer;
        _syncEntryFileLoader = syncEntryFileLoader;
        _syncFolderStateCache = syncFolderStateCache;
        _syncLabeler = syncLabeler;
        _logger = logger;
    }

    public async Task RunAsync(string sourcePath, string replicaPath, CancellationToken cancellationToken)
    {
        var sourceFiles = _syncEntryFileLoader.LoadFilesAsync(sourcePath, cancellationToken);
        var actualState = await FolderStateDeterminer.DetermineAsync(sourceFiles);
        var oldState = await _syncFolderStateCache.GetAsync(StateFilePath);

        if (oldState == null || !oldState.Equals(actualState))
        {
            var replicaFiles = _syncEntryFileLoader.LoadFilesAsync(replicaPath, cancellationToken);

            var labeledSyncTasks =
                _syncLabeler.ProcessAsync(sourcePath, sourceFiles, replicaPath, replicaFiles, cancellationToken);

            await _syncTaskProducer.ProduceAsync(labeledSyncTasks, _taskQueueChannel, cancellationToken);

            _ = _batchSyncTaskConsumer.StartAsync(_taskQueueChannel, cancellationToken);

            await _syncFolderStateCache.SetAsync(StateFilePath, actualState);
            _logger.LogInformation("Loading completed.");
        }
        else
        {
            _logger.LogInformation(" No changes detected.");
        }
    }

    public async Task StopAsync()
    {
        await _syncTaskProducer.StopProducerAsync(_taskQueueChannel);
        _logger.LogInformation("Folder sync pipeline stopped.");
    }
}