using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Commanding;

public record UpdateFileSyncCommand(SyncEntry Source, string ReplicaPath) : ISyncCommand
{
    public async Task ExecuteAsync(CancellationToken CancellationToken)
    {
        Console.WriteLine("Updating folder...");

        // using var src = File.OpenRead(Source.Path);
        // using var dst = File.Create(DestinationPath);
        // await src.CopyToAsync(dst, cancellationToken);
    }
}