using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Commanding;

public interface ISyncCommandFactory
{
    ISyncCommand CreateFor(SyncTask task, string targetReplica);
}