using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Labeling;

public class DummySyncLabeler(ILogger<DummySyncLabeler> logger) : ISyncLabeler
{
    private string GetRelativePath(string fullPath, string rootPath)
    {
        return Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
    }

    public async IAsyncEnumerable<SyncTask> ProcessAsync(
        string sourcePath,
        IAsyncEnumerable<SyncEntry> sourceFiles,
        string replicaPath,
        IAsyncEnumerable<SyncEntry> replicaFiles,
        CancellationToken cancellationToken)
    {
        var source = await sourceFiles.ToListAsync(cancellationToken);
        var sourceMap = source.ToDictionary(f => GetRelativePath(f.Path, sourcePath));

        var dest = await replicaFiles.ToListAsync(cancellationToken);
        var destMap = dest.ToDictionary(f => GetRelativePath(f.Path, replicaPath));

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
}