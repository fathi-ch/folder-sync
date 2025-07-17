namespace folder.sync.service.Infrastructure;

public record FolderState(DateTime MaxLastModified, int CountMax, string CombinedHash);