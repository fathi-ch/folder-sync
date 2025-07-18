using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Labeling;

public interface ISyncLabeler
{
    IAsyncEnumerable<SyncTask> ProcessAsync(string sourcePath, IAsyncEnumerable<SyncEntry> sourceFiles,
        string replicaPath,
        IAsyncEnumerable<SyncEntry> replicaFiles, CancellationToken cancellationToken);
}

public record SyncTask(SyncCommand Command, SyncEntry Entry);

public enum SyncCommand
{
    Create,
    Update,
    Delete
}