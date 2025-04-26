using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher.Core.Managers;

public class ProfileManager : IProfileManager
{
    private const string ProfilesDirectoryRelativePath = "./profiles";
    public string? ProfileDirectoryPath { get; }

    public ProfileManager()
    {
        if (TryInitializeProfileDirectoryPath(out var directory))
        {
            ProfileDirectoryPath = directory;
        }
    }

    private async Task<ExecutionProfile?> ResolveProfileAsync(StartupOptions inputOpts, string profile)
    {
        if (ProfileDirectoryPath == null)
        {
            Console.WriteLine("The profiles directory failed to initialize and execution cannot continue.");
            return null;
        }

        try
        {
            var execProfilePath = Path.Combine(ProfileDirectoryPath, "./" + profile + ".json");
            if (!File.Exists(execProfilePath))
            {
                Console.WriteLine($"The profile '{inputOpts.LoadProfiles}' does not exist.");
                return null;
            }

            var contents = await File.ReadAllTextAsync(execProfilePath);
            var optsProfile = JsonConvert.DeserializeObject<ExecutionProfile>(contents);

            Console.WriteLine($"Executing profile '{profile}'.");

            if (optsProfile == null)
            {
                throw new NullReferenceException($"The profile '{profile}' is null, execution will not continue on this profile.");
            }

            return optsProfile;
        }
        catch (NullReferenceException nullReferenceException)
        {
            Console.WriteLine(nullReferenceException.Message);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Error: Profile file '{profile}.json' was deleted between check and load.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: Unable to read profile file. The file may be in use by another process. {ex.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: You don't have permission to read the profile file. Try running as administrator.");
        }
        catch (JsonSerializationException ex)
        {
            Console.WriteLine($"Error: Failed to deserialize profile data. The profile format may be incompatible. {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error: Profile file contains invalid JSON. {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to load profile '{profile}'. {ex.Message}");
        }

        return null;
    }

    public async Task<ImmutableArray<ExecutionProfile>> ParseLoadProfilesAsync(StartupOptions opts)
    {
        if (opts.LoadProfiles == null)
        {
            Console.WriteLine("The load profiles are null, something went wrong.");
            return default;
        }

        var list = new List<ExecutionProfile>();
        var profileNames = opts.LoadProfiles.Split(',');
        foreach (var profile in profileNames)
        {
            var resolved = await ResolveProfileAsync(opts, profile);
            if (resolved != null)
            {
                list.Add(resolved);
            }
        }

        return list.ToImmutableArray();
    }

    private static readonly Regex ProfileNameRegex = new(@"^(?<name>[\w-_.]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);

    public async Task SaveProfileAsync(StartupOptions opts)
    {
        if (ProfileDirectoryPath == null)
        {
            Console.WriteLine("The profiles directory failed to initialize and execution cannot continue.");
            return;
        }

        if (opts.SaveProfile == null)
        {
            Console.WriteLine("The save profile is null, something went wrong.");
            return;
        }

        var match = ProfileNameRegex.Match(opts.SaveProfile);

        if (!match.Success)
        {
            Console.WriteLine("The profile name cannot be parsed correctly.");
            Console.WriteLine("The profile name must be a valid file name, it cannot include spaces or periods, and must contain at least one character of alphanumeric, underscores, or hyphens");
            return;
        }

        var saveProfileName = match.Groups["name"].Value;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(ProfileDirectoryPath, "./" + saveProfileName + ".json"),
                JsonConvert.SerializeObject(opts, Formatting.Indented,
                    new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.None
                    }));
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Error: Directory '{ProfileDirectoryPath}' does not exist. Please create it first.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: You don't have permission to save the profile. Try running as administrator.");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine($"Error: The profile name or path is too long for your operating system.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: Unable to write profile file. The file may be in use by another process. {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: Invalid profile name. Profile names can't contain invalid characters. {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error: Failed to serialize profile data. {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to save profile '{saveProfileName}'. {ex.Message}");
        }
    }

    public void PrintProfilesList()
    {
        if (ProfileDirectoryPath == null)
        {
            Console.WriteLine("The profiles directory failed to initialize and execution cannot continue.");
            return;
        }

        try
        {
            var files = Directory.GetFiles(ProfileDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                Console.WriteLine("No profiles found.");
                return;
            }

            var fileNames = files.Select(Path.GetFileNameWithoutExtension).OrderBy(t => t).ToList();
            Console.WriteLine("Current Profiles:");
            foreach (var file in fileNames)
            {
                Console.WriteLine(file);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: You don't have permission to access the profiles directory. Try running as administrator.");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Error: The profiles directory '{ProfileDirectoryPath}' was not found or was removed during operation.");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine($"Error: The profiles directory path is too long for your operating system.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: Problem accessing profiles directory. {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to list profiles. {ex.Message}");
        }
    }

    private static bool TryInitializeProfileDirectoryPath(out string? profilesDir)
    {
        profilesDir = null;
        try
        {
            var executableDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);

            profilesDir = Path.Combine(executableDir ?? AppDomain.CurrentDomain.BaseDirectory, ProfilesDirectoryRelativePath);

            Directory.CreateDirectory(profilesDir);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Error: Directory '{profilesDir}' does not exist. Please create it first.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(
                $"Error: You don't have permission to create the profiles directory '{profilesDir}'. Try running as administrator.");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine(
                $"Error: The profiles directory '{profilesDir}' name or path is too long for your operating system.");
        }
        catch (NotSupportedException)
        {
            Console.WriteLine(
                $"Error: Creating the directory '{profilesDir}' is not supported.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: A problem occurred attempting to resolve the profile directory '{profilesDir}'");
        }

        return false;
    }

}