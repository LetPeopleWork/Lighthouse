using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Moving a stored secret onto a new key rewrites the same three columns a token refresh rewrites. If a
    /// refresh lands between the moment the move reads a value and the moment it writes one, the refreshed
    /// token must not be overwritten with a re-encryption of the token it replaced - that is a credential
    /// nobody can recover without asking the work tracking system for a new one, which is the cost this work
    /// exists to remove. The move therefore names the value it observed in its own WHERE clause. This is the
    /// evidence that both relational providers honour that, so no lock and no maintenance window is needed.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class ReEncryptionCompareAndSwapProbeTests
    {
        private const string SecretOptionKey = "PersonalAccessToken";

        private static readonly EncryptionKey RetiredKey = new("key-retired", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey ActiveKey = new("key-active", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private static readonly CryptoService Crypto = new(
            new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, ActiveKey, RetiredKey)),
            NullLogger<CryptoService>.Instance);

        [Test]
        public async Task CompareAndSwap_OnSqlite_MovesWhatItObserved_AndDeclinesWhatSomebodyElseRewrote()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-cas-probe-{Guid.NewGuid():N}.db");

            try
            {
                await using var provider = BuildSqliteProvider(databaseFile);
                await MigrateAsync(provider);
                await AssertCompareAndSwapAsync(provider);
            }
            finally
            {
                SqliteConnection.ClearAllPools();

                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task CompareAndSwap_OnPostgres_MovesWhatItObserved_AndDeclinesWhatSomebodyElseRewrote()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertCompareAndSwapAsync(provider);
        }

        private static async Task AssertCompareAndSwapAsync(ServiceProvider provider)
        {
            var connectionId = await SeedAsync(provider, "the-personal-access-token", "the-access-token", "the-refresh-token");

            var (optionId, observedOption) = await ObservedOptionAsync(provider, connectionId);
            var (credentialId, observedAccessToken) = await ObservedAccessTokenAsync(provider, connectionId);

            await RefreshTokenLandsAsync(provider, credentialId, "the-refreshed-access-token");

            int optionRowsMoved;
            int credentialRowsMoved;

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                optionRowsMoved = await context.Set<WorkTrackingSystemConnectionOption>()
                    .Where(option => option.Id == optionId && option.Value == observedOption)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(option => option.Value, Crypto.Encrypt("the-personal-access-token")));

                credentialRowsMoved = await context.Set<OAuthCredential>()
                    .Where(credential => credential.Id == credentialId && credential.AccessToken == observedAccessToken)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(credential => credential.AccessToken, Crypto.Encrypt("the-access-token")));
            }

            var (_, storedAccessToken) = await ObservedAccessTokenAsync(provider, connectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(optionRowsMoved, Is.EqualTo(1),
                    "a row nobody else rewrote must be moved onto the new key");
                Assert.That(credentialRowsMoved, Is.Zero,
                    "a row a token refresh rewrote after it was read must not be moved, because moving it would write the old token back");
                Assert.That(Crypto.Read(storedAccessToken).PlainText, Is.EqualTo("the-refreshed-access-token"),
                    "the refreshed token must survive the re-encryption pass");
            }
        }

        private static async Task RefreshTokenLandsAsync(ServiceProvider provider, int credentialId, string refreshedAccessToken)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var credential = await context.Set<OAuthCredential>().SingleAsync(stored => stored.Id == credentialId);
            credential.AccessToken = refreshedAccessToken;
            credential.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();
        }

        private static async Task<(int Id, string Stored)> ObservedOptionAsync(ServiceProvider provider, int connectionId)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var option = await context.Set<WorkTrackingSystemConnectionOption>()
                .AsNoTracking()
                .SingleAsync(stored => stored.WorkTrackingSystemConnectionId == connectionId);

            return (option.Id, option.Value);
        }

        private static async Task<(int Id, string Stored)> ObservedAccessTokenAsync(ServiceProvider provider, int connectionId)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var credential = await context.Set<OAuthCredential>()
                .AsNoTracking()
                .SingleAsync(stored => stored.WorkTrackingSystemConnectionId == connectionId);

            return (credential.Id, credential.AccessToken);
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

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();
        }

        // Built the way the product builds it: one connection, opened once, with the same journal mode and
        // busy timeout, because whether two writers can collide at all is a property of that arrangement.
        private static ServiceProvider BuildSqliteProvider(string databaseFile)
        {
            var connection = new SqliteConnection($"Data Source={databaseFile}");
            connection.Open();

            SqliteLegacyDoubleQuotedStringInterceptor.EnableLegacyDoubleQuotedStrings(connection);

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=10000; PRAGMA synchronous=NORMAL;";
                pragma.ExecuteNonQuery();
            }

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService>(Crypto);
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

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
