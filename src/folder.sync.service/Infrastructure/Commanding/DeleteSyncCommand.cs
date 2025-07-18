using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class DeleteSyncCommand : ISyncCommand
{
    private readonly ILogger<DeleteSyncCommand> _logger;
    private readonly string _pathToDelete;

    public DeleteSyncCommand(string pathToDelete, ILogger<DeleteSyncCommand> logger)
    {
        _pathToDelete = pathToDelete;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting: {Path}", _pathToDelete);

            if (File.Exists(_pathToDelete))
                File.Delete(_pathToDelete);
            else if (Directory.Exists(_pathToDelete))
                Directory.Delete(_pathToDelete, recursive: true);
            else
                _logger.LogWarning("Nothing to delete at: {Path}", _pathToDelete);
          
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete failed: {Path}", _pathToDelete);
        }

        await Task.CompletedTask;
    }
}
