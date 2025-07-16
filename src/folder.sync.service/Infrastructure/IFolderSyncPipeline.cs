namespace folder.sync.service.Infrastructure;

public interface IFolderSyncPipeline
{
    Task RunAsync(CancellationToken cancellationToken);
    Task StopAsync();
}