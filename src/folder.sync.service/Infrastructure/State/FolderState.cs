namespace folder.sync.service.Infrastructure.State;

public record FolderState(DateTime MaxLastModified, int CountMax, string CombinedHash);