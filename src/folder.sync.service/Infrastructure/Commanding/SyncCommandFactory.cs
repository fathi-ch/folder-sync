using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class SyncCommandFactory : ISyncCommandFactory
{
    public ISyncCommand CreateFor(SyncTask task, string targetReplica)
    {
        if (string.IsNullOrWhiteSpace(task.Entry.Path))
            throw new InvalidOperationException("Missing RelativePath in entry.");

        var fullDestPath = Path.Combine(targetReplica, task.Entry.Path);


        return task.Command switch
        {
            SyncCommand.Create when task.Entry is FileEntry f => new CreateFileSyncCommand(f, fullDestPath),
            SyncCommand.Update when task.Entry is FileEntry f => new UpdateFileSyncCommand(f, fullDestPath),
            SyncCommand.Create when task.Entry is FolderEntry d => new CreateFolderSyncCommand(fullDestPath),
            SyncCommand.Delete => new DeleteSyncCommand(fullDestPath),
            _ => throw new InvalidOperationException($"Unhandled task: {task.Command} for {task.Entry.Path}")
        };
    }
}