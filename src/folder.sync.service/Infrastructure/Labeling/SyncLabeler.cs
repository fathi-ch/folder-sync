using System.Runtime.CompilerServices;
using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Labeling;

public class SyncLabeler : ISyncLabeler
{
    private readonly ILogger<SyncLabeler> _logger;

    public SyncLabeler(ILogger<SyncLabeler> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<SyncTask> ProcessAsync(string sourcePath, IAsyncEnumerable<SyncEntry> sourceFiles,
        string replicaPath, IAsyncEnumerable<SyncEntry> replicaFiles,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sourceMap = new Dictionary<string, SyncEntry>();
        var destMap = new Dictionary<string, SyncEntry>();

        try
        {
            await foreach (var src in sourceFiles.WithCancellation(cancellationToken))
            {
                if (string.Equals(src.Path.TrimEnd('\\', '/'), sourcePath.TrimEnd('\\', '/'),
                        StringComparison.OrdinalIgnoreCase))
                    continue; // Skip root source folder

                var relPath = GetRelativePath(src.Path, sourcePath);
                _logger.LogDebug("SRC: full={Full}, rel={Rel}", src.Path, relPath);

                var entry = src switch
                {
                    FileEntry f => f with { Path = relPath, RelativePath = relPath },
                    FolderEntry d => d with { Path = relPath, RelativePath = relPath },
                    _ => src
                };

                sourceMap[relPath] = entry;
            }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                _logger.LogInformation("File loading canceled due to shutdown.");
            else
                _logger.LogError(ex, "Failed to load source files from {SourcePath}", sourcePath);
            yield break;
        }

        try
        {
            await foreach (var dst in replicaFiles.WithCancellation(cancellationToken))
            {
                if (string.Equals(dst.Path.TrimEnd('\\', '/'), replicaPath.TrimEnd('\\', '/'),
                        StringComparison.OrdinalIgnoreCase))
                    continue; // Skip root replica folder

                var relPath = GetRelativePath(dst.Path, replicaPath);
                _logger.LogDebug("DST: full={Full}, rel={Rel}", dst.Path, relPath);

                var entry = dst switch
                {
                    FileEntry f => f with { Path = relPath, RelativePath = relPath },
                    FolderEntry d => d with { Path = relPath, RelativePath = relPath },
                    _ => dst
                };

                destMap[relPath] = entry;
            }

            if (!destMap.Any())
                _logger.LogDebug("No replica entries loaded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load replica files from {ReplicaPath}", replicaPath);
            yield break;
        }

        _logger.LogDebug("Labeling sync tasks. Source: {Source}, Replica: {Replica}", sourcePath, replicaPath);

        foreach (var folder in sourceMap.Values
                     .OfType<FolderEntry>()
                     .Where(f =>
                         !destMap.ContainsKey(f.Path) &&
                         !string.IsNullOrEmpty(f.Path) &&
                         f.Path != ".")
                     .OrderBy(f => f.Path.Count(c => c == Path.DirectorySeparatorChar)))
        {
            _logger.LogDebug("Evaluating folder: {Path}", folder.Path);
            yield return new SyncTask(SyncCommand.Create, folder, Path.Combine(sourcePath, folder.Path));
        }

        foreach (var (path, srcEntry) in sourceMap)
            if (srcEntry is FileEntry srcFile)
            {
                if (!destMap.TryGetValue(path, out var dstEntry))
                {
                    _logger.LogDebug("[CREATE] {Path}", srcFile.Path);
                    yield return new SyncTask(SyncCommand.Create, srcFile, Path.Combine(sourcePath, srcFile.Path));
                }
                else if (dstEntry is FileEntry dstFile)
                {
                    if (srcFile.Hash != dstFile.Hash || srcFile.LastModified > dstFile.LastModified)
                    {
                        _logger.LogDebug("[UPDATE] {Path}", srcFile.Path);
                        yield return new SyncTask(SyncCommand.Update, srcFile, Path.Combine(sourcePath, srcFile.Path));
                    }
                }
            }

        foreach (var entry in destMap
                     .Where(kvp => !sourceMap.ContainsKey(kvp.Key))
                     .OrderByDescending(kvp => kvp.Value.Path.Count(c => c == Path.DirectorySeparatorChar)))
        {
            _logger.LogDebug("[DELETE] {Path}", entry.Value.Path);
            yield return new SyncTask(SyncCommand.Delete, entry.Value, Path.Combine(sourcePath, entry.Value.Path));
        }

        // TODO: Implement status 
        _logger.LogInformation(
            "Sync labeling completed. Detected: Create/Update/Delete for: {Dectected} items, Completed:{Done} ",
            sourceMap.Count, destMap.Count);
    }

    private string GetRelativePath(string fullPath, string rootPath)
    {
        var rel = Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
        return rel == "." ? Path.GetFileName(fullPath) : rel;
    }
}