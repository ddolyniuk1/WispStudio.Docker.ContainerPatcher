using System.Globalization;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private enum ELogType
        {
            Info, Warn, Error, Fatal
        }

        public void LogInfo(string message, params object[] args)
        {
            Log(ELogType.Info, message, args);
        }

        public void LogWarn(string message, params object[] args)
        {
            Log(ELogType.Warn, message, args);
        }

        public void LogError(string message, params object[] args)
        {
            Log(ELogType.Error, message, args);
        }

        public void LogFatal(string message, params object[] args)
        {
            Log(ELogType.Fatal, message, args);
        }

        private static string LogTypeToString(ELogType type)
        {
            return type switch
            {
                ELogType.Info => Strings.LogType_Info,
                ELogType.Warn => Strings.LogType_Warn,
                ELogType.Error => Strings.LogType_Error,
                ELogType.Fatal => Strings.LogType_Fatal,
                _ => type.ToString()
            };
        }

        private static void Log(ELogType logType, string message, params object[] args)
        {
            // ReSharper disable once LocalizableElement
            // We do not need to localize this
            var format =
                $"[{DateTimeOffset.Now.ToString(CultureInfo.CurrentUICulture.DateTimeFormat)}][{LogTypeToString(logType).ToUpper()}]: {string.Format(message, args)}";
            Console.WriteLine(format);
        }
    }
}
