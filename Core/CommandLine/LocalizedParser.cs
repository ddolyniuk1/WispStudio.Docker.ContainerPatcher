using CommandLine;
using System.Reflection;
using WispStudios.Docker.ContainerPatcher.Core.Localization;

namespace WispStudios.Docker.ContainerPatcher.Core.CommandLine
{
    public static class LocalizedParser
    {
        public static Parser Create()
        {
            return new Parser(config =>
            {
                config.HelpWriter = Console.Out;
                config.AutoHelp = true;
                config.AutoVersion = true;
                config.EnableDashDash = true;
            });
        }

        public static ParserResult<T> ParseArguments<T>(string[] args) where T : class
        {
            var result = Create().ParseArguments<T>(args);
             
            foreach (var property in typeof(T).GetProperties())
            {
                var optionAttribute = property.GetCustomAttribute<OptionAttribute>();
                if (optionAttribute == null || !optionAttribute.HelpText.StartsWith("ResourceKey:")) continue;
                var resourceKey = optionAttribute.HelpText["ResourceKey:".Length..];
                 
                var helpTextField = typeof(OptionAttribute).GetField("HelpText",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (helpTextField != null)
                {
                    helpTextField.SetValue(optionAttribute, ResourceProvider.GetString(resourceKey));
                }
            }

            return result;
        }
    }
}
