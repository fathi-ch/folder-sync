using System.Threading.Channels;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;
using folder.sync.service.Infrastructure.Queue;
using folder.sync.service.Infrastructure.State;

namespace folder.sync.service.Infrastructure.Pipeline;

public class FolderSyncPipeline : IFolderSyncPipeline
{
    private readonly ILogger<FolderSyncPipeline> _logger;

    //private readonly IFileLoader _fileLoader;
    private readonly ISyncLabeler _syncLabeler;
    private readonly IFolderStateCache _folderStateCache;
    private readonly ISyncTaskProducer _syncTaskProducer;
    private readonly Channel<SyncTask> _taskQueueChannel;
    private readonly ISyncTaskConsumer _batchSyncTaskConsumer;

    private const string STATE_PATH = ".cache/folder_state.json";

    public FolderSyncPipeline(ILogger<FolderSyncPipeline> logger, ISyncLabeler syncLabeler,
        IFolderStateCache folderStateCache, Channel<SyncTask> taskQueueChannel, ISyncTaskProducer syncTaskProducer,
        ISyncTaskConsumer batchSyncTaskConsumer)
    {
        _taskQueueChannel = taskQueueChannel;
        _syncTaskProducer = syncTaskProducer;
        _batchSyncTaskConsumer = batchSyncTaskConsumer;
        _folderStateCache = folderStateCache;
        _syncLabeler = syncLabeler;
        _logger = logger;
    }

    public async Task RunAsync(string sourcePath, string replicaPath, CancellationToken cancellationToken)
    {
        var loaderS = new DummyLoaderTest(true);
        var sourceFiles = loaderS.LoadFilesAsync(sourcePath);

        var actualState = await FolderStateDeterminer.DetermineAsync(sourceFiles);
        var oldState = await _folderStateCache.GetAsync(STATE_PATH);

        if (oldState == null || !oldState.Equals(actualState))
        {
            var loaderR = new DummyLoaderTest(false);
            var replicaFiles = loaderR.LoadFilesAsync(replicaPath);

            var task = _syncLabeler.ProcessAsync(sourcePath, sourceFiles, replicaPath, replicaFiles, cancellationToken);

            await _syncTaskProducer.ProduceAsync(task, _taskQueueChannel, cancellationToken);

            _ = _batchSyncTaskConsumer.StartAsync(_taskQueueChannel, cancellationToken);

            _logger.LogInformation("Loading completed.");
            await _folderStateCache.SetAsync(sourcePath, actualState);
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