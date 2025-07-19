namespace folder.sync.service.Infrastructure.FileManager;

public interface IFileOperationHandler<in T> where T : IFileSystemOperation
{
    Task HandleAsync(T operation, CancellationToken cancellationToken = default);
}