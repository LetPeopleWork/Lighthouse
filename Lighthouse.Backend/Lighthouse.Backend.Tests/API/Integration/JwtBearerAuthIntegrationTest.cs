using System.Net;
using Lighthouse.Backend.Tests.TestHelpers.JwtBearerAuth;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class JwtBearerAuthIntegrationTest
    {
        [Test]
        public async Task ValidToken_AuthenticatedUserWithTeamScope_Returns200()
        {
            using var host = new JwtBearerTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            await host.SeedTeamAdminUserAsync("owner-a", teamA);
            var token = host.MintToken(new JwtTokenOptions { Subject = "owner-a" });

            var status = await host.GetTeamWithBearerAsync(teamA, token);

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task TwoTokensDistinctSubjects_EachSeesOnlyOwnTeamScope()
        {
            using var host = new JwtBearerTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            var teamB = await host.SeedTeamAsync("Team B");
            await host.SeedTeamAdminUserAsync("owner-a", teamA);
            await host.SeedTeamAdminUserAsync("owner-b", teamB);
            var tokenA = host.MintToken(new JwtTokenOptions { Subject = "owner-a" });
            var tokenB = host.MintToken(new JwtTokenOptions { Subject = "owner-b" });

            var callerASeesOwnTeam = await host.GetTeamWithBearerAsync(teamA, tokenA);
            var callerASeesOtherTeam = await host.GetTeamWithBearerAsync(teamB, tokenA);
            var callerBSeesOwnTeam = await host.GetTeamWithBearerAsync(teamB, tokenB);
            var callerBSeesOtherTeam = await host.GetTeamWithBearerAsync(teamA, tokenB);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callerASeesOwnTeam, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(callerASeesOtherTeam, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(callerBSeesOwnTeam, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(callerBSeesOtherTeam, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task WrongAudienceToken_Rejected401()
        {
            using var host = new JwtBearerTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            await host.SeedTeamAdminUserAsync("owner-a", teamA);
            var token = host.MintToken(new JwtTokenOptions
            {
                Subject = "owner-a",
                Audience = "some-other-api",
            });

            var status = await host.GetTeamWithBearerAsync(teamA, token);

            Assert.That(status, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task ExpiredToken_Rejected401()
        {
            using var host = new JwtBearerTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            await host.SeedTeamAdminUserAsync("owner-a", teamA);
            var token = host.MintToken(new JwtTokenOptions { Subject = "owner-a", Expired = true });

            var status = await host.GetTeamWithBearerAsync(teamA, token);

            Assert.That(status, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task BadSignatureToken_Rejected401()
        {
            using var host = new JwtBearerTestHost();
            await host.SeedSystemAdminAsync("system-admin");
            var teamA = await host.SeedTeamAsync("Team A");
            await host.SeedTeamAdminUserAsync("owner-a", teamA);
            var token = host.MintToken(new JwtTokenOptions { Subject = "owner-a", ValidSignature = false });

            var status = await host.GetTeamWithBearerAsync(teamA, token);

            Assert.That(status, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
