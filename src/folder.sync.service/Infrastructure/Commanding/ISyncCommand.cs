namespace folder.sync.service.Infrastructure.Commanding;

public interface ISyncCommand
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}