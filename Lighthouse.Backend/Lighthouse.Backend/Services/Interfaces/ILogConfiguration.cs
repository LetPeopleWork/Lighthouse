namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILogConfiguration
    {
        string CurrentLogLevel { get; }

        string[] SupportedLogLevels { get; }

        string? LogPath { get; }

        void SetLogLevel(string level);

        /// <summary>
        /// The newest log file. Asking for a tail returns roughly that many bytes from the end of it
        /// instead of all of it, which is what makes following the log affordable: the whole file is
        /// re-read on every ask, and a Debug-level instance writes one far too big to send repeatedly.
        /// </summary>
        string GetLogs(int? tailBytes = null);
    }
}
