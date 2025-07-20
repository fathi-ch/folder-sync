using System.Text.Json;
using folder.sync.service.Common;

namespace folder.sync.service.Infrastructure.State;

public class FileFolderStateCache : IFolderStateCache
{
    private readonly ILogger<FileFolderStateCache> _logger;
    private readonly string _filePath = AppConstants.StateFilePath;
    
    public FileFolderStateCache(ILogger<FileFolderStateCache> logger)
    {
        _logger = logger;
    }

    public async Task<FolderState?> GetAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<FolderState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Cache] Failed to read {Message}. Starting fresh.", ex.Message);
            return null;
        }
    }

    public async Task SetAsync(FolderState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Cache] Failed to  write state:{ex.Message}", ex.Message);
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
                _logger.LogInformation("Flushing the state: {Path}", _filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong during flushing the state: {path}", _filePath);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}