using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class BaselineSettingsSyncTest
    {
        [Test]
        public void SyncTeamWithTeamSettings_SyncsBaselineStartDate()
        {
            var team = new Team();
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var dto = new TeamSettingDto { ProcessBehaviourChartBaselineStartDate = start, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
        }

        [Test]
        public void SyncTeamWithTeamSettings_SyncsBaselineEndDate()
        {
            var team = new Team();
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var dto = new TeamSettingDto { ProcessBehaviourChartBaselineEndDate = end, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
        }

        [Test]
        public void SyncTeamWithTeamSettings_ClearsBaselineDatesWhenNull()
        {
            var team = new Team
            {
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow,
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow
            };
            var dto = new TeamSettingDto
            {
                ProcessBehaviourChartBaselineStartDate = null,
                ProcessBehaviourChartBaselineEndDate = null,
                WorkTrackingSystemConnectionId = 1
            };

            team.SyncTeamWithTeamSettings(dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ProcessBehaviourChartBaselineStartDate, Is.Null);
                Assert.That(team.ProcessBehaviourChartBaselineEndDate, Is.Null);
            }
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsBaselineStartDate()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var dto = new PortfolioSettingDto { ProcessBehaviourChartBaselineStartDate = start, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsBaselineEndDate()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var dto = new PortfolioSettingDto { ProcessBehaviourChartBaselineEndDate = end, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsBaselineDatesWhenNull()
        {
            var portfolio = new Portfolio
            {
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow,
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow
            };
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto
            {
                ProcessBehaviourChartBaselineStartDate = null,
                ProcessBehaviourChartBaselineEndDate = null,
                WorkTrackingSystemConnectionId = 1
            };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.Null);
                Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.Null);
            }
        }
    }
}
