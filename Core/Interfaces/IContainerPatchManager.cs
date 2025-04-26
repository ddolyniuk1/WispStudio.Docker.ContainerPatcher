using System.Collections.Immutable;
using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher.Core.Interfaces;

public interface IContainerPatchManager
{
    Task Run(ExecutionProfile profile);
    Task Run(IImmutableList<ExecutionProfile> profiles);
}