namespace folder.sync.service.Infrastructure.FileManager;

public record FileEntry(string Path, long Size, DateTime LastModified, string Hash)
    : SyncEntry(Path, Size, LastModified);