using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Options;
using WispStudios.Docker.ContainerPatcher.Core.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Managers;

public class ProfileManager : IProfileManager
{
    private readonly ILogger _logger;
    private const string ProfilesDirectoryRelativePath = "./profiles";
    public string? ProfileDirectoryPath { get; }

    public ProfileManager(ILogger logger)
    {
        _logger = logger;
        if (TryInitializeProfileDirectoryPath(out var directory))
        {
            ProfileDirectoryPath = directory;
        }
    }

    /// <summary>
    /// Resolves a profile from disk, returns null if not found.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    private async Task<ExecutionProfile?> ResolveProfileAsync(string profile)
    {
        if (ProfileDirectoryPath == null)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_ProfileDirectoryIsNullError);
            return null;
        }

        try
        {
            var execProfilePath = Path.Combine(ProfileDirectoryPath, "./" + profile + ".json");
            if (!File.Exists(execProfilePath))
            {
                _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_ProfileDoesNotExistError, profile);
                return null;
            }

            var contents = await File.ReadAllTextAsync(execProfilePath);
            var optsProfile = JsonConvert.DeserializeObject<ExecutionProfile>(contents);

            _logger.LogInfo(Strings.ProfileManager_ResolveProfileAsync_ExecutingProfileMessage, profile);
             
            return optsProfile;
        }
        catch (NullReferenceException)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_NullReferenceException, profile);
        }
        catch (FileNotFoundException)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_FileNotFoundException, profile);
        }
        catch (IOException ex)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_IOException, ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_Error_UnauthorizedAccessException);
        }
        catch (JsonSerializationException ex)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_JsonSerializationException, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_JsonException, ex.Message);
        }
        catch (Exception)
        {
            _logger.LogError(Errors.ProfileManager_ResolveProfileAsync_GeneralException, profile);
        }

        return null;
    }

    public async Task<ImmutableArray<ExecutionProfile>> ParseLoadProfilesAsync(StartupOptions opts)
    {
        if (opts.LoadProfiles == null)
        {
            _logger.LogError(Errors.ProfileManager_ParseLoadProfilesAsync_LoadProfilesWasNullError);
            return default;
        }

        var list = new List<ExecutionProfile>();
        var profileNames = opts.LoadProfiles.Split(',');
        foreach (var profile in profileNames)
        {
            var resolved = await ResolveProfileAsync(profile);
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
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_ProfileDirectoryIsNullError);
            return;
        }

        if (opts.SaveProfile == null)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_ProfileWasNullError);
            return;
        }

        var match = ProfileNameRegex.Match(opts.SaveProfile);

        if (!match.Success)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_ProfileNameCannotBeParsed1);
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_ProfileNameCannotBeParsed2);
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
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_DirectoryNotFoundError, ProfileDirectoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_UnauthorizedAccessError);
        }
        catch (PathTooLongException)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_PathTooLongError);
        }
        catch (IOException ex)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_IOException, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_InvalidProfileNameError, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_FailedToSerializeProfileError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(Errors.ProfileManager_SaveProfileAsync_GeneralException, saveProfileName, ex.Message);
        }

        _logger.LogInfo(Strings.ProfileManager_SaveProfileAsync_SavedProfileMessage, saveProfileName);
    }

    public void PrintProfilesList()
    {
        if (ProfileDirectoryPath == null)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_ProfileDirectoryIsNullError);
            return;
        }

        try
        {
            var files = Directory.GetFiles(ProfileDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                _logger.LogError(Errors.ProfileManager_PrintProfilesList_NoProfilesFoundError);
                return;
            }

            var fileNames = files.Select(Path.GetFileNameWithoutExtension).OrderBy(t => t).ToList();
            _logger.LogInfo(Strings.ProfileManager_PrintProfilesList_CurrentProfilesMessage);
            foreach (var file in fileNames)
            {
                _logger.LogInfo(file ?? "unknown");
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_UnauthorizedAccessExceptionError);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_DirectoryNotFoundError, ProfileDirectoryPath);
        }
        catch (PathTooLongException)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_PathTooLongError);
        }
        catch (IOException ex)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_IOException, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(Errors.ProfileManager_PrintProfilesList_GeneralError, ex.Message);
        }
    }

    private bool TryInitializeProfileDirectoryPath(out string? profilesDir)
    {
        profilesDir = "<empty>";
        try
        {
            var executableDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(executableDir))
            {
                executableDir = AppContext.BaseDirectory;
            }

            profilesDir = Path.Combine(executableDir, ProfilesDirectoryRelativePath);

            Directory.CreateDirectory(profilesDir);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_DirectoryNotFoundError, profilesDir);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_UnauthorizedAccessError, profilesDir);
        }
        catch (PathTooLongException)
        {
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_PathTooLongError, profilesDir);
        }
        catch (NotSupportedException)
        {
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_NotSupportedError, profilesDir);
        }
        catch (IOException)
        {
            _logger.LogError(Errors.ProfileManager_TryInitializeProfileDirectoryPath_IOExceptionOccurred, profilesDir);
        }

        return false;
    }

}