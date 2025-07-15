using Serilog.Core;
using Serilog.Events;

namespace folder.sync.service.Logging;

public class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = logEvent.Properties.TryGetValue("SourceContext", out var value)
            ? value.ToString().Trim('"')
            : "";

        var shortName = context?.Split('.').LastOrDefault() ?? context;

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ShortSourceContext", shortName));
    }
}