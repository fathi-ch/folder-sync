namespace folder.sync.service.Infrastructure.FileManager;

public abstract record SyncEntry(string Path, DateTime LastModified)
{
    public string? RelativePath { get; init; }
}