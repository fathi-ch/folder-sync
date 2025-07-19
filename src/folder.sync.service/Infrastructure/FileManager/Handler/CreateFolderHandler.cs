namespace folder.sync.service.Infrastructure.FileManager.Handler;

public class CreateFolderHandler : IFileOperationHandler<CreateFolderOperation>
{
    private readonly ILogger<CreateFolderOperation> _logger;

    public CreateFolderHandler(ILogger<CreateFolderOperation> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(CreateFolderOperation operation, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(operation.Path))
        {
            _logger.LogInformation("Creating folder: {FolderPath}", operation.Path);
            Directory.CreateDirectory(operation.Path);
        }
        else
        {
            _logger.LogDebug("Folder already exists: {FolderPath}", operation.Path);
        }

        await Task.CompletedTask;
    }
}