using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class TeamDeletionIntegrationTest() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task DeleteTeamAfterPortfolioDeletion_WithExistingForecasts_TeamDeletionSucceeds()
        {
            var loadResponse = await Client.PostAsync("/api/demo/scenarios/0/load", null);
            loadResponse.EnsureSuccessStatusCode();

            var team = DatabaseContext.Teams.Single();
            var portfolio = DatabaseContext.Portfolios.Include(p => p.Features).Single();

            var feature = new Feature { Name = "Test Epic", Order = "1" };
            portfolio.Features.Add(feature);
            await DatabaseContext.SaveChangesAsync();

            var whenForecast = new WhenForecast { FeatureId = feature.Id, TeamId = team.Id, NumberOfItems = 5 };
            DatabaseContext.Set<WhenForecast>().Add(whenForecast);
            await DatabaseContext.SaveChangesAsync();

            // Checkpoint 1 — deleting the portfolio must succeed
            var deletePortfolioResponse = await Client.DeleteAsync($"/api/portfolios/{portfolio.Id}");
            Assert.That(deletePortfolioResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            // Checkpoint 2 — deleting the team must also succeed (fails before fix because
            // WhenForecast.TeamId still references the team after the orphaned Feature was not cleaned up)
            var deleteTeamResponse = await Client.DeleteAsync($"/api/teams/{team.Id}");
            Assert.That(deleteTeamResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
    }
}
