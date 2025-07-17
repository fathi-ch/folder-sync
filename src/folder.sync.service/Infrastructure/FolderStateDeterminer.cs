using System.Linq;

namespace folder.sync.service.Infrastructure;

public static class FolderStateDeterminer
{
    public static async Task<FolderState> DetermineAsync(IAsyncEnumerable<FileEntry> files)
    {
        var maxTime = await files.MaxAsync(f => f.LastModified);
        var count = await files.CountAsync(f => f.LastModified == maxTime);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var combined = string.Join('|',
            files.OrderBy(f => f.Path)
                .Select(f => $"{f.Path}|{f.Size}|{f.LastModified.Ticks}|{f.Hash}"));

        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        var hashStr = Convert.ToHexString(hashBytes);

        return new FolderState(maxTime, count, hashStr);
    }
}