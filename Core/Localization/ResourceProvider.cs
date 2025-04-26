using System.Globalization;
using System.Linq.Expressions;
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

        public static string GetString<T>(Expression<Func<T, string>> propertySelector, params object[] args)
        {
            if (propertySelector.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Expression must be a member access expression");

            var resourceKey = memberExpression.Member.Name;
            var value = GetString(resourceKey, CultureInfo.CurrentUICulture);

            if (args is { Length: > 0 } && !string.IsNullOrEmpty(value))
                return string.Format(value, args);

            return value;
        }

        public static IFormatProvider GetCulture()
        {
            return CultureInfo.CurrentUICulture.DateTimeFormat;
        }
    }
}
