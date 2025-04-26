using System.Globalization;
using System.Reflection;
using System.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Localization
{
    public static class ResourceProvider
    { 
        private static CultureInfo _currentCulture;

        static ResourceProvider()
        {
            StringResourceManager = new ResourceManager(
                "WispStudios.Docker.ContainerPatcher.Core.Resources.Strings",
                Assembly.GetExecutingAssembly());
            ErrorResourceManager = new ResourceManager(
                "WispStudios.Docker.ContainerPatcher.Core.Resources.Errors",
                Assembly.GetExecutingAssembly());
            _currentCulture = CultureInfo.CurrentUICulture; 
        }

        public static ResourceManager ErrorResourceManager { get; set; }

        public static ResourceManager StringResourceManager { get; set; }

        public static void SetCulture(string cultureName)
        {
            _currentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture = _currentCulture;
        }

        public static string? GetString(string resourceKey)
        {
            return StringResourceManager.GetString(resourceKey);
        }

        public static string? GetError(string resourceKey)
        {
            return ErrorResourceManager.GetString(resourceKey);
        }
    }
}
