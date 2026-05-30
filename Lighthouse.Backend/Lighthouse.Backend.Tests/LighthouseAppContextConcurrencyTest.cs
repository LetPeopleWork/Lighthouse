using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class LighthouseAppContextConcurrencyTest
    {
        private TestWebApplicationFactory<Program> factory = null!;

        [SetUp]
        public void Init()
        {
            factory = new TestWebApplicationFactory<Program>();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            factory.Dispose();
        }

        [Test]
        public void AddedTeam_GetsNonEmptyInitialConcurrencyToken()
        {
            var teamId = SeedTeam();

            Guid initialToken;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                initialToken = context.Teams.Single(t => t.Id == teamId).ConcurrencyToken;
            }

            Assert.That(initialToken, Is.Not.EqualTo(Guid.Empty), "A newly added team must receive a non-empty initial concurrency token.");
        }

        [Test]
        public async Task SaveModifiedTeam_WithoutEditApply_LeavesConcurrencyTokenUnchanged()
        {
            var teamId = SeedTeam();

            Guid tokenBefore;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var team = context.Teams.Single(t => t.Id == teamId);
                tokenBefore = team.ConcurrencyToken;

                team.Name = "System Save Team";
                await context.SaveChangesAsync();
            }

            Guid tokenAfter;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                tokenAfter = context.Teams.Single(t => t.Id == teamId).ConcurrencyToken;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokenBefore, Is.Not.EqualTo(Guid.Empty), "A persisted team must carry a non-empty token.");
                Assert.That(tokenAfter, Is.EqualTo(tokenBefore), "A plain system/background save must NOT churn the concurrency token.");
            }
        }

        [Test]
        public async Task ApplyConcurrencyTokenForEdit_OnHumanEditPath_AdvancesConcurrencyToken()
        {
            var teamId = SeedTeam();

            Guid tokenBefore;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var team = context.Teams.Single(t => t.Id == teamId);
                tokenBefore = team.ConcurrencyToken;

                team.Name = "Human Edit Team";
                context.ApplyConcurrencyTokenForEdit(team, tokenBefore);
                await context.SaveChangesAsync();
            }

            Guid tokenAfter;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                tokenAfter = context.Teams.Single(t => t.Id == teamId).ConcurrencyToken;
            }

            Assert.That(tokenAfter, Is.Not.EqualTo(tokenBefore), "The human-edit path must advance the concurrency token so read-your-writes holds.");
        }

        [Test]
        public async Task SaveWithStaleToken_OnTokenedAggregate_DoesNotRetry_PropagatesConcurrencyException()
        {
            var teamId = SeedTeam();

            using var staleScope = factory.Services.CreateScope();
            var staleContext = staleScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var staleTeam = staleContext.Teams.Single(t => t.Id == teamId);
            var staleToken = staleTeam.ConcurrencyToken;

            using (var winnerScope = factory.Services.CreateScope())
            {
                var winnerContext = winnerScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var winnerTeam = winnerContext.Teams.Single(t => t.Id == teamId);
                winnerTeam.Name = "Winner";
                winnerContext.ApplyConcurrencyTokenForEdit(winnerTeam, staleToken);
                await winnerContext.SaveChangesAsync();
            }

            staleTeam.Name = "Loser";
            staleContext.ApplyConcurrencyTokenForEdit(staleTeam, staleToken);

            Assert.That(
                async () => await staleContext.SaveChangesAsync(),
                Throws.InstanceOf<DbUpdateConcurrencyException>(),
                "A stale save on a tokened aggregate must propagate the concurrency exception, not be reloaded-and-retried into a last-writer-wins overwrite.");
        }

        private int SeedTeam()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }
    }
}
