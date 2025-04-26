using CommandLine;
using CommandLine.Text;

namespace WispStudios.Docker.ContainerPatcher.Core.Options;

public class ExecutionProfile
{
    [Option('i', "input", Required = false, HelpText = "CSV list of files or directories to transfer")]
    public string? Input { get; set; }

    [Option('o', "output", Required = false, HelpText = "Target path in the container")]
    public string? Output { get; set; }

    [Option('t', "target", Required = true, HelpText = "Target container name or ID")]
    public string? Target { get; set; }

    [Option('h', "host", Required = true, HelpText = "Target host address")]
    public string? Host { get; set; }

    [Option("replace-tag", Required = false, HelpText = "Backup tag to save the original container state")]
    public string? ReplaceTag { get; set; }

    [Option("restore-tag", Required = false, HelpText = "Tag to restore (reverting to the backup version)")]
    public string? RestoreTag { get; set; }

    [Usage(ApplicationAlias = "WispStudios.Docker.ContainerPatcher")]
    public static IEnumerable<Example> Examples =>
        new List<Example>()
        {
            new("Backup and modify a container",
                new ExecutionProfile
                {
                    Input = "C:\\path\\to\\file.txt,C:\\another\\directory",
                    Output = "/app/data",
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    ReplaceTag = "backup-20250426"
                }),
            new("Restore a container from backup",
                new ExecutionProfile
                {
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    RestoreTag = "backup-20250426"
                })
        };

}