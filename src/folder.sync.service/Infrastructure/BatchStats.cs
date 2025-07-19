namespace folder.sync.service.Infrastructure;

public record BatchStats(
    int Count,
    long TotalSize,
    double AvgSize,
    string? SlowestFile,
    double? SlowestMs,
    string? FastestFile,
    double? FastestMs
)
{
    public override string ToString()
    {
        string FormatSize(long bytes)
        {
            return bytes switch
            {
                >= 1L << 30 => $"{bytes / (1 << 30):0.##} GiB",
                >= 1L << 20 => $"{bytes / (1 << 20):0.##} MiB",
                _ => $"{bytes} bytes"
            };
        }

        return $"Batch Summary: {Count} files, Total {FormatSize(TotalSize)}, Avg: {FormatSize((long)AvgSize)}/file, " +
               $"Fastest: {FastestFile} ({FastestMs:N1} ms), Slowest: {SlowestFile} ({SlowestMs:N1} ms)";
    }
}