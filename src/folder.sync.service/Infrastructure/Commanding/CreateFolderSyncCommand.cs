namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFolderSyncCommand(string FolderPath) : ISyncCommand
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Creating folder...");
        // Directory.CreateDirectory(FolderPath);
        return Task.CompletedTask;
    }
}