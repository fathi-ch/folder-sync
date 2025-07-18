namespace folder.sync.service.Infrastructure.FileManager;

public interface IFileLoader
{
    IAsyncEnumerable<SyncEntry> LoadFilesAsync(string path);
}