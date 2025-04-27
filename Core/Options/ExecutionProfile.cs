using CommandLine;
using CommandLine.Text;
using WispStudios.Docker.ContainerPatcher.Core.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Options;

public class ExecutionProfile
{
    public override string ToString()
    {
        return Name ?? "<empty>";
    }

    [Option('i', "input", Required = false, HelpText = "InputHelp", ResourceType = typeof(Strings))]
    public string? Input { get; set; }

    [Option('o', "output", Required = false, HelpText = "OutputHelp", ResourceType = typeof(Strings))]
    public string? Output { get; set; }

    [Option('t', "target", Required = false, HelpText = "TargetHelp", ResourceType = typeof(Strings))]
    public string? Target { get; set; }

    [Option('h', "host", Required = false, HelpText = "HostHelp", ResourceType = typeof(Strings))]
    public string? Host { get; set; }

    [Option("replace-tag", Required = false, HelpText = "ReplaceTagHelp", ResourceType = typeof(Strings))]
    public string? ReplaceTag { get; set; }

    [Option("restore-tag", Required = false, HelpText = "RestoreTagHelp", ResourceType = typeof(Strings))]
    public string? RestoreTag { get; set; }

    public string? Name { get; set; }


    [Usage(ApplicationAlias = "WispStudios.Docker.ContainerPatcher")]
    public static IEnumerable<Example> Examples =>
        new List<Example>()
        {
            new("Example1Help",
                new ExecutionProfile
                {
                    Input = "C:\\path\\to\\file.txt,C:\\another\\directory",
                    Output = "/app/data",
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    ReplaceTag = "backup-20250426"
                }),
            new("Example2Help",
                new ExecutionProfile
                {
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    RestoreTag = "backup-20250426"
                })
        };

}