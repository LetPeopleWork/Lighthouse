using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class SystemInfoService : ISystemInfoService
    {
        private static readonly HashSet<string> PostgresSensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pwd", "user id", "uid", "username", "user name"
        };

        private readonly IConfiguration configuration;
        private readonly ILogConfiguration logConfiguration;

        public SystemInfoService(IConfiguration configuration, ILogConfiguration logConfiguration)
        {
            this.configuration = configuration;
            this.logConfiguration = logConfiguration;
        }

        public SystemInfo GetSystemInfo()
        {
            var dbProvider = configuration.GetValue<string>("Database:Provider") ?? "Unknown";
            var connectionString = configuration.GetValue<string>("Database:ConnectionString");

            return new SystemInfo(
                Os: RuntimeInformation.OSDescription.Trim(),
                Runtime: RuntimeInformation.FrameworkDescription,
                Architecture: RuntimeInformation.OSArchitecture.ToString(),
                ProcessId: Environment.ProcessId,
                DatabaseProvider: dbProvider,
                DatabaseConnection: GetSafeDatabaseConnection(dbProvider, connectionString),
                LogPath: logConfiguration.LogPath);
        }

        private static string? GetSafeDatabaseConnection(string provider, string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            var normalizedProvider = provider.ToLowerInvariant();

            if (normalizedProvider == "sqlite")
            {
                var builder = new SqliteConnectionStringBuilder(connectionString);
                return builder.DataSource;
            }

            if (normalizedProvider is "postgresql" or "postgres")
            {
                var safeParts = connectionString
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Where(part =>
                    {
                        var key = part.Split('=')[0].Trim();
                        return !PostgresSensitiveKeys.Contains(key);
                    });

                return string.Join(";", safeParts);
            }

            return null;
        }
    }
}
