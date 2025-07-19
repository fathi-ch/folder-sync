namespace folder.sync.service.Infrastructure.FileManager;

public class FileSystemOperationDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FileSystemOperationDispatcher(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task DispatchAsync(IFileSystemOperation op, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var handlerType = typeof(IFileOperationHandler<>).MakeGenericType(op.GetType());
        dynamic handler = scope.ServiceProvider.GetRequiredService(handlerType);
        await handler.HandleAsync((dynamic)op, ct);
    }
}