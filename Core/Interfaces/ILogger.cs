namespace WispStudios.Docker.ContainerPatcher.Core.Interfaces
{
    public interface ILogger
    {
        void LogInfo(string message, params object[] args);
        void LogWarn(string message, params object[] args);
        void LogError(string message, params object[] args); 
        void LogFatal(string message, params object[] args);
    }
}
