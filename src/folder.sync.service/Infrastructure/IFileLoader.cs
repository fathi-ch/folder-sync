namespace folder.sync.service.Infrastructure;

public interface IFileLoader
{
    IAsyncEnumerable<FileEntry> LoadFilesAsync(string path);
}