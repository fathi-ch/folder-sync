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

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<ShortSourceContextEnricher>()
    .CreateLogger();

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

                    var helpText = HelpText.AutoBuild(parserResult, h => h, e => e);
                    Console.WriteLine(helpText);

                    Environment.Exit(1);
                }

                Console.WriteLine(
                    $"Running sync: {options.SourcePath} -> {options.ReplicaPath} every {options.IntervalInSec}s");
            })
            .WithNotParsed(errors =>
            {
                Console.WriteLine(HelpText.AutoBuild(parserResult, h => h, e => e));
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

    builder.Logging.AddSerilog(logger, true);
    builder.Services.AddSingleton(syncOptions);
    builder.Services.AddHostedService<FolderSyncService>();
    builder.Services.AddSingleton<IFolderSyncPipeline, FolderSyncPipeline>();
    builder.Services.AddSingleton<IFileLoader, FileSystemLoader>();
    builder.Services.AddSingleton<ISyncLabeler, SyncLabeler>();
    builder.Services.AddSingleton<IFolderStateCache, FileFolderStateCache>();
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
    logger.Fatal(ex, "Fatal error occurred during startup or runtime");
}