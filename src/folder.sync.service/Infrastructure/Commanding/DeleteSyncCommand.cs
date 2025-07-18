namespace folder.sync.service.Infrastructure.Commanding;

public record DeleteSyncCommand(string PathToDelete, ILogger<DeleteSyncCommand> logger) : ISyncCommand
{
    private readonly ILogger<DeleteSyncCommand> _logger = logger;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Deleting folder...");
        // if (File.Exists(PathToDelete)) File.Delete(PathToDelete);
        // else if (Directory.Exists(PathToDelete)) Directory.Delete(PathToDelete, recursive: true);
        // return Task.CompletedTask;
    }
}