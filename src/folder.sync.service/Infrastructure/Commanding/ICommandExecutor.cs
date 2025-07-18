namespace folder.sync.service.Infrastructure.Commanding;

public interface ICommandExecutor
{
    Task ExecuteAsync(ISyncCommand command, CancellationToken cancellationToken);
}