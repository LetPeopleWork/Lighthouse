using System.Text;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Evidence that the three columns holding secrets are not capped in length on either relational
    /// provider. A credential of 100 KB is far longer than anything a work tracking system issues, and
    /// encrypting it grows it further, so a value that survives this survives any real one. Reading the
    /// model would suggest the same, but a silent truncation is a lost credential, so it is proven here
    /// against real SQLite and real PostgreSQL instead.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class LongSecretRoundTripTests
    {
        private const int CredentialLength = 100 * 1024;

        private const string SecretOptionKey = "PersonalAccessToken";

        private static readonly EncryptionKey ActiveKey = new("key-active", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly CryptoService Crypto = new(new EncryptionKeyRingHolder(new EncryptionKeyRing(ActiveKey)), NullLogger<CryptoService>.Instance);

        [Test]
        public async Task LongCredentials_OnSqlite_ReadBackUnchanged()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-long-secret-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertLongCredentialsRoundTripAsync(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task LongCredentials_OnPostgres_ReadBackUnchanged()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertLongCredentialsRoundTripAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();
        }

        private static async Task AssertLongCredentialsRoundTripAsync(ServiceProvider provider)
        {
            var optionSecret = LongCredential("option");
            var accessToken = LongCredential("access");
            var refreshToken = LongCredential("refresh");

            var connectionId = await SeedAsync(provider, optionSecret, accessToken, refreshToken);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var storedOption = await context.Set<WorkTrackingSystemConnectionOption>()
                .AsNoTracking()
                .SingleAsync(option => option.WorkTrackingSystemConnectionId == connectionId);

            var storedCredential = await context.Set<OAuthCredential>()
                .AsNoTracking()
                .SingleAsync(credential => credential.WorkTrackingSystemConnectionId == connectionId);

            AssertStoredEnvelopeYields(storedOption.Value, optionSecret, "the connection secret option");
            AssertStoredEnvelopeYields(storedCredential.AccessToken, accessToken, "the OAuth access token");
            AssertStoredEnvelopeYields(storedCredential.RefreshToken, refreshToken, "the OAuth refresh token");
        }

        private static async Task<int> SeedAsync(ServiceProvider provider, string optionSecret, string accessToken, string refreshToken)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Contoso Board",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = SecretOptionKey,
                Value = optionSecret,
                IsSecret = true,
            });

            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            context.Set<OAuthCredential>().Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();

            return connection.Id;
        }

        private static void AssertStoredEnvelopeYields(string storedValue, string credential, string column)
        {
            var read = Crypto.Read(storedValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope),
                    $"{column} must be stored as an envelope");
                Assert.That(read.PlainText, Has.Length.EqualTo(credential.Length),
                    $"{column} must come back at its full length rather than truncated by the column");
                Assert.That(read.PlainText, Is.EqualTo(credential),
                    $"{column} must come back exactly as it was written");
            }
        }

        private static string LongCredential(string marker)
        {
            var block = string.Concat(Enumerable.Repeat("0123456789abcdef", 256));
            var builder = new StringBuilder(marker, CredentialLength);

            while (builder.Length < CredentialLength)
            {
                builder.Append(block);
            }

            return builder.ToString(0, CredentialLength);
        }

        private static ServiceProvider BuildSqliteProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService>(Crypto);
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }

        private static ServiceProvider BuildPostgresProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService>(Crypto);
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            return services.BuildServiceProvider();
        }
    }
}
