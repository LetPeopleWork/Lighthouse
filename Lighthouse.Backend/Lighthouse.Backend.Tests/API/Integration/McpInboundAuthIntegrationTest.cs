using System.Net;
using Lighthouse.Backend.Tests.TestHelpers.McpInboundAuth;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class McpInboundAuthIntegrationTest
    {
        [Test]
        public async Task ApiKeyAuth_TwoCallersDistinctKeys_EachSeesOnlyOwnScopedData()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var keyA = await host.SeedCallerAsync("owner-a", ownerTeamIds: [teamA], keyScopeTeamIds: [teamA]);
            var keyB = await host.SeedCallerAsync("owner-b", ownerTeamIds: [teamB], keyScopeTeamIds: [teamB]);

            var callerASeesOwnTeam = await host.GetTeamAsync(teamA, keyA);
            var callerASeesOtherTeam = await host.GetTeamAsync(teamB, keyA);
            var callerBSeesOwnTeam = await host.GetTeamAsync(teamB, keyB);
            var callerBSeesOtherTeam = await host.GetTeamAsync(teamA, keyB);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callerASeesOwnTeam, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(callerASeesOtherTeam, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(callerBSeesOwnTeam, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(callerBSeesOtherTeam, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ApiKeyAuth_ForwardedCallerKey_ResolvesToCallerOwnerNotBakedKey()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var keyOfTeamAOwner = await host.SeedCallerAsync("owner-a", ownerTeamIds: [teamA], keyScopeTeamIds: [teamA]);
            var keyOfTeamBOwner = await host.SeedCallerAsync("owner-b", ownerTeamIds: [teamB], keyScopeTeamIds: [teamB]);

            var withOwnersOwnKey = await host.GetTeamAsync(teamA, keyOfTeamAOwner);
            var withForeignKey = await host.GetTeamAsync(teamA, keyOfTeamBOwner);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withOwnersOwnKey, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(withForeignKey, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ApiKeyAuth_KeyScopeNarrowerThanOwner_KeyRestrictsBeyondOwnerGrants()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var narrowKey = await host.SeedCallerAsync("owner-a", ownerTeamIds: [teamA, teamB], keyScopeTeamIds: [teamA]);

            var inKeyScope = await host.GetTeamAsync(teamA, narrowKey);
            var ownerCanReachButKeyCannot = await host.GetTeamAsync(teamB, narrowKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inKeyScope, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(ownerCanReachButKeyCannot, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ApiKeyAuth_KeyClaimsScopeOwnerLacks_ExcessScopeDropped()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var overReachingKey = await host.SeedCallerAsync("owner-a", ownerTeamIds: [teamA], keyScopeTeamIds: [teamA, teamB]);

            var ownerCovered = await host.GetTeamAsync(teamA, overReachingKey);
            var ownerNotCovered = await host.GetTeamAsync(teamB, overReachingKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ownerCovered, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(ownerNotCovered, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ApiKeyAuth_SystemAdminOwnerWithTeamScopedKey_KeyRestrictsBelowSystemWide()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var teamScopedKey = await host.SeedSystemAdminCallerAsync("owner-root", keyScopeTeamIds: [teamA]);

            var inKeyScope = await host.GetTeamAsync(teamA, teamScopedKey);
            var systemWideButOutOfKeyScope = await host.GetTeamAsync(teamB, teamScopedKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inKeyScope, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(systemWideButOutOfKeyScope, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ApiKeyAuth_KeyClaimsSystemScopeOwnerLacks_NoPrivilegeEscalation()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var escalatingKey = await host.SeedCallerWithSystemScopedKeyAsync("owner-a", ownerTeamId: teamA);

            var systemScopeIsDropped = await host.GetTeamAsync(teamA, escalatingKey);

            Assert.That(systemScopeIsDropped, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task ApiKeyAuth_CallerWithoutScope_ForbiddenOnOutOfScopeResource()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            var keyScopedToTeamA = await host.SeedCallerAsync("owner-a", ownerTeamIds: [teamA], keyScopeTeamIds: [teamA]);

            var writeOutOfScope = await host.UpdateTeamDataAsync(teamB, keyScopedToTeamA);

            Assert.That(writeOutOfScope, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task ApiKeyAuth_SingleKeyDevPath_StillWorks()
        {
            using var host = new McpInboundAuthTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var legacyKey = await host.SeedCallerAsync("owner-legacy", ownerTeamIds: [teamA], keyScopeTeamIds: null);

            var legacyKeyReadsOwnerScope = await host.GetTeamAsync(teamA, legacyKey);

            Assert.That(legacyKeyReadsOwnerScope, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
