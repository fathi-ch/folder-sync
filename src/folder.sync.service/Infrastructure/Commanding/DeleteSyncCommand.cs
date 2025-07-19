using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class DeleteSyncCommand : ISyncCommand
{
    private readonly ILogger<DeleteSyncCommand> _logger;
    private readonly string _pathToDelete;
    private readonly IBatchState _batchState;
    private readonly SyncTask _task;

    public DeleteSyncCommand(IBatchState batchState, SyncTask task, string pathToDelete, ILogger<DeleteSyncCommand> logger)
    {
        _pathToDelete = pathToDelete;
        _logger = logger;
        _batchState = batchState;
        _task = task;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting: {Path}", _pathToDelete);

            if (File.Exists(_pathToDelete))
            {
                File.Delete(_pathToDelete);
                _logger.LogInformation("Deleted file: {Path}", _pathToDelete);
                _batchState.MarkSuccess(_task);
            }
            else if (Directory.Exists(_pathToDelete))
            {
                Directory.Delete(_pathToDelete, recursive: true);
                _logger.LogInformation("Deleted directory: {Path}", _pathToDelete);
                _batchState.MarkSuccess(_task);
            }
            else
            {
                var message = $"Path not found during delete: {_pathToDelete}";
                _logger.LogWarning(message);
                _batchState.MarkFailure(_task, new FileNotFoundException(message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete failed: {Path}", _pathToDelete);
            _batchState.MarkFailure(_task,ex);
        }

        await Task.CompletedTask;
    }
}
