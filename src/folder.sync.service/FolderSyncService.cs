using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure;

namespace folder.sync.service;

public class FolderSyncService : BackgroundService
{
    private readonly ILogger<FolderSyncService> _logger;
    private readonly IFolderSyncPipeline _folderSyncPipeline;
    private readonly FolderSyncServiceConfig _config;

    public FolderSyncService(ILogger<FolderSyncService> logger, FolderSyncServiceConfig config,
        IFolderSyncPipeline folderSyncPipeline)
    {
        _logger = logger;
        _config = config;
        _folderSyncPipeline = folderSyncPipeline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.IntervalInSec));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            try
            {
                await _folderSyncPipeline.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                var flattened = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError("Error: {Type} | Msg: {Message} | Stack: {Stack}",
                    ex.GetType().Name,
                    ex.Message,
                    ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim());
            }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _folderSyncPipeline.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}