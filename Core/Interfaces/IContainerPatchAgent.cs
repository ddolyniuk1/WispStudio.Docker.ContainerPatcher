using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher.Core.Interfaces;

public interface IContainerPatchAgent
{
    public Task ExecuteAsync(ExecutionProfile profile);
}