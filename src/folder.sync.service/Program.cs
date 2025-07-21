using System.Threading.Channels;
using Serilog;
using CommandLine;
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
using folder.sync.service.Common;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder();

builder.Services.Configure<HostOptions>(opts =>
{
    opts.ShutdownTimeout = TimeSpan.FromSeconds(10);
    opts.ServicesStartConcurrently = true;
    opts.ServicesStopConcurrently = true;
});

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
        syncOptions = parserResult.ExtractValidOptions();
    }
    else
    {
        syncOptions = GetSectionValue<FolderSyncServiceConfig>(builder.Configuration, FolderSyncServiceConfig.SectionName);
    }

    syncOptions.Validate();

    builder.Host.UseSerilog((ctx, sp, lc) => { ConfigureLogger(lc, ctx, syncOptions); });
    
    builder.Services.AddSingleton(syncOptions);
    builder.Services.AddHostedService<FolderSyncService>();
    builder.Services.AddHealthChecks();
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
  //  builder.Services.AddSingleton<ISyncTaskConsumer, SyncTaskConsumer>(); //Useful for quick testing

  builder.Services.AddOpenTelemetry()
      .ConfigureResource(r => r.AddService(
          serviceName: "FolderSyncService",
          serviceNamespace: "SyncSystem",
          serviceVersion: "1.0.0",
          autoGenerateServiceInstanceId: true))
      .WithTracing(b => b
          .AddSource("FolderSync.BatchConsumer"))
      .WithMetrics(b => b
          .AddMeter("FolderSync.Metrics")
          .AddRuntimeInstrumentation());

    var app = builder.Build();
    app.MapHealthChecks("/healthz");
    var log = app.Services.GetRequiredService<ILogger<Program>>();
    log.LogInformation("Service startup complete");
    app.Run();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex, "Fatal error occurred during startup or runtime");
}

return;

T GetSectionValue<T>(IConfiguration configuration, string sectionName) where T : class
{
    return configuration
               .GetSection(sectionName)
               .Get<T>()
           ?? throw new InvalidOperationException($"Missing section: {sectionName}");
}

void ConfigureLogger(LoggerConfiguration loggerConfiguration, HostBuilderContext hostBuilderContext,
    FolderSyncServiceConfig folderSyncServiceConfig)
{
    loggerConfiguration.ReadFrom.Configuration(hostBuilderContext.Configuration)
        .Enrich.With<ShortSourceContextEnricher>()
        .Enrich.WithThreadId()
        .Enrich.WithMachineName();

    loggerConfiguration.WriteTo.File(
        folderSyncServiceConfig.LogPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: AppConstants.SerilogTemplateOutPut
    );
}

