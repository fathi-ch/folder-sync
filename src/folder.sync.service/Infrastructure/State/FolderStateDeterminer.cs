using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.State;

public static class FolderStateDeterminer
{
    public static async Task<FolderState> DetermineAsync(IAsyncEnumerable<SyncEntry> files)
    {
        var fileList = await files.ToListAsync();

        var maxTime = fileList.Max(f => f.LastModified);
        var count = fileList.Count(f => f.LastModified == maxTime);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var combined = string.Join('|',
            fileList
                .OfType<FileEntry>() // only hash real files
                .OrderBy(f => f.Path)
                .Select(f => $"{f.Path}|{f.Size}|{f.LastModified.Ticks}|{f.Hash}"));

        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        var hashStr = Convert.ToHexString(hashBytes);

        return new FolderState(maxTime, count, hashStr);
    }
}