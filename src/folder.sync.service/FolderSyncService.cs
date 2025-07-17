using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure;

namespace folder.sync.service;

public class FolderSyncService : BackgroundService
{
    private readonly ILogger<FolderSyncService> _logger;
    private readonly IFolderSyncPipeline _folderSyncPipeline;
    private readonly string _sourcePath;
    private readonly string _replicaPath;
    private readonly int _intervalInSec;

    public FolderSyncService(ILogger<FolderSyncService> logger, FolderSyncServiceConfig config,
        IFolderSyncPipeline folderSyncPipeline)
    {
        _folderSyncPipeline = folderSyncPipeline;
        _intervalInSec = config.IntervalInSec;
        _replicaPath = config.ReplicaPath;
        _sourcePath = config.SourcePath;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalInSec));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            try
            {
                await _folderSyncPipeline.RunAsync(_sourcePath, _replicaPath, stoppingToken);
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