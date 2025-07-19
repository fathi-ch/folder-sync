using System.Diagnostics;
using System.Security.Cryptography;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;
using FastRsync.Signature;
using FastRsync.Core;
using FastRsync.Delta;

namespace folder.sync.service.Infrastructure.Commanding;

public record UpdateFileSyncCommand : ISyncCommand
{
    private readonly ILogger<UpdateFileSyncCommand> _logger;
    private readonly SyncEntry _source;
    private readonly string _replicaPath;
    private readonly IBatchState _batchState;
    private readonly SyncTask _task;

    public UpdateFileSyncCommand(IBatchState batchState, SyncTask task, SyncEntry source, string replicaPath,ILogger<UpdateFileSyncCommand> logger)
    {
        _logger = logger;
        _batchState = batchState;
        _task = task;
        _source = source;
        _replicaPath = replicaPath;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
{
    var sourceFile = (_source as FileEntry)?.Path;
    if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
    {
        _logger.LogWarning("Source file missing: {Path}", sourceFile);
        _batchState.MarkFailure(_task, new FileNotFoundException("Missing source file", sourceFile));
        return;
    }

    var sigFile = Path.GetTempFileName();
    var deltaFile = Path.GetTempFileName();
    var patchedFile = Path.GetTempFileName();

    try
    {
        //await using (var replicaRead = new FileStream(_replicaPath, FileMode.Open, FileAccess.Read, FileShare.None))
        try {
            await using var replicaRead = new FileStream(_replicaPath, FileMode.Open, FileAccess.Read, FileShare.None);
       
        await using (var sigOut = File.Create(sigFile))
        {
            new SignatureBuilder().Build(replicaRead, new SignatureWriter(sigOut));
        }
        } catch (IOException ex) {
            _logger.LogError(ex, "replicaRead failed");
            throw;
        }
        
        await using var srcStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.None);
        await using var sigIn = new FileStream(sigFile, FileMode.Open, FileAccess.Read, FileShare.None);
        await using (var deltaOut = new FileStream(deltaFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            new DeltaBuilder().BuildDelta(
                srcStream,
                new SignatureReader(sigIn, progressHandler: null),
                new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaOut)));
        }

        
        
        try {
            await using var oldReplica = new FileStream(_replicaPath, FileMode.Open, FileAccess.Read, FileShare.None);
       
        
        await using var deltaIn = new FileStream(deltaFile, FileMode.Open, FileAccess.Read, FileShare.None);
        await using (var patchedOut = new FileStream(patchedFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            new DeltaApplier { SkipHashCheck = true }
                .Apply(oldReplica, new BinaryDeltaReader(deltaIn, progressHandler: null, 4096), patchedOut);
        }
        } catch (IOException ex) {
            _logger.LogError(ex, "oldReplica failed");
            throw;
        }
        // Final copy with full lock
        await using var patchedInput = new FileStream(patchedFile, FileMode.Open, FileAccess.Read, FileShare.None);
       
        
        
        
        try {
            await using var finalOut = new FileStream(_replicaPath, FileMode.Create, FileAccess.Write, FileShare.None);
       
        await patchedInput.CopyToAsync(finalOut, cancellationToken);
        } catch (IOException ex) {
            _logger.LogError(ex, "finalOut failed");
            throw;
        }

        _logger.LogInformation("Replica updated via delta (locked): {Path}", _replicaPath);
        _batchState.MarkSuccess(_task);
    }
    catch (Exception ex)
    {
        await Task.Delay(200, cancellationToken);
        LogLockingProcess(_replicaPath);
        _batchState.MarkFailure(_task, ex);
    }
    finally
    {
        File.Delete(sigFile);
        File.Delete(deltaFile);
        File.Delete(patchedFile);
    }

    await Task.CompletedTask;
}

    
    void LogLockingProcess(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"D:\ProcessHandle\handle.exe",
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            _logger.LogWarning("Locked file info:\n{Output}", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check locking process for: {Path}", filePath);
        }
    }
}
