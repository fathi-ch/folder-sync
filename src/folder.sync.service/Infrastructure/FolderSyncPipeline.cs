namespace folder.sync.service.Infrastructure;

public class FolderSyncPipeline : IFolderSyncPipeline
{
    private ILogger<FolderSyncPipeline> _logger;

    public FolderSyncPipeline(ILogger<FolderSyncPipeline> logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Folder sync pipeline started.");
    }

    public async Task StopAsync()
    {
        Thread.Sleep(2000);
        _logger.LogInformation("Folder sync pipeline stopped.");
    }
}