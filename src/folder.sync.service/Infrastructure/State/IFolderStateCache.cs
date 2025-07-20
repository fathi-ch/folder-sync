namespace folder.sync.service.Infrastructure.State;

public interface IFolderStateCache
{
    Task<FolderState?> GetAsync();
    Task SetAsync(FolderState state);
    Task FlushAsync();
}