using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    // Epic 5146 slice 02a (#5641) — ADR-131; feature security checklist S4.
    // EF InMemory cannot model the conditional update this property rests on, so the probe needs a real provider.
    [TestFixture]
    [Category("requires-docker")]
    public class EmbedSessionSingleUseConcurrencyTests
    {
        private const string OwnerSubject = "embed-concurrency-owner";
        private const int ConcurrentRedemptions = 8;
        private const int ConcurrentPolls = 8;

        // Restated rather than referenced: a test that reads the name off the production constant
        // survives every mutation of it.
        private const string NonceReplayEventName = "EmbedHandshakeNonceReplayed";

        /// <summary>
        /// Epic 5146 slice 01 (#5692) — ADR-137 D68. The nonce half of the same guarantee, and the
        /// reason it needs its own case: consumption is a different conditional update over different
        /// columns, and it is the one that hands out a credential rather than spending one.
        /// The row is seeded through the service rather than driven through /embed/start: the thing
        /// under test is the conditional update, not the sign-in hop, and hop 1 would need an
        /// interactive session cookie this host has no forge for.
        /// D62/D67 rides along — every loser must be RECOGNISED as a second reader, not quietly
        /// folded into `unknown`, or a replayed nonce is unobservable exactly when it is being raced.
        /// </summary>
        [Test]
        public async Task Handshake_ManySimultaneousPollsOfOneNonce_ExactlyOneReceivesTheGrant()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            using var factory = new PostgresEmbedFactory(postgres.GetConnectionString());

            MigrateAndSeedOwner(factory);
            var nonce = await RecordHandshakeGrantAsync(factory);
            var neverIssued = await ViewerEmbedTestHost.PollHandshakeAsync(factory, ViewerEmbedTestHost.NewNonce());

            var readings = await RaceAsync(
                ConcurrentPolls,
                () => ViewerEmbedTestHost.CreateClient(factory),
                client => PollHandshakeAsync(client, nonce));

            var grants = readings.Where(reading => reading.HasProperty("token")).ToList();
            var losers = readings.Where(reading => !reading.HasProperty("token")).ToList();
            var replaysRecognised = factory.LogEvents.EventNames.Count(name => name == NonceReplayEventName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(grants, Has.Count.EqualTo(1),
                    "the nonce is a bearer credential for one session; a read-then-write reads every row as "
                    + "unconsumed inside the same window and mints a second token for the same sign-in");
                Assert.That(losers, Has.Count.EqualTo(ConcurrentPolls - 1),
                    "the losers are counted, not tolerated — one winner is half the property");
                Assert.That(losers, Is.All.EqualTo(neverIssued),
                    "D45: a loser must be byte-identical to a nonce that never existed, or losing the race "
                    + "is itself the oracle that says a live session happened here");
                Assert.That(replaysRecognised, Is.EqualTo(ConcurrentPolls - 1),
                    "D62/D67: every second reader is recognised and logged. Indistinguishable to the caller "
                    + "and invisible to the operator are not the same thing");
            }
        }

        /// <summary>
        /// Single use at the entry point: ADR-131's affected-row count, on the provider that can
        /// actually model the conditional update.
        /// </summary>
        [Test]
        public async Task Enter_ManySimultaneousRedemptionsOfOneViewerToken_ExactlyOneEstablishesASession()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            using var factory = new PostgresEmbedFactory(postgres.GetConnectionString());

            MigrateAndSeedOwner(factory);
            var nonce = await RecordHandshakeGrantAsync(factory);
            var token = await ClaimViewerTokenAsync(factory, nonce);

            var outcomes = await RaceRedemptionsAsync(factory, token);

            var sessions = outcomes.Count(outcome =>
                outcome.StatusCode == HttpStatusCode.Redirect && outcome.CarriesEmbedCookie);
            var refusals = outcomes.Count(outcome => outcome.StatusCode == HttpStatusCode.Unauthorized);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sessions, Is.EqualTo(1),
                    "single use is the conditional update's affected-row count; a read-then-write passes every "
                    + "single-threaded test and hands out extra sessions exactly when it matters. Counting the "
                    + "redirect alone would pass on a response that redirects without signing anyone in");
                Assert.That(refusals, Is.EqualTo(ConcurrentRedemptions - 1),
                    "and every loser is refused legibly rather than handed a second frame");
            }
        }

        private static Task<(HttpStatusCode StatusCode, bool CarriesEmbedCookie)[]> RaceRedemptionsAsync(
            PostgresEmbedFactory factory,
            string token)
        {
            return RaceAsync(
                ConcurrentRedemptions,
                () => ViewerEmbedTestHost.CreateClient(factory),
                client => RedeemAsync(client, token),
                WarmRedemptionPathAsync);
        }

        // D73: the clients are built ABOVE the barrier. CreateClient starts and locks the test server
        // and costs orders of magnitude more than the request it precedes, so a barrier that also
        // gates construction staggers the callers, every loser reads the row after the winner has
        // committed, and the atomic update under test is never exercised. Structural here rather than
        // restated per race, so a new race cannot get it wrong.
        private static async Task<T[]> RaceAsync<T>(
            int callers,
            Func<HttpClient> createClient,
            Func<HttpClient, Task<T>> call,
            Func<IReadOnlyList<HttpClient>, Task>? warmUp = null)
        {
            var clients = Enumerable.Range(0, callers).Select(_ => createClient()).ToList();

            try
            {
                if (warmUp is not null)
                {
                    await warmUp(clients);
                }

                using var barrier = new Barrier(callers);
                var calls = clients
                    .Select(client => Task.Run(async () =>
                    {
                        barrier.SignalAndWait();
                        return await call(client);
                    }))
                    .ToList();

                return await Task.WhenAll(calls);
            }
            finally
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
        }

        // D73, second half: hoisting the clients is not enough on its own. The first request through
        // /embed/enter pays for JIT, the EF query compile and a physical Npgsql connection, and that
        // cost swamps the request itself. In a cold process every racer pays it at once and they
        // arrive together; once an earlier test in the fixture has warmed the process, only the
        // connection opens are left to vary, the spread wins, and the losers read a row the winner
        // has already committed. Each caller therefore spends one refused redemption before the
        // barrier, so the raced request is the first one for nobody.
        private static async Task WarmRedemptionPathAsync(IReadOnlyList<HttpClient> clients)
        {
            var warmups = clients
                .Select(client => Task.Run(async () => await RedeemAsync(client, UnknownToken())))
                .ToList();

            var outcomes = await Task.WhenAll(warmups);

            // The warm-up doubles what this fixture spends against EmbedSession, and TestServer
            // reports no remote IP, so every client here shares the one "unknown" partition:
            // 1 setup request + 8 warm-ups + 8 raced = 17 of 20 per 60 seconds. Asserted rather
            // than commented, so raising ConcurrentRedemptions fails here saying why, instead of
            // turning into a 429 the race below would read as a refusal.
            Assert.That(outcomes.Select(outcome => outcome.StatusCode), Is.All.EqualTo(HttpStatusCode.Unauthorized),
                "precondition: the warm-up must be refused for the token it presents, not rate limited");
        }

        private static string UnknownToken()
        {
            return $"{ViewerEmbedTestHost.NewNonce()}.{ViewerEmbedTestHost.NewNonce()}";
        }

        private static async Task<(HttpStatusCode StatusCode, bool CarriesEmbedCookie)> RedeemAsync(
            HttpClient client,
            string token)
        {
            using var response = await client.GetAsync(new Uri(
                $"{ViewerEmbedTestHost.EntryPath}?token={Uri.EscapeDataString(token)}",
                UriKind.Relative));
            var cookie = ViewerEmbedTestHost.ReadCookieValue(response, ViewerEmbedTestHost.EmbedCookieName);

            return (response.StatusCode, CarriesEmbedCookie: cookie is { Length: > 0 });
        }

        private static async Task<ViewerEmbedTestHost.HandshakeReading> PollHandshakeAsync(HttpClient client, string nonce)
        {
            using var response = await client.GetAsync(
                new Uri($"{ViewerEmbedTestHost.HandshakePath}/{Uri.EscapeDataString(nonce)}", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            return new ViewerEmbedTestHost.HandshakeReading((int)response.StatusCode, body);
        }

        private static async Task<string> RecordHandshakeGrantAsync(PostgresEmbedFactory factory)
        {
            var nonce = ViewerEmbedTestHost.NewNonce();

            using var scope = factory.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<IEmbedSessionTokenService>();
            await tokenService.RecordHandshakeGrantAsync(OwnerSubject, nonce, CancellationToken.None);

            return nonce;
        }

        private static async Task<string> ClaimViewerTokenAsync(PostgresEmbedFactory factory, string nonce)
        {
            var reading = await ViewerEmbedTestHost.PollHandshakeAsync(factory, nonce);
            Assert.That(reading.HasProperty("token"), Is.True,
                "precondition: the first poll of a resolved grant must hand back a token");

            return reading.ReadString("token");
        }

        private static void MigrateAndSeedOwner(PostgresEmbedFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.Migrate();

            var profile = new UserProfile
            {
                Subject = OwnerSubject,
                SubjectClaimType = "sub",
                DisplayName = "Embed Concurrency Owner",
                Email = "embed-concurrency@example.test",
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            dbContext.SaveChanges();
        }

        private sealed class PostgresEmbedFactory : WebApplicationFactory<Program>
        {
            private readonly string connectionString;

            public PostgresEmbedFactory(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public ViewerEmbedTestHost.CapturedLogEvents LogEvents { get; } = new();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                // Provider is chosen from configuration, so the app registers Npgsql itself —
                // registering a second DbContext here would put two providers in one container.
                builder.UseSetting("Database:Provider", "postgres");
                builder.UseSetting("Database:ConnectionString", connectionString);
                builder.UseSetting("Embed:Enabled", "true");
                builder.UseSetting("Authentication:Enabled", "true");
                builder.UseSetting("Authentication:Authority", "https://example.test/oidc");
                builder.UseSetting("Authentication:ClientId", "lighthouse-embed-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting("Authentication:MetadataAddress", "https://example.test/oidc/.well-known/openid-configuration");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");
                builder.UseSetting("Authorization:Enabled", "true");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    services.AddSingleton<IStartupFilter>(new UnservedSpaPageStartupFilter());

                    // D72: writeToProviders is left false, so an ILoggerProvider added here is inert.
                    // Serilog is the pipeline, so the capture has to be a Serilog sink.
                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(LogEvents).CreateLogger(),
                        dispose: true));

                    var licenseServiceMock = new Mock<ILicenseService>();
                    licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            }
        }
    }
}
