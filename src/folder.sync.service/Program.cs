using folder.sync.service;
using folder.sync.service.Logging;
using Serilog;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
   // .Enrich.FromLogContext()
    .Enrich.With<ShortSourceContextEnricher>()
    .CreateLogger();

builder.Logging.AddSerilog(logger, dispose: true);

builder.Services.AddHostedService<Worker>();

try
{
    var app = builder.Build();

    var log = app.Services.GetRequiredService<ILogger<Program>>();
    log.LogInformation("Application startup complete");

    app.Run();

    log.LogInformation("Application shut down cleanly");
}
catch (Exception ex)
{
    logger.Fatal(ex, "Fatal error occurred during startup or runtime");
}