using System.Diagnostics;
using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFileSyncCommand(SyncEntry Source, string ReplicaPath, ILogger<CreateFileSyncCommand> _logger)
    : ISyncCommand
{
    private ILogger<CreateFileSyncCommand> _logger = _logger;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var destDir = Path.GetDirectoryName(ReplicaPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir!);
            
            await using var src = new FileStream(Source.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            await using var dst = new FileStream(ReplicaPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            
            _logger.LogInformation("Copying file from {Source} to {Destination} of Size: {Size} bytes", Source.Path, ReplicaPath, src.Length);
            
            var stopwatch =  Stopwatch.StartNew();
            await src.CopyToAsync(dst, cancellationToken);
            stopwatch.Stop();
            
            var speed = src.Length / stopwatch.Elapsed.TotalSeconds;
            _logger.LogInformation("Copy completed in {Duration}ms with throughput: {Speed:N0} bytes/sec", stopwatch.ElapsedMilliseconds, speed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File copy operation was canceled. Source: {Source}, Destination: {Destination}",
                Source.Path, ReplicaPath);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied during file copy. Source: {Source}, Destination: {Destination}",
                Source.Path, ReplicaPath);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error during file copy. Source: {Source}, Destination: {Destination}",
                Source.Path, ReplicaPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file copy. Source: {Source}, Destination: {Destination}",
                Source.Path, ReplicaPath);
            throw;
        }
    }
}