namespace folder.sync.service.Infrastructure.FileManager;

public record CreateFolderOperation(string Path) : IFileSystemOperation;