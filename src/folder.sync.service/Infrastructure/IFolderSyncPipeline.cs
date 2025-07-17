namespace folder.sync.service.Infrastructure;

public interface IFolderSyncPipeline
{
    Task RunAsync(string sourcePath, string replicaPath, CancellationToken cancellationToken);
    Task StopAsync();
}