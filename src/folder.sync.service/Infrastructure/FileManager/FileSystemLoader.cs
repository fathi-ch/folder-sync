using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace folder.sync.service.Infrastructure.FileManager;

public class FileSystemLoader : IFileLoader
{
    private readonly ILogger<FileSystemLoader> _logger;

    public FileSystemLoader(ILogger<FileSystemLoader> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<SyncEntry> LoadFilesAsync(string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
            yield break;

        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDir = pending.Pop();

            FolderEntry? folderEntry = null;
            try
            {
                var dirInfo = new DirectoryInfo(currentDir);
                folderEntry = new FolderEntry(dirInfo.FullName, 0, dirInfo.LastWriteTimeUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Dir] {currentDir} skipped: {ex.Message}", currentDir, ex.Message);
            }

            if (folderEntry is not null)
                yield return folderEntry;

            var files = Array.Empty<string>();
            var subdirs = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(currentDir);
                subdirs = Directory.GetDirectories(currentDir);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Access] {currentDir}: {ex.Message}", currentDir, ex.Message);
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileEntry? fileEntry = null;

                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Exists)
                    {
                        var hash = await ComputeFileHashAsync(fi.FullName, cancellationToken);
                        fileEntry = new FileEntry(fi.FullName, fi.Length, fi.LastWriteTimeUtc, hash);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("[File] {file} skipped: {ex.Message}", file, ex.Message);
                }

                if (fileEntry is not null)
                    yield return fileEntry;
            }

            foreach (var subdir in subdirs) pending.Push(subdir);
        }
    }

    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[Hash] {Path} canceled unexpectedly.", path);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("[Hash] {path} skipped: {ex.Message}", path, ex.Message);
            return string.Empty;
        }
    }
}