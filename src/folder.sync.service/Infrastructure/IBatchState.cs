using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure;

public interface IBatchState
{
    void MarkSuccess(SyncTask task);
    void MarkFailure(SyncTask task, Exception ex);
    IReadOnlyList<SyncTask> GetFailedTasks();
    int SuccessCount { get; }
    int FailedCount { get; }
}