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
        //1- Loading Source files
        var sourceFiles =  _syncEntryFileLoader.LoadFilesAsync(sourcePath, cancellationToken);
        
        //2- Computing the folder state "snapshot"
        var actualState = await FolderStateDeterminer.DetermineAsync(sourceFiles);
        
        var oldState = await _syncFolderStateCache.GetAsync();

        if (oldState == null || !oldState.Equals(actualState))
        { 
            //3- Loading replica files
            var replicaFiles = _syncEntryFileLoader.LoadFilesAsync(replicaPath, cancellationToken);
            
            //4- Labeling based on changes, existance
            var labeledSyncTasks =
                _syncLabeler.ProcessAsync(sourcePath, sourceFiles, replicaPath, replicaFiles, cancellationToken);

            //5- Producing Tasks
            await _syncTaskProducer.ProduceAsync(labeledSyncTasks, _taskQueueChannel, cancellationToken);
            
            //6- Consuming Tasks
            _ = _batchSyncTaskConsumer.StartAsync(_taskQueueChannel, cancellationToken);
            
            //7- Saving the current folder state
            await _syncFolderStateCache.SetAsync(actualState);
            _logger.LogInformation("Loading completed.");
        }
        else
        {
            var sourceCount = await sourceFiles.CountAsync(cancellationToken);
            var replicaCount = await _syncEntryFileLoader.LoadFilesAsync(replicaPath, cancellationToken).CountAsync(cancellationToken);
            if (sourceCount > replicaCount)
            {
                _logger.LogDebug("Folders are not in Sync, [COUNT] source: {sourceCount} vs replicas: {repCount}", sourceCount, replicaCount);
                _syncFolderStateCache.FlushAsync();
            }
                    
            _logger.LogInformation("No changes detected.");
        }
    }

    public async Task StopAsync()
    {
        await _syncTaskProducer.StopProducerAsync(_taskQueueChannel);
        _logger.LogInformation("Folder sync pipeline stopped.");
    }
}