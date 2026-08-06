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

        [Test]
        public async Task Redeem_ManySimultaneousRedemptionsOfOneToken_ExactlyOneEstablishesASession()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            using var factory = new PostgresEmbedFactory(postgres.GetConnectionString());

            MigrateAndSeedOwner(factory);
            var apiKey = await CreateApiKeyAsync(factory);
            var token = await EmbedSessionTestHost.MintTokenAsync(factory, apiKey);

            using var barrier = new Barrier(ConcurrentRedemptions);
            var redemptions = Enumerable.Range(0, ConcurrentRedemptions)
                .Select(_ => Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    using var response = await EmbedSessionTestHost.EnterAsync(factory, token);
                    return response.StatusCode;
                }))
                .ToList();

            var statuses = await Task.WhenAll(redemptions);

            var successes = statuses.Count(status => status == HttpStatusCode.Redirect);
            var refusals = statuses.Count(status => status == HttpStatusCode.Unauthorized);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(successes, Is.EqualTo(1),
                    "single use is the conditional update's affected-row count; a read-then-write passes every "
                    + "single-threaded test and hands out extra sessions exactly when it matters");
                Assert.That(refusals, Is.EqualTo(ConcurrentRedemptions - 1),
                    "every loser of the race must be refused legibly, not served a second session");
            }
        }

        /// <summary>
        /// Epic 5146 slice 01 (#5692) — ADR-132 D68. The nonce half of the same guarantee, and the
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

            // The clients are built before the barrier on purpose. Creating one starts the test server
            // and is far slower than the request itself, so a barrier that also gates construction
            // staggers the callers and the race never happens.
            var clients = Enumerable.Range(0, ConcurrentPolls)
                .Select(_ => ViewerEmbedTestHost.CreateClient(factory))
                .ToList();

            ViewerEmbedTestHost.HandshakeReading[] readings;
            try
            {
                using var barrier = new Barrier(ConcurrentPolls);
                var polls = clients
                    .Select(client => Task.Run(async () =>
                    {
                        barrier.SignalAndWait();
                        return await PollHandshakeAsync(client, nonce);
                    }))
                    .ToList();

                readings = await Task.WhenAll(polls);
            }
            finally
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }

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
        /// The viewer half of single use. The API-key case above does not cover it: a subject-bound row
        /// runs through a different NamesAnIdentity branch and a different principal construction, so a
        /// regression could land on one path and not the other.
        /// </summary>
        [Test]
        public async Task Enter_ManySimultaneousRedemptionsOfOneViewerToken_ExactlyOneEstablishesASession()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            using var factory = new PostgresEmbedFactory(postgres.GetConnectionString());

            MigrateAndSeedOwner(factory);
            var nonce = await RecordHandshakeGrantAsync(factory);
            var token = await ClaimViewerTokenAsync(factory, nonce);

            using var barrier = new Barrier(ConcurrentRedemptions);
            var redemptions = Enumerable.Range(0, ConcurrentRedemptions)
                .Select(_ => Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    using var response = await EmbedSessionTestHost.EnterAsync(factory, token);
                    var cookie = EmbedSessionTestHost.ReadCookieValue(response, EmbedSessionTestHost.EmbedCookieName);

                    return (response.StatusCode, CarriesEmbedCookie: cookie is { Length: > 0 });
                }))
                .ToList();

            var outcomes = await Task.WhenAll(redemptions);

            var sessions = outcomes.Count(outcome =>
                outcome.StatusCode == HttpStatusCode.Redirect && outcome.CarriesEmbedCookie);
            var refusals = outcomes.Count(outcome => outcome.StatusCode == HttpStatusCode.Unauthorized);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sessions, Is.EqualTo(1),
                    "a viewer-identity token is single use in the same sense an API-key one is; counting the "
                    + "redirect alone would pass on a response that redirects without signing anyone in");
                Assert.That(refusals, Is.EqualTo(ConcurrentRedemptions - 1),
                    "and every loser is refused legibly rather than handed a second frame");
            }
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

        private static async Task<string> CreateApiKeyAsync(PostgresEmbedFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var creationResult = await apiKeyService.CreateApiKeyAsync(
                "embed-concurrency-key",
                "epic 5146 slice 02a",
                createdByUser: OwnerSubject,
                ownerSubject: OwnerSubject);

            return creationResult.PlainTextKey;
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
