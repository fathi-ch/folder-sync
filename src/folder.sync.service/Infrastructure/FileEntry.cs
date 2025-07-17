namespace folder.sync.service.Infrastructure;

public record FileEntry(string Path, long Size, DateTime LastModified, string Hash);