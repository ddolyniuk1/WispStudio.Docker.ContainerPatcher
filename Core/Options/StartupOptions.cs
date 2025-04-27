using CommandLine;
using Newtonsoft.Json;
using WispStudios.Docker.ContainerPatcher.Core.Resources;

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

            if (string.IsNullOrWhiteSpace(Input) || string.IsNullOrWhiteSpace(Output) ||
                string.IsNullOrWhiteSpace(Target))
            {
                return EStartupCommands.InvalidInput;
            }
             
            return EStartupCommands.None;
        }
    }

    [Option("save-profile", Required = false, HelpText = "SaveProfileHelp", ResourceType = typeof(Strings))]
    [JsonIgnore]
    public string? SaveProfile { get; set; }

    [Option("load-profiles", Required = false, HelpText = "LoadProfilesHelp", ResourceType = typeof(Strings))]
    [JsonIgnore]
    public string? LoadProfiles { get; set; }

    [Option("list-profiles", Required = false, HelpText = "ListProfilesHelp", ResourceType = typeof(Strings))]
    [JsonIgnore]
    public bool? ListProfiles { get; set; }

    [Option("language", Required = false, HelpText = "LanguageHelp", ResourceType = typeof(Strings))]
    [JsonIgnore]
    public string Language { get; set; }
}