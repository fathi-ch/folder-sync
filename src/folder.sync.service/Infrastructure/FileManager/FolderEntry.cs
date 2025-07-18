namespace folder.sync.service.Infrastructure.FileManager;

public record FolderEntry(string Path, long Size, DateTime LastModified) : SyncEntry(Path, Size, LastModified);