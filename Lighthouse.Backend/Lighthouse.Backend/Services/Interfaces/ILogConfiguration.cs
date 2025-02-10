namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILogConfiguration
    {
        string CurrentLogLevel { get; }

        string[] SupportedLogLevels { get; }

        void SetLogLevel(string level);

        string GetLogs();
    }
}
