using System.Collections.Concurrent;
using folder.sync.service.Infrastructure;
using folder.sync.service.Infrastructure.Labeling;

namespace folder.sync.service.Infrastructure.Queue;


public class BatchState : IBatchState
{
    private readonly ConcurrentDictionary<string, SyncTask> _successes = new();
    private readonly ConcurrentDictionary<string, List<Exception>> _failures = new();
    private readonly ConcurrentDictionary<string, SyncTask> _finalFailures = new();

    private readonly ILogger<BatchState> _logger;

    public BatchState(ILogger<BatchState> logger)
    {
        _logger = logger;
    }

    public void MarkSuccess(SyncTask task)
    {
        _failures.TryRemove(task.Entry.Path, out _);
        _finalFailures.TryRemove(task.Entry.Path, out _);
        _successes[task.Entry.Path] = task;

        _logger.LogDebug("[BatchState] Marked SUCCESS for {Path}", task.Entry.Path);
    }

    public void MarkFailure(SyncTask task, Exception ex)
    {
        _successes.TryRemove(task.Entry.Path, out _);

        _failures.AddOrUpdate(
            task.Entry.Path,
            _ => new List<Exception> { ex },
            (_, list) => { list.Add(ex); return list; });

        _finalFailures.TryAdd(task.Entry.Path, task); // ✅ ensures GetFailureCount() sees it

        _logger.LogDebug("[BatchState] Marked FAILURE for {Path}. Total errors: {Count}",
            task.Entry.Path,
            _failures.TryGetValue(task.Entry.Path, out var l) ? l.Count : 0);
    }

    public void FlushFailures(SyncTask task)
    {
        _failures.TryRemove(task.Entry.Path, out _);
        _finalFailures[task.Entry.Path] = task;

        _logger.LogDebug("[BatchState] FLUSHED failure for {Path}", task.Entry.Path);
    }

    public int GetFailureCount(SyncTask task) =>
        _failures.TryGetValue(task.Entry.Path, out var list) ? list.Count : 0;

    public int GetSuccessCount() => _successes.Count;

    public int GetFailureCount() => _finalFailures.Count;

    public int GetRetryCount() => _failures.Count(kv => kv.Value.Count > 1);

    public IReadOnlyList<SyncTask> SuccessfulTasks => _successes.Values.ToList();

    public IReadOnlyList<(SyncTask Task, Exception Error)> FailedTasks =>
        _finalFailures.SelectMany(kvp =>
            _failures.TryGetValue(kvp.Key, out var exList)
                ? exList.Select(ex => (kvp.Value, ex))
                : Enumerable.Empty<(SyncTask, Exception)>()).ToList();

    public IReadOnlyList<SyncTask> GetFailedTasks() => _finalFailures.Values.ToList();
}
