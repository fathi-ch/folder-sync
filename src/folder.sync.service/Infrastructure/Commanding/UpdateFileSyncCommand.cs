using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Commanding;

public record UpdateFileSyncCommand : ISyncCommand
{
    private readonly ILogger<UpdateFileSyncCommand> _logger;
    private readonly SyncEntry _source;
    private readonly string _replicaPath;

    public UpdateFileSyncCommand(SyncEntry source, string replicaPath,ILogger<UpdateFileSyncCommand> logger)
    {
        _logger = logger;
        _source = source;
        _replicaPath = replicaPath;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating file...");

        // using var src = File.OpenRead(Source.Path);
        // using var dst = File.Create(DestinationPath);
        // await src.CopyToAsync(dst, cancellationToken);
    }
}