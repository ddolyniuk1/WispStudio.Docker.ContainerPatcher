using Unity;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;

namespace WispStudios.Docker.ContainerPatcher.Core.Factories;

public class ContainerPatchAgentFactory : IContainerPatchAgentFactory
{
    private readonly IUnityContainer _container;

    public ContainerPatchAgentFactory(IUnityContainer container)
    {
        _container = container;
    }
    public IContainerPatchAgent Create()
    {
        return _container.Resolve<IContainerPatchAgent>();
    }
}