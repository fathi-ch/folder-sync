using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class CreateFolderSyncCommand : ISyncCommand
{
    private readonly string _folderPath;
    private readonly ILogger<CreateFolderSyncCommand> _logger;
    private readonly IBatchState _batchState;
    private readonly SyncTask _task;

    public CreateFolderSyncCommand(IBatchState batchState, SyncTask task, string folderPath, ILogger<CreateFolderSyncCommand> logger)
    {
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _batchState = batchState;
        _task = task;
    }

    public Task ExecuteAsync( CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(_folderPath))
            {
                _logger.LogInformation("Creating folder: {FolderPath}", _folderPath);
                Directory.CreateDirectory(_folderPath);
            }
            else
            {
                _logger.LogDebug("Folder already exists: {FolderPath}", _folderPath);
            }
            
        }
        catch (Exception ex)
        {
            var message = $"Path not found during delete: {_folderPath}";
            _logger.LogWarning(ex, "Failed to create folder: {FolderPath}", _folderPath);
            _batchState.MarkFailure(_task, new FileNotFoundException(message));
        }

        return Task.CompletedTask;
    }
}