using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public class CreateFolderSyncCommand : ISyncCommand
{
    private readonly string _folderPath;
    private readonly ILogger<CreateFolderSyncCommand> _logger;

    public CreateFolderSyncCommand( string folderPath, ILogger<CreateFolderSyncCommand> logger)
    {
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogWarning(ex, "Failed to create folder: {FolderPath}", _folderPath);
        }

        return Task.CompletedTask;
    }
}