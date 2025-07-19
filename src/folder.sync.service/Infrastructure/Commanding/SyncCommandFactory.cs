using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class SyncCommandFactory : ISyncCommandFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IBatchState _batchState;
    private readonly FileSystemOperationDispatcher _fileSystemOperationDispatcher;

    public SyncCommandFactory(ILoggerFactory loggerFactory, IBatchState batchState,
        FileSystemOperationDispatcher fileSystemOperationDispatcher)
    {
        _loggerFactory = loggerFactory;
        _batchState = batchState;
        _fileSystemOperationDispatcher = fileSystemOperationDispatcher;
    }

    public ISyncCommand CreateFor(SyncTask task, string targetReplica)
    {
        var relativePath = task.Entry.RelativePath ?? throw new InvalidOperationException("RelativePath missing");
        var fullDestPath = Path.Combine(targetReplica, relativePath);
        var fullSourcePath = task.SourcePath;

        return task.Command switch
        {
            SyncCommand.Create when task.Entry is FileEntry f => new CreateFileSyncCommand(_batchState, task,
                f with { Path = fullSourcePath }, fullDestPath, _loggerFactory.CreateLogger<CreateFileSyncCommand>(),
                _fileSystemOperationDispatcher),
            SyncCommand.Update when task.Entry is FileEntry f => new UpdateFileSyncCommand(_batchState, task,
                f with { Path = fullSourcePath }, fullDestPath, _loggerFactory.CreateLogger<UpdateFileSyncCommand>(),  _fileSystemOperationDispatcher),
            SyncCommand.Create when task.Entry is FolderEntry => new CreateFolderSyncCommand(_batchState, task,
                fullDestPath, _loggerFactory.CreateLogger<CreateFolderSyncCommand>(), _fileSystemOperationDispatcher),
            SyncCommand.Delete => new DeleteSyncCommand(_batchState, task, fullDestPath, _loggerFactory.CreateLogger<DeleteSyncCommand>(),_fileSystemOperationDispatcher),

            _ => throw new InvalidOperationException($"Unhandled task: {task.Command} for {task.Entry.Path}")
        };
    }
}