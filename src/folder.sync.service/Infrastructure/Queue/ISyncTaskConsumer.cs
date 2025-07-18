using System.Threading.Channels;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public interface ISyncTaskConsumer
{
    Task StartAsync(Channel<SyncTask> reader, CancellationToken cancellationToken);
}