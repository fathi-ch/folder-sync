using System.ComponentModel.DataAnnotations;
using CommandLine;

namespace folder.sync.service.Configuration;

[Verb("sync", HelpText = "Synchronize folder contents")]
public class FolderSyncServiceConfig
{
    public const string SectionName = "FolderSyncServiceConfig";

    [Option('s', "SourcePath", Required = true, HelpText = "Source folder path")]
    public string SourcePath { get; set; }

    [Option('r', "ReplicaPath", Required = true, HelpText = "Replica folder path")]
    public string ReplicaPath { get; set; }

    [Option('l', "LogPath", Required = true, HelpText = "Logs  path")]
    public string LogPath { get; set; }

    [Option('i', "IntervalInSec", Required = true, HelpText = "Interval in seconds must be greater than 0")]
    [Range(0, int.MaxValue, ErrorMessage = "Interval must be greater than 0")]
    public int IntervalInSec { get; set; } = 1;
}