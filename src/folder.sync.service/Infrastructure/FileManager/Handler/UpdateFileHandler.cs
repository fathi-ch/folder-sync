using FastRsync.Core;
using FastRsync.Delta;
using FastRsync.Signature;

namespace folder.sync.service.Infrastructure.FileManager.Handler;

public class UpdateFileHandler : IFileOperationHandler<UpdateFileOperation>
{
    private readonly ILogger<UpdateFileHandler> _logger;

    public UpdateFileHandler(ILogger<UpdateFileHandler> logger)
    {
        _logger = logger;
    }


    public async Task HandleAsync(UpdateFileOperation operation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operation.SourcePath) || !File.Exists(operation.SourcePath))
        {
            _logger.LogWarning("Source file missing: {Path}", operation.SourcePath);

            return;
        }

        var sigFile = Path.GetTempFileName();
        var deltaFile = Path.GetTempFileName();
        var patchedFile = Path.GetTempFileName();

        try
        {
            try
            {
                await using var replicaRead =
                    new FileStream(operation.DestinationPath, FileMode.Open, FileAccess.Read, FileShare.None);

                await using (var sigOut = File.Create(sigFile))
                {
                    new SignatureBuilder().Build(replicaRead, new SignatureWriter(sigOut));
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "replicaRead failed");
                throw;
            }

            await using var srcStream = new FileStream(operation.SourcePath, FileMode.Open, FileAccess.Read, FileShare.None);
            await using var sigIn = new FileStream(sigFile, FileMode.Open, FileAccess.Read, FileShare.None);
            await using (var deltaOut = new FileStream(deltaFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DeltaBuilder().BuildDelta(
                    srcStream,
                    new SignatureReader(sigIn, null),
                    new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaOut)));
            }

            try
            {
                await using var oldReplica =
                    new FileStream(operation.DestinationPath, FileMode.Open, FileAccess.Read, FileShare.None);


                await using var deltaIn = new FileStream(deltaFile, FileMode.Open, FileAccess.Read, FileShare.None);
                await using (var patchedOut =
                             new FileStream(patchedFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new DeltaApplier { SkipHashCheck = true }
                        .Apply(oldReplica, new BinaryDeltaReader(deltaIn, null, 4096), patchedOut);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "oldReplica failed");
                throw;
            }

            // Final copy with full lock
            await using var patchedInput = new FileStream(patchedFile, FileMode.Open, FileAccess.Read, FileShare.None);

            try
            {
                await using var finalOut =
                    new FileStream(operation.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                await patchedInput.CopyToAsync(finalOut, cancellationToken);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "finalOut failed");
                throw;
            }

            _logger.LogInformation("Replica updated via delta (locked): {Path}", operation.DestinationPath);
        }
        finally
        {
            File.Delete(sigFile);
            File.Delete(deltaFile);
            File.Delete(patchedFile);
        }
    }
}