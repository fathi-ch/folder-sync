namespace folder.sync.service.Infrastructure.FileManager;

public record DeleteFileSystemOperation(string Path) : IFileSystemOperation;