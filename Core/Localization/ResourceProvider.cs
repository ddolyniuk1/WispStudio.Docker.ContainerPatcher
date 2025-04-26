using System.Globalization;

namespace WispStudios.Docker.ContainerPatcher.Core.Localization
{
    public static class ResourceProvider
    { 
        private static CultureInfo _currentCulture;

        static ResourceProvider()
        { 
            _currentCulture = CultureInfo.CurrentUICulture;
        }

        public static void SetCulture(string cultureName)
        {
            _currentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture = _currentCulture;
        }
    }
}
