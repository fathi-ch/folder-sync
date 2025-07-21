namespace folder.sync.service.Common;

public static class AppConstants
{
    //TODO: Constants to be moved to the config sections
    public const string SerilogTemplateOutPut = "[{Timestamp:HH:mm:ss} {Level:u3}] [{MachineName,-15}] [TID:{ThreadId,-3}] {SourceContext,-30} {Message:lj}{NewLine}{Exception}";
    public const string StateFilePath = ".cache/folder_state.json";
    public const int MaxBatchSize = 50; // Recommended stair steps 10,20,50 
    public const int MaxAttempts = 3;
    public const bool IsRetryEnabled = true;
    public const bool IsMetricsEnabled = true;
    public const bool IsTracingEnabled = true;
}