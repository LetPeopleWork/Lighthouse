using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Where nobody signs in, or people sign in but roles are not enforced, everybody is Admin everywhere: every
    /// report is listed, every owner may be created on and every report may be removed. The server decides this
    /// through the instance's real RBAC service, not a rule of the bulk addresses' own.
    ///
    /// Driving ports: GET /api/latest/reports, POST /api/latest/reports/bulk, POST /api/latest/reports/bulk-removal.
    /// </summary>
    public abstract class BulkReportsWhereEveryoneIsAdminScenarios : BulkReportsAcceptanceTest
    {
        // @driving_port @real-io @us-01 @slice-01 @contract-shape:pure-function
        [Test]
        public async Task Every_report_on_every_owner_is_listed()
        {
            var gravity = ATeam("Gravity");
            var apollo = APortfolio("Apollo");
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            await GivenTheOwnerHolds(apollo, TheQuartersComparison);
            AnybodyAsks();

            var owners = OwnersIn(await EveryReportTheCallerCanRead());

            Assert.That(owners, Is.EquivalentTo(new[] { OwnerKey(gravity), OwnerKey(apollo) }));
        }

        // @driving_port @real-io @us-02a @slice-02a @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task Every_Team_may_be_created_on_in_one_run()
        {
            TheInstanceHasAPremiumLicence();
            var gravity = ATeam("Gravity");
            var pulsar = ATeam("Pulsar");
            AnybodyAsks();

            using var run = await CreateForEach(TheQuartersComparison, gravity, pulsar);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => outcome.Status), Has.All.EqualTo(Created).And.Count.EqualTo(2));
        }

        // @driving_port @real-io @us-02a @slice-02a @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task A_Community_instance_is_still_refused_the_whole_run()
        {
            TheInstanceHasNoPremiumLicence();
            var gravity = ATeam("Gravity");
            AnybodyAsks();

            using var run = await CreateForEach(TheQuartersComparison, gravity);

            await ThenItIsForbidden(run);
        }

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice05)]
        public async Task Every_report_may_be_removed_in_one_run()
        {
            var gravity = ATeam("Gravity");
            var apollo = APortfolio("Apollo");
            var onGravity = await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            var onApollo = await GivenTheOwnerHolds(apollo, TheQuartersComparison);
            AnybodyAsks();

            using var run = await RemoveEach(onGravity, onApollo);
            var outcomes = await TheOutcomesOf(run);

            Assert.That(outcomes.Select(outcome => outcome.Status), Has.All.EqualTo(Removed).And.Count.EqualTo(2));
        }

        /// <summary>The caller as this instance sees them when it enforces no roles.</summary>
        protected abstract void AnybodyAsks();
    }

    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("auth-off")]
    public class BulkReportsWithoutSignInScenarios : BulkReportsWhereEveryoneIsAdminScenarios
    {
        protected override WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => WithTheRunsStandIns(root);

        protected override void AnybodyAsks() => Client.AsAnonymous();
    }

    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("auth-on-rbac-off")]
    public class BulkReportsWithRolesNotEnforcedScenarios : BulkReportsWhereEveryoneIsAdminScenarios
    {
        protected override WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => WithTheRunsStandIns(root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Enabled"] = "true",
                    ["Authentication:Authority"] = "https://example.test/oidc",
                    ["Authentication:ClientId"] = "lighthouse-test",
                    ["Authentication:ClientSecret"] = "test-secret",
                    ["Authentication:MetadataAddress"] = "https://example.test/oidc/.well-known/openid-configuration",
                    ["Authentication:RequireHttpsMetadata"] = "false",
                    ["Authorization:Enabled"] = "false",
                }));

                builder.ConfigureServices(services => services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = TestAuthHandler.SchemeName;
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
            }));

        protected override void AnybodyAsks() => Client.AsViewer("someone-without-a-role");
    }
}
