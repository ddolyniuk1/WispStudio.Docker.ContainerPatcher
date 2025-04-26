using System.Globalization;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Localization;

namespace WispStudios.Docker.ContainerPatcher.Core.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private enum ELogType
        {
            Info, Warn, Error, Fatal
        }

        public void LogInfo(string message)
        {
            Log(ELogType.Info, message);
        }

        public void LogWarn(string message)
        {
            Log(ELogType.Warn, message);
        }

        public void LogError(string message)
        {
            Log(ELogType.Error, message);
        }

        public void LogFatal(string message)
        {
            Log(ELogType.Fatal, message);
        }

        private static string LogTypeToString(ELogType type)
        {
            return ResourceProvider.GetString("LogType_" + type);
        }

        private static void Log(ELogType logType, string message)
        {
            // ReSharper disable once LocalizableElement
            // We do not need to localize this
            var format =
                $"[{DateTimeOffset.Now.ToString(CultureInfo.CurrentUICulture.DateTimeFormat)}][{LogTypeToString(logType).ToUpper()}]: {message}";
            Console.WriteLine(format);
        }
    }
}
