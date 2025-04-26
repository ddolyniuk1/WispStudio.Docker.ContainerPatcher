using Unity;
using WispStudios.Docker.ContainerPatcher.Core.Factories;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Managers;

namespace WispStudios.Docker.ContainerPatcher.Core.Extensions
{
    public static class ContainerExtensions
    {
        public static IUnityContainer RegisterContainerPatcher(this IUnityContainer container)
        {
            container.RegisterSingleton<IContainerPatchAgentFactory, ContainerPatchAgentFactory>();
            container.RegisterSingleton<IContainerPatchManager, ContainerPatchManager>();
            return container;
        }

        public static IUnityContainer RegisterProfileManagement(this IUnityContainer container)
        {
            container.RegisterSingleton<IProfileManager, ProfileManager>();
            return container;
        }
    }
}
