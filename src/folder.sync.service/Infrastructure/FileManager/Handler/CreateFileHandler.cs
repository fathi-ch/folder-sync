using System.Diagnostics;

namespace folder.sync.service.Infrastructure.FileManager.Handler;

public class CreateFileHandler : IFileOperationHandler<CreateFileOperation>
{
    private readonly ILogger<CreateFileHandler> _logger;

    public CreateFileHandler(ILogger<CreateFileHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(CreateFileOperation operation, CancellationToken cancellationToken = default)
    {
        var destDir = Path.GetDirectoryName(operation.DestinationPath);
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir!);

        await using var src = new FileStream(operation.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, true);
        await using var dst = new FileStream(operation.DestinationPath, FileMode.Create, FileAccess.Write,
            FileShare.None, 81920, true);

        _logger.LogInformation("Copying file from {Source} to {Destination} of Size: {Size} bytes",
            operation.SourcePath, operation.DestinationPath, src.Length);

        var stopwatch = Stopwatch.StartNew();
        await src.CopyToAsync(dst, cancellationToken);
        stopwatch.Stop();

        var speed = src.Length / stopwatch.Elapsed.TotalSeconds;
        _logger.LogDebug("Copy completed in {Duration}ms with throughput: {Speed:N0} bytes/sec",
            stopwatch.ElapsedMilliseconds, speed);
    }
}