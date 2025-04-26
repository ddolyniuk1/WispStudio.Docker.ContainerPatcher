using System.Collections.Immutable;
using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher.Core.Interfaces;

public interface IProfileManager
{
    Task<ImmutableArray<ExecutionProfile>> ParseLoadProfilesAsync(StartupOptions profile);
    Task SaveProfileAsync(StartupOptions opts);
    void PrintProfilesList();
}