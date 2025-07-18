using System.Threading.Channels;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public interface ISyncTaskProducer
{
    Task ProduceAsync(IAsyncEnumerable<SyncTask> syncTasks, ChannelWriter<SyncTask> writer,
        CancellationToken cancellationToken);

    Task StopProducerAsync(ChannelWriter<SyncTask> writer);
}