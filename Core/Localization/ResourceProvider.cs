using System.Globalization;
using System.Reflection;
using System.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Localization
{
    public static class ResourceProvider
    {
        private static readonly ResourceManager StringResourceManager;
        private static readonly ResourceManager ErrorResourceManager;
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

        public static void SetCulture(string cultureName)
        {
            _currentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture = _currentCulture;
        }

        public static string GetString(string key, params object[] args)
        {
            return string.Format(StringResourceManager.GetString(key, _currentCulture) ?? ErrorResourceManager.GetString(key, _currentCulture) ?? key, args);
        } 
    }
}
