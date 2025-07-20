using System.Threading.Channels;
using Serilog;
using CommandLine;
using CommandLine.Text;
using folder.sync.service.Logging;
using folder.sync.service.Validation;
using folder.sync.service.Configuration;
using folder.sync.service.Infrastructure;
using folder.sync.service.Infrastructure.Commanding;
using folder.sync.service.Infrastructure.FileManager;
using folder.sync.service.Infrastructure.FileManager.Handler;
using folder.sync.service.Infrastructure.Labeling;
using folder.sync.service.Infrastructure.Pipeline;
using folder.sync.service.Infrastructure.Queue;
using folder.sync.service.Infrastructure.State;
using folder.sync.service.Service;

var builder = Host.CreateApplicationBuilder();

builder.Services.Configure<HostOptions>(opts =>
{
    opts.ShutdownTimeout = TimeSpan.FromSeconds(10);
    opts.ServicesStartConcurrently = true;
    opts.ServicesStopConcurrently = true;
});

builder.Configuration
    .AddJsonFile("appsetings.json", true, true)
    .AddCommandLine(args);

builder.Logging.ClearProviders();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    FolderSyncServiceConfig syncOptions;

    if (args.Length > 0)
    {
        var parserResult = Parser.Default.ParseArguments<FolderSyncServiceConfig>(args);

        parserResult
            .WithParsed(options =>
            {
                if (options.IntervalInSec < 0)
                {
                    Console.WriteLine(" Invalid interval. Must be greater than 0.\n");
                    Console.WriteLine(HelpText.AutoBuild(parserResult, _ => _, _ => _).ToString());
                    Environment.Exit(1);
                }
                Console.WriteLine($"Running sync: {options.SourcePath} -> {options.ReplicaPath} every {options.IntervalInSec}s");
            })
            .WithNotParsed(errors => {
                Environment.Exit(1);
            });
        syncOptions = parserResult.Value;
    }
    else
    {
        syncOptions = builder.Configuration
                          .GetSection(FolderSyncServiceConfig.SectionName)
                          .Get<FolderSyncServiceConfig>() ??
                      throw new InvalidOperationException($"Missing section: {FolderSyncServiceConfig.SectionName}");
    }

    syncOptions.Validate();
    
    Log.Logger = new LoggerConfiguration()
    .Enrich.With<ShortSourceContextEnricher>()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        path:syncOptions.LogPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate:  "[{Timestamp:HH:mm:ss} {Level:u3}] {ShortSourceContext,-25} {Message:lj}{NewLine}{Exception}"
        )
    .CreateLogger();

    builder.Logging.AddSerilog(Log.Logger, dispose: true);
    builder.Services.AddSingleton(syncOptions);
    builder.Services.AddHostedService<FolderSyncService>();
    builder.Services.AddSingleton<IFolderSyncPipeline, FolderSyncPipeline>();
    builder.Services.AddSingleton<IFileLoader, FileSystemLoader>();
    builder.Services.AddSingleton<ISyncLabeler, SyncLabeler>();
    builder.Services.AddSingleton<IFolderStateCache, FileFolderStateCache>();

    builder.Services.AddSingleton<FileSystemOperationDispatcher>();
    builder.Services.AddScoped<IFileOperationHandler<CreateFileOperation>, CreateFileHandler>();
    builder.Services.AddScoped<IFileOperationHandler<CreateFolderOperation>, CreateFolderHandler>();
    builder.Services.AddScoped<IFileOperationHandler<UpdateFileOperation>, UpdateFileHandler>();
    builder.Services.AddScoped<IFileOperationHandler<DeleteFileSystemOperation>, DeleteFileSystemHandler>();

    builder.Services.AddSingleton<ISyncCommandFactory, SyncCommandFactory>();
    builder.Services.AddSingleton(Channel.CreateUnbounded<SyncTask>());
    builder.Services.AddSingleton<ISyncTaskProducer, SyncTaskProducer>();
    builder.Services.AddSingleton<ICommandExecutor, ConcurrentCommandExecutor>(
        _ => new ConcurrentCommandExecutor(4));
    builder.Services.AddSingleton<IBatchState, BatchState>();
    builder.Services.AddSingleton<ISyncTaskConsumer, BatchSyncTaskConsumer>();


    var app = builder.Build();
    var log = app.Services.GetRequiredService<ILogger<Program>>();
    log.LogInformation("Service startup complete");
    app.Run();
}
catch (Exception ex)
{
   Log.Logger.Fatal(ex, "Fatal error occurred during startup or runtime");
}