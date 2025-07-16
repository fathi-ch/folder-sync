using folder.sync.service.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace folder.sync.service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly FolderSyncServiceConfig _config;

    public Worker(ILogger<Worker> logger, FolderSyncServiceConfig config)
    {
        _logger = logger;
        _config = config; 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {Time}, polling every {config}", DateTimeOffset.Now, _config.IntervalInSec);
            await Task.Delay(1000, stoppingToken);
        }
    }
}