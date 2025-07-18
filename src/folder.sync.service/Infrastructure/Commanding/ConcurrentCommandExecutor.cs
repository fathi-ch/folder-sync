namespace folder.sync.service.Infrastructure.Commanding;

public class ConcurrentCommandExecutor : ICommandExecutor
{
    private readonly SemaphoreSlim _semaphore;

    public ConcurrentCommandExecutor(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
    }

    public async Task ExecuteAsync(ISyncCommand command, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await command.ExecuteAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}