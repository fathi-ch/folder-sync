using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public record UpdateFileSyncCommand : ISyncCommand
{
    private readonly FileSystemOperationDispatcher _fileSystemOperationDispatcher;
    private readonly ILogger<UpdateFileSyncCommand> _logger;
    private readonly string _replicaPath;
    private readonly string _sourcePath;
    private readonly IBatchState _batchState;
    private readonly SyncTask _task;

    public UpdateFileSyncCommand(IBatchState batchState, SyncTask task, SyncEntry source, string replicaPath,
        ILogger<UpdateFileSyncCommand> logger, FileSystemOperationDispatcher fileSystemOperationDispatcher)
    {
        _logger = logger;
        _fileSystemOperationDispatcher = fileSystemOperationDispatcher;
        _batchState = batchState;
        _task = task;
        _sourcePath = (source as FileEntry)?.Path ?? throw new InvalidOperationException();
        _replicaPath = replicaPath;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fileSystemOperationDispatcher.DispatchAsync(new UpdateFileOperation(_sourcePath, _replicaPath), cancellationToken);
            _batchState.MarkSuccess(_task);
        }
        catch (Exception ex)
        {
            _logger.LogError("Update Command Faild for: {file}", _replicaPath);
            _batchState.MarkFailure(_task, ex);
        }
    }
}