using Serilog;
using CommandLine;
using CommandLine.Text;
using folder.sync.service;
using folder.sync.service.Logging;
using folder.sync.service.Validation;
using folder.sync.service.Configuration;


var builder = Host.CreateApplicationBuilder();

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
            .Get<FolderSyncServiceConfig>() ?? throw new InvalidOperationException($"Missing section: {FolderSyncServiceConfig.SectionName}");
    }
    syncOptions.Validate();

    builder.Logging.AddSerilog(logger, dispose: true);
    builder.Services.AddSingleton(syncOptions);
    builder.Services.AddHostedService<Worker>();

    var app = builder.Build();

    var log = app.Services.GetRequiredService<ILogger<Program>>();
    log.LogInformation("Service startup complete");    
    app.Run();

}
catch (Exception ex)
{
    logger.Fatal(ex, "Fatal error occurred during startup or runtime");
}