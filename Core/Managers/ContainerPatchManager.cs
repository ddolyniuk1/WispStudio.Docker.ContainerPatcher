using System.Collections.Immutable;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher.Core.Managers;

public class ContainerPatchManager : IContainerPatchManager
{
    private readonly IContainerPatchAgentFactory _agentFactory;

    public ContainerPatchManager(IContainerPatchAgentFactory? agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    public Task Run(IImmutableList<ExecutionProfile> profiles)
    {
        return Task.WhenAll(profiles.Select(t => _agentFactory.Create().ExecuteAsync(t)));
    }

    // entry point
    public Task Run(ExecutionProfile profile)
    {
        return _agentFactory.Create().ExecuteAsync(profile);
    }
}