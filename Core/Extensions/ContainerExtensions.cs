using Unity;
using WispStudios.Docker.ContainerPatcher.Core.Agents;
using WispStudios.Docker.ContainerPatcher.Core.Factories;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Loggers;
using WispStudios.Docker.ContainerPatcher.Core.Managers;

namespace WispStudios.Docker.ContainerPatcher.Core.Extensions
{
    public static class ContainerExtensions
    {
        public static IUnityContainer RegisterContainerPatcher(this IUnityContainer container)
        {
            container.RegisterType<IContainerPatchAgent, ContainerPatchAgent>();
            container.RegisterSingleton<IContainerPatchAgentFactory, ContainerPatchAgentFactory>();
            container.RegisterSingleton<IContainerPatchManager, ContainerPatchManager>();
            return container;
        }

        public static IUnityContainer RegisterProfileManagement(this IUnityContainer container)
        {
            container.RegisterSingleton<IProfileManager, ProfileManager>();
            return container;
        }
         
        public static IUnityContainer RegisterLoggers(this IUnityContainer container)
        {
            container.RegisterType<ILogger, ConsoleLogger>();
            return container;
        }
    }
}
