using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;
using folder.sync.service.Infrastructure.Queue;

namespace folder.sync.service.Infrastructure.Commanding;

public class DeleteSyncCommand : ISyncCommand
{
    private readonly FileSystemOperationDispatcher _fileSystemOperationDispatcher;
    private readonly ILogger<DeleteSyncCommand> _logger;
    private readonly string _pathToDelete;
    private readonly IBatchState _batchState;
    private readonly SyncTask _task;

    public DeleteSyncCommand(IBatchState batchState, SyncTask task, string pathToDelete,
        ILogger<DeleteSyncCommand> logger, FileSystemOperationDispatcher fileSystemOperationDispatcher)
    {
        _pathToDelete = pathToDelete;
        _logger = logger;
        _fileSystemOperationDispatcher = fileSystemOperationDispatcher;
        _batchState = batchState;
        _task = task;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("[CMD] Executing Delete: {Path}", _pathToDelete);
            await _fileSystemOperationDispatcher.DispatchAsync(new DeleteFileSystemOperation(_pathToDelete), cancellationToken);
            _batchState.MarkSuccess(_task);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete failed: {Path}", _pathToDelete);
            _batchState.MarkFailure(_task, ex);
        }

        await Task.CompletedTask;
    }
}