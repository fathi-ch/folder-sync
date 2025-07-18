using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class SyncCommandFactory : ISyncCommandFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SyncCommandFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ISyncCommand CreateFor(SyncTask task, string targetReplica)
    {
        var relativePath = task.Entry.RelativePath ?? throw new InvalidOperationException("RelativePath missing");
        var fullDestPath = Path.Combine(targetReplica, relativePath);
        var fullSourcePath = task.SourcePath;

        return task.Command switch
        {
            SyncCommand.Create when task.Entry is FileEntry f => new CreateFileSyncCommand(
                f with { Path = fullSourcePath }, fullDestPath, _loggerFactory.CreateLogger<CreateFileSyncCommand>()),
            SyncCommand.Update when task.Entry is FileEntry f => new UpdateFileSyncCommand(
                f with { Path = fullSourcePath }, fullDestPath, _loggerFactory.CreateLogger<UpdateFileSyncCommand>()),
            SyncCommand.Create when task.Entry is FolderEntry => new CreateFolderSyncCommand(fullDestPath,
                _loggerFactory.CreateLogger<CreateFolderSyncCommand>()),
            SyncCommand.Delete => new DeleteSyncCommand(fullDestPath, _loggerFactory.CreateLogger<DeleteSyncCommand>()),

            _ => throw new InvalidOperationException($"Unhandled task: {task.Command} for {task.Entry.Path}")
        };
    }
}