using Lighthouse.Backend.Services.Interfaces;
using Serilog.Core;
using Serilog.Events;

namespace Lighthouse.Backend.Services.Implementation
{
    public class SerilogLogConfiguration : ILogConfiguration
    {
        private const string ConfigurationKeyPath = "Serilog:MinimumLevel:Default";
        private const string LogsNotFoundMessage = "Logs not Found";

        private readonly IConfiguration configuration;
        private readonly IConfigFileUpdater configFileUpdater;
        private readonly IFileSystemService fileSystem;
        private readonly string logFolderPath;

        public SerilogLogConfiguration(IConfiguration configuration, IConfigFileUpdater configFileUpdater, IFileSystemService fileSystem)
        {
            this.configuration = configuration;
            this.configFileUpdater = configFileUpdater;
            this.fileSystem = fileSystem;

            SupportedLogLevels = Enum.GetNames(typeof(LogEventLevel));

            var minimumLogLevel = configuration[ConfigurationKeyPath] ?? "";
            var currentLogLevel = ParseLogLevelFromString(minimumLogLevel);

            LoggingLevelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = currentLogLevel
            };

            logFolderPath = GetLogsFolderPath();
        }

        public LoggingLevelSwitch LoggingLevelSwitch { get; }

        public string CurrentLogLevel => LoggingLevelSwitch.MinimumLevel.ToString();

        public string[] SupportedLogLevels { get; }

        public string GetLogs()
        {
            if (string.IsNullOrEmpty(logFolderPath))
            {
                return LogsNotFoundMessage;
            }

            try
            {
                var logFiles = fileSystem.GetFiles(logFolderPath, "*.txt");

                if (logFiles.Length == 0)
                {
                    return LogsNotFoundMessage;
                }

                var newestFile = logFiles
                    .Select(file => new FileInfo(file))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .First();

                using (var stream = fileSystem.OpenFile(newestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return LogsNotFoundMessage;
            }
        }

        public void SetLogLevel(string level)
        {
            var newLogLevel = ParseLogLevelFromString(level);
            LoggingLevelSwitch.MinimumLevel = newLogLevel;
            configFileUpdater.UpdateConfigFile(ConfigurationKeyPath, newLogLevel.ToString());
        }

        private LogEventLevel ParseLogLevelFromString(string logLevel)
        {
            if (!SupportedLogLevels.Contains(logLevel))
            {
                return LogEventLevel.Information;
            }

            return (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logLevel);
        }

        private string GetLogsFolderPath()
        {
            var writeToSection = configuration.GetSection("Serilog:WriteTo").GetChildren();
            var fileSink = writeToSection
                .FirstOrDefault(sink => sink["Name"] == "File");

            if (fileSink == null)
            {
                return string.Empty;
            }

            var logFilePathPattern = fileSink["Args:path"];
            if (logFilePathPattern == null)
            {
                return string.Empty;
            }

            var path = Path.GetDirectoryName(Path.GetFullPath(logFilePathPattern));
            return path ?? string.Empty;
        }
    }
}