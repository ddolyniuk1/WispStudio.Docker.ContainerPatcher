using CommandLine;
using Newtonsoft.Json;

namespace WispStudios.Docker.ContainerPatcher.Core.Options;

public class StartupOptions : ExecutionProfile
{
    [JsonIgnore]
    public EStartupCommands StartupCommand
    {
        get
        {
            if (ListProfiles == true)
            {
                return EStartupCommands.ListProfile;
            }

            if (!string.IsNullOrWhiteSpace(SaveProfile))
            {
                return EStartupCommands.SaveProfile;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            // Disabled for readability
            if (!string.IsNullOrWhiteSpace(LoadProfiles))
            {
                return EStartupCommands.LoadProfiles;
            }

            return EStartupCommands.None;
        }
    }

    [Option("save-profile", Required = false, HelpText = "Save the parameters passed as a profile for future execution.")]
    [JsonIgnore]
    public string? SaveProfile { get; set; }

    [Option("load-profiles", Required = false, HelpText = "Load a comma separated list of existing profiles and executes them in order.")]
    [JsonIgnore]
    public string? LoadProfiles { get; set; }

    [Option("list-profiles", Required = false, HelpText = "List profiles that currently exist.")]
    [JsonIgnore]
    public bool? ListProfiles { get; set; }
}