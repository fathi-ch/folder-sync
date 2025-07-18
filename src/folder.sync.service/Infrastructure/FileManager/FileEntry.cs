namespace folder.sync.service.Infrastructure.FileManager;

public abstract record SyncEntry(string Path, DateTime LastModified);

public record FileEntry(string Path, long Size, DateTime LastModified, string Hash) : SyncEntry(Path, LastModified);

public record FolderEntry(string Path, DateTime LastModified) : SyncEntry(Path, LastModified);