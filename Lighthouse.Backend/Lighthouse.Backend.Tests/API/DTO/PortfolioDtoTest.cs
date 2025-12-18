using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class PortfolioDtoTest
    {
        [Test]
        public void CreatePortfolioDto_GivenLastUpdatedTime_ReturnsDateAsUTC()
        {
            var projectUpdateTime = DateTime.Now;
            var project = new Portfolio
            {
                UpdateTime = projectUpdateTime
            };

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(projectUpdateTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
            ;
        }

        [Test]
        public void CreatePortfolioDto_GivenPortfolioWithInvolvedTeams_ReturnsInvolvedTeams()
        {
            var team1 = new Team { Id = 1, Name = "Team 1" };
            var team2 = new Team { Id = 2, Name = "Team 2" };

            var portfolio = new Portfolio();
            portfolio.Teams.Add(team1);
            portfolio.Teams.Add(team2);

            var subject = CreateSubject(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.InvolvedTeams.Count, Is.EqualTo(2));
                Assert.That(subject.InvolvedTeams.Any(t => t.Id == team1.Id && t.Name == team1.Name), Is.True);
                Assert.That(subject.InvolvedTeams.Any(t => t.Id == team2.Id && t.Name == team2.Name), Is.True);
            }
        }

        private PortfolioDto CreateSubject(Portfolio project)
        {
            return new PortfolioDto(project);
        }
    }
}
