using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class SystemInfoService : ISystemInfoService
    {
        private readonly IConfiguration configuration;
        private readonly ILogConfiguration logConfiguration;
        private readonly IServiceConfig serviceConfig;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<SystemInfoService> logger;

        private readonly KeyCustodyDescription keyCustody;

        public SystemInfoService(IConfiguration configuration, ILogConfiguration logConfiguration, IServiceConfig serviceConfig, IServiceScopeFactory scopeFactory, ILogger<SystemInfoService> logger, KeyCustodyDescription keyCustody)
        {
            this.keyCustody = keyCustody;
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
                InstallTimestamp: GetInstallTimestamp(),
                Encryption: keyCustody.Line);
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

        // Names what may be published rather than what may not. A connection string is a format with
        // quoting and aliases, not a list of semicolon-separated pairs: a password may legitimately
        // contain a semicolon, and the driver answers to more than one spelling of "password". Removing
        // the names somebody thought of leaves everything nobody thought of on the wire, so this reads
        // the string with the driver's own parser and reports back only the three fields named here.
        private string? GetSafeDatabaseConnection(string provider, string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            var normalizedProvider = provider.ToLowerInvariant();

            try
            {
                if (normalizedProvider == "sqlite")
                {
                    return new SqliteConnectionStringBuilder(connectionString).DataSource;
                }

                if (normalizedProvider is "postgresql" or "postgres")
                {
                    var configured = new NpgsqlConnectionStringBuilder(connectionString);

                    var reported = new NpgsqlConnectionStringBuilder();
                    reported["Host"] = configured.Host;
                    reported["Port"] = configured.Port;
                    reported["Database"] = configured.Database;

                    return reported.ConnectionString;
                }
            }
            // A connection string the driver will not read is one the application is not running on, so
            // there is nothing here worth reporting. This response is what the interface fetches before it
            // can draw anything, and it costs the operator one field instead of the whole page.
            catch (Exception unreadable) when (unreadable is ArgumentException or FormatException)
            {
                logger.LogWarning(unreadable, "The configured database connection string could not be parsed, so the system information reports no connection details");
            }

            return null;
        }
    }
}
