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

            SupportedLogLevels = Enum.GetNames<LogEventLevel>();

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

        public string? LogPath => string.IsNullOrEmpty(logFolderPath) ? null : logFolderPath;

        public string GetLogs(int? tailBytes = null)
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

                using var stream = fileSystem.OpenFile(newestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (tailBytes is null || stream.Length <= tailBytes.Value)
                {
                    using var whole = new StreamReader(stream);
                    return whole.ReadToEnd();
                }

                // Reading the byte before the tail is what tells a tail that already begins on a line
                // of its own from one that begins half-way through a line. Without it, a tail that
                // happened to land exactly on a line start would have that whole line trimmed off it.
                var startOfTail = stream.Length - tailBytes.Value;
                stream.Seek(startOfTail - 1, SeekOrigin.Begin);
                var landedOnALineBoundary = stream.ReadByte() == '\n';

                using var reader = new StreamReader(stream);
                var tail = reader.ReadToEnd();

                return landedOnALineBoundary ? tail : FromTheFirstWholeLineIn(tail);
            }
            catch (Exception)
            {
                return LogsNotFoundMessage;
            }
        }

        /// <summary>
        /// A byte offset from the end of the file lands wherever it lands: part-way through a line, and
        /// possibly part-way through a multi-byte character. Everything before the first newline is that
        /// fragment, so dropping it cures both at once. A tail holding no newline at all is one enormous
        /// line, and half of it is still better than none.
        /// </summary>
        private static string FromTheFirstWholeLineIn(string tail)
        {
            var firstLineBreak = tail.IndexOf('\n');

            return firstLineBreak < 0 ? tail : tail[(firstLineBreak + 1)..];
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

            return Enum.Parse<LogEventLevel>(logLevel);
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