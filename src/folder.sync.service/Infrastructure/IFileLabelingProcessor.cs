namespace folder.sync.service.Infrastructure;

public interface IFileLabelingProcessor
{
    IAsyncEnumerable<FileTask> ProcessAsync(string sourcePath, IAsyncEnumerable<FileEntry> sourceFiles,
        string replicaPath,
        IAsyncEnumerable<FileEntry> replicaFiles, CancellationToken cancellationToken);
}

public record FileTask(FileCommand Command, FileEntry Entry);

public enum FileCommand
{
    Create,
    Update,
    Delete
}