using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Commanding;

public record UpdateFileSyncCommand(SyncEntry Source, string ReplicaPath, ILogger<UpdateFileSyncCommand> logger)
    : ISyncCommand
{
    private readonly ILogger<UpdateFileSyncCommand> _logger = logger;

    public async Task ExecuteAsync(CancellationToken CancellationToken)
    {
        _logger.LogInformation("Updating file...");

        // using var src = File.OpenRead(Source.Path);
        // using var dst = File.Create(DestinationPath);
        // await src.CopyToAsync(dst, cancellationToken);
    }
}