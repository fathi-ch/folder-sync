using System.Diagnostics;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFileSyncCommand : ISyncCommand
{
    private readonly ILogger<CreateFileSyncCommand> _logger;
    private readonly SyncEntry _source;
    private readonly string _replicaPath;

    public CreateFileSyncCommand(SyncEntry source, string replicaPath, ILogger<CreateFileSyncCommand> logger)
    {
        _logger = logger;
        _source = source;
        _replicaPath = replicaPath;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var destDir = Path.GetDirectoryName(_replicaPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir!);

            await using var src = new FileStream(_source.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                true);
            await using var dst = new FileStream(_replicaPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920,
                true);

            _logger.LogInformation("Copying file from {Source} to {Destination} of Size: {Size} bytes", _source.Path,
                _replicaPath, src.Length);

            var stopwatch = Stopwatch.StartNew();
            await src.CopyToAsync(dst, cancellationToken);
            stopwatch.Stop();

            var speed = src.Length / stopwatch.Elapsed.TotalSeconds;
            _logger.LogDebug("Copy completed in {Duration}ms with throughput: {Speed:N0} bytes/sec", stopwatch.ElapsedMilliseconds, speed);
                 
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("File copy operation was canceled. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("Access denied during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
        }
        catch (IOException ex)
        {
            _logger.LogError("I/O error during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
        }
    }
}