namespace folder.sync.service.Infrastructure.FileManager;

public class DummyLoaderTest : IFileLoader
{
    private readonly bool _useMockData;

    public DummyLoaderTest(bool useMockData)
    {
        _useMockData = useMockData;
    }


    public async IAsyncEnumerable<SyncEntry> LoadFilesAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            yield break;
        }

        if (!_useMockData) yield break;

        yield return new FileEntry(Path.Combine(path, "file1.txt"), 1024, DateTime.UtcNow, "hash1");
        await Task.Delay(1);

        yield return new FileEntry(Path.Combine(path, "file2.txt"), 2048, DateTime.UtcNow, "hash2");
        await Task.Delay(1);

        yield return new FileEntry(Path.Combine(path, "file3.txt"), 512, DateTime.UtcNow, "hash3");
        await Task.Delay(1);

        yield return new FolderEntry(Path.Combine(path, "SubDir"), DateTime.UtcNow);
    }
}