using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class CreateFolderSyncCommand : ISyncCommand
{
    private readonly FileSystemOperationDispatcher _fileSystemOperationDispatcher;
    private readonly ILogger<CreateFolderSyncCommand> _logger;
    private readonly IBatchState _batchState;
    private readonly string _folderPath;
    private readonly SyncTask _task;

    public CreateFolderSyncCommand(IBatchState batchState, SyncTask task, string folderPath,
        ILogger<CreateFolderSyncCommand> logger, FileSystemOperationDispatcher fileSystemOperationDispatcher)
    {
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystemOperationDispatcher = fileSystemOperationDispatcher;
        _batchState = batchState;
        _task = task;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fileSystemOperationDispatcher.DispatchAsync(new CreateFolderOperation(_folderPath),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create folder: {FolderPath}", _folderPath);
            _batchState.MarkFailure(_task, ex);
        }
    }
}