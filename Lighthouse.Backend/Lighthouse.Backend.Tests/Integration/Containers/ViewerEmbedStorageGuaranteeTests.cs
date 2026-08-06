using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-137 D53/D54. Two storage-level guarantees that EF InMemory
    /// cannot see: it enforces neither check constraints nor foreign keys.
    /// </summary>
    [TestFixture]
    [Category("epic-5146-jira-forge-app")]
    public class ViewerEmbedStorageGuaranteeTests
    {
        public const string GrantOrRefusalConstraintName = "CK_EmbedSessionTokens_GrantOrRefusal";

        private const string GrantInsert = """
            INSERT INTO "EmbedSessionTokens"
              ("TokenId", "SecretHash", "Subject", "HandshakeNonceHash", "RefusalCode", "ApiKeyId", "CreatedAt", "ExpiresAt")
            VALUES ('grant-token', 'grant-hash', 'viewer-subject', 'nonce-hash', NULL, NULL, '2026-08-06', '2026-08-07')
            """;

        private const string RefusalInsert = """
            INSERT INTO "EmbedSessionTokens"
              ("TokenId", "SecretHash", "Subject", "HandshakeNonceHash", "RefusalCode", "ApiKeyId", "CreatedAt", "ExpiresAt")
            VALUES (NULL, NULL, 'viewer-subject', 'nonce-hash', 'no_access', NULL, '2026-08-06', '2026-08-07')
            """;

        private const string IllegalInsert = """
            INSERT INTO "EmbedSessionTokens"
              ("TokenId", "SecretHash", "Subject", "HandshakeNonceHash", "RefusalCode", "ApiKeyId", "CreatedAt", "ExpiresAt")
            VALUES ('grant-token', 'grant-hash', 'viewer-subject', 'nonce-hash', 'no_access', NULL, '2026-08-06', '2026-08-07')
            """;

        private const string ConstraintMissingMessage =
            "the generated migration must emit " + GrantOrRefusalConstraintName
            + "; a model-only constraint that does not round-trip is rung 1 of D54's ladder failing";

        private const string IllegalRowMessage =
            "a refused viewer must not hold a redeemable credential — and a constraint that rejected "
            + "everything would pass this line while meaning nothing, which is why the two legal shapes "
            + "are asserted alongside it";

        private const string CascadeLostMessage =
            "deleting an API key must still revoke every token it minted while the API-key path is "
            + "reachable (slice 01 keeps it reachable on purpose)";

        [Test]
        public async Task OnSqlite_TheGrantOrRefusalConstraint_DiscriminatesRatherThanRejectsEverything()
        {
            var probe = await WithSqliteAsync(ProbeConstraintAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.ConstraintDeclared, Is.True, ConstraintMissingMessage);
                Assert.That(probe.GrantAccepted, Is.True, "a grant row is legal");
                Assert.That(probe.RefusalAccepted, Is.True, "a refusal row is legal");
                Assert.That(probe.IllegalAccepted, Is.False, IllegalRowMessage);
            }
        }

        [Test]
        [Category("requires-docker")]
        public async Task OnPostgres_TheGrantOrRefusalConstraint_DiscriminatesRatherThanRejectsEverything()
        {
            var probe = await WithPostgresAsync(ProbeConstraintAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.ConstraintDeclared, Is.True, ConstraintMissingMessage);
                Assert.That(probe.GrantAccepted, Is.True, "a grant row is legal");
                Assert.That(probe.RefusalAccepted, Is.True, "a refusal row is legal");
                Assert.That(probe.IllegalAccepted, Is.False, IllegalRowMessage);
            }
        }

        /// <summary>
        /// D53, raised on peer review. EF's default for an OPTIONAL relationship is ClientSetNull, so a
        /// crafter who makes ApiKeyId nullable and moves on deletes ADR-131's revocation lever 1 during
        /// the one slice where both paths still run — and nothing else in the suite notices. Green today
        /// on purpose: this is the guard, authored before the change that could break it.
        /// </summary>
        [Test]
        public async Task OnSqlite_DeletingAnApiKey_StillRemovesTheEmbedSessionRowsItMinted()
        {
            var probe = await WithSqliteAsync(ProbeCascadeAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.BeforeDeletion, Is.EqualTo(1), "differential control: the row exists while the key does");
                Assert.That(probe.AfterDeletion, Is.Zero, CascadeLostMessage);
            }
        }

        [Test]
        [Category("requires-docker")]
        public async Task OnPostgres_DeletingAnApiKey_StillRemovesTheEmbedSessionRowsItMinted()
        {
            var probe = await WithPostgresAsync(ProbeCascadeAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.BeforeDeletion, Is.EqualTo(1), "differential control: the row exists while the key does");
                Assert.That(probe.AfterDeletion, Is.Zero, CascadeLostMessage);
            }
        }

        private static async Task<ConstraintProbe> ProbeConstraintAsync(LighthouseAppContext context)
        {
            var declared = await ConstraintIsDeclaredAsync(context);

            var grantAccepted = await TryExecuteAsync(context, GrantInsert);
            await TryExecuteAsync(context, "DELETE FROM \"EmbedSessionTokens\"");
            var refusalAccepted = await TryExecuteAsync(context, RefusalInsert);
            await TryExecuteAsync(context, "DELETE FROM \"EmbedSessionTokens\"");
            var illegalAccepted = await TryExecuteAsync(context, IllegalInsert);

            return new ConstraintProbe(declared, grantAccepted, refusalAccepted, illegalAccepted);
        }

        private static async Task<CascadeProbe> ProbeCascadeAsync(LighthouseAppContext context)
        {
            var apiKey = new ApiKey
            {
                Name = "viewer-embed-cascade-key",
                Description = "epic 5146 slice 01",
                KeyHash = "hash",
                Salt = "salt",
                CreatedByUser = "cascade-owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.ApiKeys.Add(apiKey);
            await context.SaveChangesAsync();

            context.EmbedSessionTokens.Add(new EmbedSessionToken
            {
                TokenId = $"cascade-{Guid.NewGuid():N}",
                SecretHash = "cascade-hash",
                ApiKeyId = apiKey.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1),
            });
            await context.SaveChangesAsync();

            var beforeDeletion = await context.EmbedSessionTokens.CountAsync();

            context.ApiKeys.Remove(apiKey);
            await context.SaveChangesAsync();

            return new CascadeProbe(beforeDeletion, await context.EmbedSessionTokens.CountAsync());
        }

        private static async Task<bool> ConstraintIsDeclaredAsync(LighthouseAppContext context)
        {
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            var sql = isSqlite
                ? "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'EmbedSessionTokens'"
                : $"SELECT conname FROM pg_constraint WHERE conname = '{GrantOrRefusalConstraintName}'";

            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var scalar = await command.ExecuteScalarAsync();

            return scalar?.ToString()?.Contains(GrantOrRefusalConstraintName, StringComparison.Ordinal) == true;
        }

        private static async Task<bool> TryExecuteAsync(LighthouseAppContext context, string sql)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
                return true;
            }
            catch (DbException)
            {
                return false;
            }
        }

        private static async Task<T> WithSqliteAsync<T>(Func<LighthouseAppContext, Task<T>> probe)
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-viewer-embed-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite(
                        $"Data Source={databaseFile};Pooling=False",
                        sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

                return await RunAsync(provider, probe);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static async Task<T> WithPostgresAsync<T>(Func<LighthouseAppContext, Task<T>> probe)
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            await using var provider = BuildProvider(options =>
                options.UseNpgsql(
                    postgres.GetConnectionString(),
                    npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            return await RunAsync(provider, probe);
        }

        private static async Task<T> RunAsync<T>(ServiceProvider provider, Func<LighthouseAppContext, Task<T>> probe)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            return await probe(context);
        }

        private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> configure)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(configure);

            return services.BuildServiceProvider();
        }

        private sealed record ConstraintProbe(
            bool ConstraintDeclared,
            bool GrantAccepted,
            bool RefusalAccepted,
            bool IllegalAccepted);

        private sealed record CascadeProbe(int BeforeDeletion, int AfterDeletion);
    }
}
