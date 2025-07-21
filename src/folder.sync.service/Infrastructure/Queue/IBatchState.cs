using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;

public interface IBatchState
{
    void MarkSuccess(SyncTask task);
    void MarkFailure(SyncTask task, Exception ex);
    void FlushFailures(SyncTask task);

    int GetSuccessCount();
    int GetFailureCount();
    int GetRetryCount();

    IReadOnlyList<SyncTask> SuccessfulTasks { get; }
    IReadOnlyList<(SyncTask Task, Exception Error)> FailedTasks { get; }
    IReadOnlyList<SyncTask> GetFailedTasks();
}
