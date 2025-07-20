namespace folder.sync.service.Common;

public static class AppConstants
{
    public const string SerilogTemplateOutPut = "[{Timestamp:HH:mm:ss} {Level:u3}] {ShortSourceContext,-25} {Message:lj}{NewLine}{Exception}";
    public const string StateFilePath = ".cache/folder_state.json";
    public const int MaxBatchSize = 50; // Recommended stair step 10,20,50 
}