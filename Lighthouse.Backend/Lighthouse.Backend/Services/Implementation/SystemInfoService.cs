using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
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
        private readonly IServiceConfig serviceConfig;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<SystemInfoService> logger;

        public SystemInfoService(IConfiguration configuration, ILogConfiguration logConfiguration, IServiceConfig serviceConfig, IServiceScopeFactory scopeFactory, ILogger<SystemInfoService> logger)
        {
            this.configuration = configuration;
            this.logConfiguration = logConfiguration;
            this.serviceConfig = serviceConfig;
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        public SystemInfo GetSystemInfo()
        {
            var dbProvider = configuration.GetValue<string>("Database:Provider") ?? "Unknown";
            var connectionString = configuration.GetValue<string>("Database:ConnectionString");

            var authentication = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>() ?? new AuthenticationConfiguration();
            var authorization = configuration.GetSection("Authorization").Get<AuthorizationConfiguration>() ?? new AuthorizationConfiguration();

            return new SystemInfo(
                Os: RuntimeInformation.OSDescription.Trim(),
                Runtime: RuntimeInformation.FrameworkDescription,
                Architecture: RuntimeInformation.OSArchitecture.ToString(),
                ProcessId: Environment.ProcessId,
                DatabaseProvider: dbProvider,
                DatabaseConnection: GetSafeDatabaseConnection(dbProvider, connectionString),
                LogPath: logConfiguration.LogPath,
                IsAuthenticationEnabled: authentication.Enabled,
                IsAuthorizationEnabled: authorization.Enabled,
                EmergencyAdminSubjects: authorization.EmergencySystemAdminSubjects,
                BaseUrl: serviceConfig.BaseUrl,
                InstallTimestamp: GetInstallTimestamp());
        }

        private string? GetInstallTimestamp()
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var appSettingService = scope.ServiceProvider.GetRequiredService<IAppSettingService>();
                return appSettingService.GetInstallTimestamp()?.ToString("O", CultureInfo.InvariantCulture);
            }
            catch (Exception readFailure)
            {
                logger.LogWarning(readFailure, "Install timestamp could not be read; reporting it as absent so the feedback nudge fails closed");
                return null;
            }
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
