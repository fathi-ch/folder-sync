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

            if (!mustExist && !Directory.Exists(fullPath))
            {
                Console.WriteLine($"{label} directory does not exist. Creating: {fullPath}");
                Directory.CreateDirectory(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label} path is invalid: {ex.Message}");
            Environment.Exit(1);
        }
    }
}