using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFileSyncCommand(SyncEntry Source, string ReplicaPath) : ISyncCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Creating folder...");
        // var destDir = Path.GetDirectoryName(DestinationPath);
        // if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
        // using var src = File.OpenRead(Source.Path);
        // using var dst = File.Create(DestinationPath);
        // await src.CopyToAsync(dst, cancellationToken);
    }
}