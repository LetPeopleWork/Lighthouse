using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class EstimationFieldSyncTest
    {
        [Test]
        public void SyncTeamWithTeamSettings_SyncsEstimationFieldDefinitionId()
        {
            var team = new Team();
            var dto = new TeamSettingDto { EstimationAdditionalFieldDefinitionId = 42, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationAdditionalFieldDefinitionId, Is.EqualTo(42));
        }

        [Test]
        public void SyncTeamWithTeamSettings_ClearsEstimationFieldWhenNull()
        {
            var team = new Team { EstimationAdditionalFieldDefinitionId = 42 };
            var dto = new TeamSettingDto { EstimationAdditionalFieldDefinitionId = null, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsEstimationFieldDefinitionId()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationAdditionalFieldDefinitionId = 7, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationAdditionalFieldDefinitionId, Is.EqualTo(7));
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsEstimationFieldWhenNull()
        {
            var portfolio = new Portfolio { EstimationAdditionalFieldDefinitionId = 7 };
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationAdditionalFieldDefinitionId = null, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void SyncTeamWithTeamSettings_SyncsEstimationUnit()
        {
            var team = new Team();
            var dto = new TeamSettingDto { EstimationUnit = "Points", WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationUnit, Is.EqualTo("Points"));
        }

        [Test]
        public void SyncTeamWithTeamSettings_ClearsEstimationUnitWhenNull()
        {
            var team = new Team { EstimationUnit = "Points" };
            var dto = new TeamSettingDto { EstimationUnit = null, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationUnit, Is.Null);
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsEstimationUnit()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationUnit = "T-Shirt", WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationUnit, Is.EqualTo("T-Shirt"));
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsEstimationUnitWhenNull()
        {
            var portfolio = new Portfolio { EstimationUnit = "T-Shirt" };
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationUnit = null, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationUnit, Is.Null);
        }
    }
}
