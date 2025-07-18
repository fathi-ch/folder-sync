using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure;

public class BatchState : IBatchState
{
    private readonly List<SyncTask> _successes = new();
    private readonly List<(SyncTask Task, Exception Error)> _failures = new();

    public void MarkSuccess(SyncTask task)
    {
        _successes.Add(task);
    }

    public void MarkFailure(SyncTask task, Exception ex)
    {
        _failures.Add((task, ex));
    }

    public IReadOnlyList<SyncTask> GetFailedTasks()
    {
        return _failures.Select(f => f.Task).ToList();
    }

    public void ClearFailures()
    {
        _failures.Clear();
    }

    public int SuccessCount => _successes.Count;
    public int FailedCount => _failures.Count;
    public IReadOnlyList<SyncTask> SuccessfulTasks => _successes;
    public IReadOnlyList<(SyncTask Task, Exception Error)> FailedTasks => _failures;
}