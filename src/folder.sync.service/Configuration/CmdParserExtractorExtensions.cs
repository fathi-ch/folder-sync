using CommandLine;
using CommandLine.Text;

namespace folder.sync.service.Configuration;

public static class CmdParserExtractorExtensions
{
    public static FolderSyncServiceConfig ExtractValidOptions(this ParserResult<FolderSyncServiceConfig> result)
    {
        result.WithParsed(options =>
            {
                if (options.IntervalInSec < 0)
                {
                    Console.WriteLine(" Invalid interval. Must be greater than 0.\n");
                    Console.WriteLine(HelpText.AutoBuild(result, _ => _, _ => _));
                    Environment.Exit(1);
                }

                Console.WriteLine($"Running sync: {options.SourcePath} -> {options.ReplicaPath} every {options.IntervalInSec}s");
            })
            .WithNotParsed(errors =>
            {
                Console.WriteLine(HelpText.AutoBuild(result, _ => _, _ => _));
                Environment.Exit(1);
            });

        return result.Value!;
    }
}