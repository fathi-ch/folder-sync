using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure;

public class BatchState : IBatchState
{
    private readonly HashSet<SyncTask> _successes = new();
    private readonly Dictionary<SyncTask, Exception> _failures = new();

    public void MarkSuccess(SyncTask task)
    {
        _failures.Remove(task);

        _successes.Add(task);
    }

    public void MarkFailure(SyncTask task, Exception ex)
    {
        if (_successes.Contains(task))
            return;

        _failures[task] = ex;
    }

    public IReadOnlyList<SyncTask> GetFailedTasks()
    {
        return _failures.Keys.ToList();
    }

    public void ClearFailures()
    {
        _failures.Clear();
    }

    public int SuccessCount => _successes.Count;
    public int FailedCount => _failures.Count;
    public IReadOnlyList<SyncTask> SuccessfulTasks => _successes.ToList();

    public IReadOnlyList<(SyncTask Task, Exception Error)> FailedTasks =>
        _failures.Select(kvp => (kvp.Key, kvp.Value)).ToList();
}