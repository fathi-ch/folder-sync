using folder.sync.service.Infrastructure.Commanding;

namespace folder.sync.service.Infrastructure.FileManager;

public record CreateFileOperation(string SourcePath, string DestinationPath) : IFileSystemOperation;