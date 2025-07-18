namespace folder.sync.service.Infrastructure.FileManager;

public record FolderEntry(string Path, DateTime LastModified) : SyncEntry(Path, LastModified);