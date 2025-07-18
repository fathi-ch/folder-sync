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
    
    public async IAsyncEnumerable<SyncTask> ProcessAsync(string sourcePath, IAsyncEnumerable<SyncEntry> sourceFiles, string replicaPath, IAsyncEnumerable<SyncEntry> replicaFiles, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sourceMap = new Dictionary<string, SyncEntry>();
        try
        {
            await foreach (var src in sourceFiles.WithCancellation(cancellationToken))
                sourceMap[GetRelativePath(src.Path, sourcePath)] = src;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load source files: {ex.Message}", ex.Message);
            yield break;
        }

        var destMap = new Dictionary<string, SyncEntry>();
        try
        {
            await foreach (var dst in replicaFiles.WithCancellation(cancellationToken))
                destMap[GetRelativePath(dst.Path, replicaPath)] = dst;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load replica files: {ex.Message}", ex.Message);
            yield break;
        }

        foreach (var folder in sourceMap.Values
                     .OfType<FolderEntry>()
                     .Where(f => !destMap.ContainsKey(Path.GetRelativePath(sourcePath, f.Path)))
                     .OrderBy(f => f.Path.Count(c => c == Path.DirectorySeparatorChar)))
            yield return new SyncTask(SyncCommand.Create, folder);

        foreach (var (path, srcEntry) in sourceMap)
            if (srcEntry is FileEntry srcFile)
            {
                if (!destMap.TryGetValue(path, out var dstEntry))
                    yield return new SyncTask(SyncCommand.Create, srcFile);
                else if (dstEntry is FileEntry dstFile)
                    if (srcFile.Hash != dstFile.Hash || srcFile.LastModified > dstFile.LastModified)
                        yield return new SyncTask(SyncCommand.Update, srcFile);
            }

        foreach (var entry in destMap
                     .Where(kvp => !sourceMap.ContainsKey(kvp.Key))
                     .OrderByDescending(kvp => kvp.Value.Path.Count(c => c == Path.DirectorySeparatorChar)))
            yield return new SyncTask(SyncCommand.Delete, entry.Value);
    }

    private string GetRelativePath(string fullPath, string rootPath)
    {
        return Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
    }
}