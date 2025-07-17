namespace folder.sync.service.Infrastructure;

public interface IFolderStateCache
{
    Task<FolderState?> GetAsync(string path);
    Task SetAsync(string path, FolderState state);
}