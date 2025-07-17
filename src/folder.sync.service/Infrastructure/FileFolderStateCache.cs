using System.Text.Json;

namespace folder.sync.service.Infrastructure;

public class FileFolderStateCache : IFolderStateCache
{
    private readonly ILogger<FileFolderStateCache> _logger;
    private readonly string _filePath = ".cache/folder_state.json";

    public FileFolderStateCache(ILogger<FileFolderStateCache> logger)
    {
        _logger = logger;
        var dir = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir!);
    }

    public async Task<FolderState?> GetAsync(string folderPath)
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

    public async Task SetAsync(string folderPath, FolderState state)
    {
        try
        {
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
}