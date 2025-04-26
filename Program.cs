using CommandLine;
using Unity;
using WispStudios.Docker.ContainerPatcher.Core.Extensions;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Options;

namespace WispStudios.Docker.ContainerPatcher
{
    internal class Program
    { 
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<StartupOptions>(args)
                .WithParsedAsync(async opts => await RunWithOptionsAsync(opts));
        }
         
        private static async Task RunWithOptionsAsync(StartupOptions opts)
        {
            var container = new UnityContainer()
                .RegisterContainerPatcher()
                .RegisterProfileManagement();
           
            var profileManager = container.Resolve<IProfileManager>(); 
            var containerPatchManager = container.Resolve<IContainerPatchManager>();

            switch (opts.StartupCommand)
            {
                case EStartupCommands.None:
                    await containerPatchManager.Run(opts);
                    break;
                case EStartupCommands.ListProfile:
                    profileManager.PrintProfilesList();
                    break;
                case EStartupCommands.LoadProfiles:
                    await containerPatchManager.Run(await profileManager.ParseLoadProfilesAsync(opts));
                    break;
                case EStartupCommands.SaveProfile:
                    await profileManager.SaveProfileAsync(opts);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}