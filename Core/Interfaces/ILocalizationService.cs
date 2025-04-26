using System.Globalization;

namespace WispStudios.Docker.ContainerPatcher.Core.Interfaces
{
    public interface ILocalizationService
    { 
        string GetString(string key, params object[] items);
        void SetCulture(CultureInfo culture);
    }
}
