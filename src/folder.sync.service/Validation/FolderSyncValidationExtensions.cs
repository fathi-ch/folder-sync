using System.ComponentModel.DataAnnotations;
using folder.sync.service.Configuration;

namespace folder.sync.service.Validation;

public static class FolderSyncValidationExtensions
{
    public static void Validate(this FolderSyncServiceConfig config)
    {
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(config, context, results, true))
        {
            foreach (var error in results)
                Console.WriteLine(error.ErrorMessage);

            Environment.Exit(1);
        }

        config.ValidatePath("Source", config.SourcePath, true);
        config.ValidatePath("Replica", config.ReplicaPath, false);
        config.ValidateFilePath("LogPath", config.LogPath);
    }

    private static void ValidatePath(this FolderSyncServiceConfig _, string label, string path, bool mustExist)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is empty");

            var fullPath = Path.GetFullPath(path);

            if (mustExist && !Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"{label} directory does not exist: {fullPath}");

            if (!mustExist && !Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label} path is invalid: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ValidateFilePath(this FolderSyncServiceConfig _, string label, string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is empty");

            var fullPath = Path.GetFullPath(filePath);
            var dir = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException("Invalid directory from path");

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label} path is invalid: {ex.Message}");
            Environment.Exit(1);
        }
    }
}