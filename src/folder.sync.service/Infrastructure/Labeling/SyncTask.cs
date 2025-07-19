using folder.sync.service.Infrastructure.FileManager;

namespace folder.sync.service.Infrastructure.Labeling;

public record SyncTask(SyncCommand Command, SyncEntry Entry, string SourcePath);