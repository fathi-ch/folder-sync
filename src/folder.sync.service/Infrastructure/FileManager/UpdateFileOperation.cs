namespace folder.sync.service.Infrastructure.FileManager;

public record UpdateFileOperation(string SourcePath, string DestinationPath) : IFileSystemOperation;