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
