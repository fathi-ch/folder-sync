namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFolderSyncCommand(string FolderPath, ILogger<CreateFolderSyncCommand> logger) : ISyncCommand
{
    private ILogger<CreateFolderSyncCommand> _logger = logger;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Creating folder...");
        // Directory.CreateDirectory(FolderPath);
        return Task.CompletedTask;
    }
}