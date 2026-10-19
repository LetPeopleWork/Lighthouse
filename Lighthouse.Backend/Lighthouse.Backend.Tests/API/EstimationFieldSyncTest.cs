using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;
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
            var team = new Team().OwningEstimationField(42);
            var dto = new TeamSettingDto { EstimationSource = SettingSource.Own, EstimationAdditionalFieldDefinitionId = null, WorkTrackingSystemConnectionId = 1 };

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
        public void SyncWithPortfolioSettings_TakesTheParentOverrideFieldAsThePortfoliosOwn()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { ParentOverrideAdditionalFieldDefinitionId = 9, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.ParentOverrideSource, Is.EqualTo(SettingSource.Own));
                Assert.That(portfolio.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(9));
            }
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsEstimationFieldWhenNull()
        {
            var portfolio = new Portfolio().OwningEstimationField(7);
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationSource = SettingSource.Own, EstimationAdditionalFieldDefinitionId = null, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void SyncTeamWithTeamSettings_SyncsEstimationUnit()
        {
            var team = new Team();
            var dto = new TeamSettingDto { EstimationSource = SettingSource.Own, EstimationUnit = "Points", WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationUnit, Is.EqualTo("Points"));
        }

        [Test]
        public void SyncTeamWithTeamSettings_ClearsEstimationUnitWhenNull()
        {
            var team = new Team().OwningEstimationUnit("Points");
            var dto = new TeamSettingDto { EstimationSource = SettingSource.Own, EstimationUnit = null, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationUnit, Is.Null);
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsEstimationUnit()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationSource = SettingSource.Own, EstimationUnit = "T-Shirt", WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationUnit, Is.EqualTo("T-Shirt"));
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsEstimationUnitWhenNull()
        {
            var portfolio = new Portfolio().OwningEstimationUnit("T-Shirt");
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationSource = SettingSource.Own, EstimationUnit = null, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationUnit, Is.Null);
        }

        [Test]
        public void SyncTeamWithTeamSettings_SyncsUseNonNumericEstimation()
        {
            var team = new Team();
            var dto = new TeamSettingDto { EstimationSource = SettingSource.Own, UseNonNumericEstimation = true, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.UseNonNumericEstimation, Is.True);
        }

        [Test]
        public void SyncTeamWithTeamSettings_SyncsEstimationCategoryValues()
        {
            var team = new Team();
            var dto = new TeamSettingDto { EstimationSource = SettingSource.Own, EstimationCategoryValues = ["XS", "S", "M"], WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.EstimationCategoryValues, Is.EqualTo(new List<string> { "XS", "S", "M" }));
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsUseNonNumericEstimation()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationSource = SettingSource.Own, UseNonNumericEstimation = true, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.UseNonNumericEstimation, Is.True);
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsEstimationCategoryValues()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { EstimationSource = SettingSource.Own, EstimationCategoryValues = ["S", "M", "L"], WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.EstimationCategoryValues, Is.EqualTo(new List<string> { "S", "M", "L" }));
        }
    }
}
