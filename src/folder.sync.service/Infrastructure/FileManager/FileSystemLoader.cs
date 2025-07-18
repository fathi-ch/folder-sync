namespace folder.sync.service.Infrastructure.FileManager;

public class FileSystemLoader : IFileLoader
{
    private readonly ILogger<FileSystemLoader> _logger;

    public FileSystemLoader(ILogger<FileSystemLoader> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<SyncEntry> LoadFilesAsync(string path, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogError("Source Path does not exist: {Path}", path);
            yield break;
        }
        
        var entries = Directory.EnumerateFileSystemEntries(path,"*", SearchOption.AllDirectories);

        foreach (var entry in entries)
        {
            if(cancellationToken.IsCancellationRequested)
               yield break;

            SyncEntry? result = null;

            try
            {
                var info = new FileInfo(entry);
                if (info.Exists)
                {
                    var hash = await ComputeHashAsync(info.FullName, cancellationToken);
                    result = new FileEntry(
                        Path: info.FullName,
                        Size: info.Length,
                        LastModified: info.LastWriteTimeUtc,
                        Hash: hash
                    ); 
                }
                else
                {
                    var dirInfo = new DirectoryInfo(entry);
                    if (dirInfo.Exists)
                    {
                        result = new FolderEntry(
                            Path: dirInfo.FullName,
                            LastModified: dirInfo.LastWriteTimeUtc
                        );
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load entry: {Entry}", entry);
                continue;
            }
            if (result is not null)
                yield return result;

            await Task.Yield();
        }
    }
    
    private async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hash for {Path}", filePath);
            return "HASH_FAIL";
        }
    }
}