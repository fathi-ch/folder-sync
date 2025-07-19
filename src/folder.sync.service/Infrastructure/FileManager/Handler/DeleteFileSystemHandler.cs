using System.Globalization;

namespace folder.sync.service.Infrastructure.FileManager.Handler;

public class DeleteFileSystemHandler : IFileOperationHandler<DeleteFileSystemOperation>
{
    private readonly ILogger<DeleteFileSystemHandler> _logger;

    public DeleteFileSystemHandler(ILogger<DeleteFileSystemHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(DeleteFileSystemOperation operation, CancellationToken cancellationToken = default)
    {
        if (File.Exists(operation.Path))
        {
            File.Delete(operation.Path);
            _logger.LogInformation("Deleted file: {Path}", operation.Path);
        }
        else if (Directory.Exists(operation.Path))
        {
            Directory.Delete(operation.Path, true);
            _logger.LogInformation("Deleted directory: {Path}", operation.Path);
        }

        await Task.CompletedTask;
    }
}