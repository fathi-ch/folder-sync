using System.Diagnostics;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public record CreateFileSyncCommand : ISyncCommand
{
    private readonly FileSystemOperationDispatcher _fileSystemOperationDispatcher;
    private readonly ILogger<CreateFileSyncCommand> _logger;
    private readonly IBatchState _batchState;
    private readonly string _replicaPath;
    private readonly SyncEntry _source;
    private readonly SyncTask _task;

    public CreateFileSyncCommand(IBatchState batchState, SyncTask task, SyncEntry source, string replicaPath,
        ILogger<CreateFileSyncCommand> logger, FileSystemOperationDispatcher fileSystemOperationDispatcher)
    {
        _logger = logger;
        _fileSystemOperationDispatcher = fileSystemOperationDispatcher;
        _batchState = batchState;
        _task = task;
        _source = source;
        _replicaPath = replicaPath;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fileSystemOperationDispatcher.DispatchAsync(new CreateFileOperation(_source.Path, _replicaPath), cancellationToken);
            _batchState.MarkSuccess(_task);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("File copy operation was canceled. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
            _batchState.MarkFailure(_task, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("Access denied during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
            _batchState.MarkFailure(_task, ex);
        }
        catch (IOException ex)
        {
            _logger.LogError("I/O error during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
            _batchState.MarkFailure(_task, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during file copy. Source: {Source}, Destination: {Destination}", _source.Path, _replicaPath);
            _batchState.MarkFailure(_task, ex);
        }
    }
}