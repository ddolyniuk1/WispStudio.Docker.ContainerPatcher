using CommandLine;
using CommandLine.Text;

namespace WispStudios.Docker.ContainerPatcher.Core.Options;

public class ExecutionProfile
{
    public override string ToString()
    {
        return Name ?? "<empty>";
    }

    [Option('i', "input", Required = false, HelpText = "ResourceKey:InputHelp")]
    public string? Input { get; set; }

    [Option('o', "output", Required = false, HelpText = "ResourceKey:OutputHelp")]
    public string? Output { get; set; }

    [Option('t', "target", Required = false, HelpText = "ResourceKey:TargetHelp")]
    public string? Target { get; set; }

    [Option('h', "host", Required = false, HelpText = "ResourceKey:HostHelp")]
    public string? Host { get; set; }

    [Option("replace-tag", Required = false, HelpText = "ResourceKey:ReplaceTagHelp")]
    public string? ReplaceTag { get; set; }

    [Option("restore-tag", Required = false, HelpText = "ResourceKey:RestoreTagHelp")]
    public string? RestoreTag { get; set; }

    public string? Name { get; set; }


    [Usage(ApplicationAlias = "WispStudios.Docker.ContainerPatcher")]
    public static IEnumerable<Example> Examples =>
        new List<Example>()
        {
            new("ResourceKey:Example1Help",
                new ExecutionProfile
                {
                    Input = "C:\\path\\to\\file.txt,C:\\another\\directory",
                    Output = "/app/data",
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    ReplaceTag = "backup-20250426"
                }),
            new("ResourceKey:Example2Help",
                new ExecutionProfile
                {
                    Target = "my-container",
                    Host = "tcp://my-host:port",
                    RestoreTag = "backup-20250426"
                })
        };

}