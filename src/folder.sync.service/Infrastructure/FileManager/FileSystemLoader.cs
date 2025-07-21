using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace folder.sync.service.Infrastructure.FileManager;

public class FileSystemLoader : IFileLoader
{
    private readonly ILogger<FileSystemLoader> _logger;
    private readonly Dictionary<string, (long Length, DateTime LastWriteTimeUtc, string Hash)> _fileCache = new();

    public FileSystemLoader(ILogger<FileSystemLoader> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<SyncEntry> LoadFilesAsync(string rootPath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("[Init] Path does not exist: {Path}", rootPath);
            yield break;
        }

        var channel = Channel.CreateUnbounded<SyncEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        var producer = Task.Run(async () =>
        {
            try
            {
                var pending = new Stack<string>();
                pending.Push(rootPath);

                while (pending.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var currentDir = pending.Pop();
                    
                    _logger.LogDebug("[Scan] Entering: {Dir}", currentDir);
                    try
                    {
                        var dirInfo = new DirectoryInfo(currentDir);
                        var folderEntry = new FolderEntry(dirInfo.FullName, 0, dirInfo.LastWriteTimeUtc);
                        await channel.Writer.WriteAsync(folderEntry, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[DirError] {Dir}: {Msg}", currentDir, ex.Message);
                    }

                    var files = Array.Empty<string>();
                    string[] subdirs = Array.Empty<string>();

                    try
                    {
                        files = Directory.GetFiles(currentDir);
                        subdirs = Directory.GetDirectories(currentDir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[AccessDenied] {Dir}: {Msg}", currentDir, ex.Message);
                    }

                    foreach (var subdir in subdirs)
                        pending.Push(subdir);
                    
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount - 1, // Change this as needed to gain max performance
                        CancellationToken = cancellationToken
                    };

                    var entriesBag = new ConcurrentBag<SyncEntry>();

                    await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            if (!fi.Exists) return;

                            var fileKey = fi.FullName;
                            var currentMeta = (fi.Length, fi.LastWriteTimeUtc);

                            string hash;
                            if (_fileCache.TryGetValue(fileKey, out var cached) &&
                                cached.Length == fi.Length && cached.LastWriteTimeUtc == fi.LastWriteTimeUtc)
                            {
                                hash = cached.Hash;
                            }
                            else
                            {
                                hash = await ComputeFileHashWithMemoryMapAsync(fi.FullName, ct);
                                _fileCache[fileKey] = (fi.Length, fi.LastWriteTimeUtc, hash);
                            }

                            var entry = new FileEntry(fi.FullName, fi.Length, fi.LastWriteTimeUtc, hash);
                            entriesBag.Add(entry);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("[File] {File} skipped: {Message}", file, ex.Message);
                        }
                    });

                    foreach (var entry in entriesBag)
                    {
                        await channel.Writer.WriteAsync(entry, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);


        await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken)) yield return entry;

        await producer;
    }

    private async Task<string> ComputeFileHashWithMemoryMapAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            await using var accessor = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var sha256 = SHA256.Create();

            var hash = await sha256.ComputeHashAsync(accessor, cancellationToken);
            return Convert.ToHexString(hash);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[Hash-MMF] {FilePath} canceled", filePath);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("[Hash-MMF] {FilePath} failed: {Message}", filePath, ex.Message);
            return string.Empty;
        }
    }

}
