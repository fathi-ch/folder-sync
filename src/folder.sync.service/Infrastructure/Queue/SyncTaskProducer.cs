using System.Threading.Channels;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public class SyncTaskProducer : ISyncTaskProducer
{
    private readonly ILogger<SyncTaskProducer> _logger;

    public SyncTaskProducer(ILogger<SyncTaskProducer> logger)
    {
        _logger = logger;
    }

    public async Task ProduceAsync(IAsyncEnumerable<SyncTask> syncTasks, ChannelWriter<SyncTask> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var task in syncTasks.WithCancellation(cancellationToken))
                await writer.WriteAsync(task, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Channel closed unexpectedly while writing sync tasks.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Task production was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during task production.");
            throw;
        }
    }

    public Task StopProducerAsync(ChannelWriter<SyncTask> writer)
    {
        try
        {
            writer.TryComplete();
            _logger.LogInformation("Closing channel ...");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to complete the ChannelWriter {message}.", ex.Message);
        }

        return Task.CompletedTask;
    }
}