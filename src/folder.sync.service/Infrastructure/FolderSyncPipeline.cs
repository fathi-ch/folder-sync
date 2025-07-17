namespace folder.sync.service.Infrastructure;

public class FolderSyncPipeline : IFolderSyncPipeline
{
    private readonly ILogger<FolderSyncPipeline> _logger;

    //private readonly IFileLoader _fileLoader;
    private readonly IFileLabelingProcessor _fileLabelingProcessor;
    private readonly IFolderStateCache _folderStateCache;

    public FolderSyncPipeline(ILogger<FolderSyncPipeline> logger, IFileLabelingProcessor fileLabelingProcessor,
        IFolderStateCache folderStateCache)
    {
        _logger = logger;
        _fileLabelingProcessor = fileLabelingProcessor;
        _folderStateCache = folderStateCache;
    }

    public async Task RunAsync(string sourcePath, string replicaPath, CancellationToken cancellationToken)
    {
        var loaderS = new DummyLoaderTest(true);
        var sourceFiles = loaderS.LoadFilesAsync(sourcePath);

        var actualState = await FolderStateDeterminer.DetermineAsync(sourceFiles);
        var oldState = await _folderStateCache.GetAsync(".cache/folder_state.json");

        if (oldState == null || !oldState.Equals(actualState))
        {
            var loaderR = new DummyLoaderTest(false);
            var replicaFiles = loaderR.LoadFilesAsync(replicaPath);


            var result = _fileLabelingProcessor
                .ProcessAsync(sourcePath, sourceFiles, replicaPath, replicaFiles, cancellationToken);

            _logger.LogInformation("Loading completed.");
            //await _folderStateCache.SetAsync(sourcePath, actualState);
        }
        else
        {
            _logger.LogInformation(" No changes detected.");
        }
    }

    public async Task StopAsync()
    {
        Thread.Sleep(2000);
        _logger.LogInformation("Folder sync pipeline stopped.");
    }
}