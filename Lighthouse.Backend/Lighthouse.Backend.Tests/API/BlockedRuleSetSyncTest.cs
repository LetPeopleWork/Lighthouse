using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class BlockedRuleSetSyncTest
    {
        [Test]
        public void SyncTeamWithTeamSettings_SyncsBlockedRuleSetJson()
        {
            var team = new Team();
            var ruleSetJson = "{\"version\":1,\"mode\":\"and\",\"conditions\":[]}";
            var dto = new TeamSettingDto { BlockedRuleSetJson = ruleSetJson, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.BlockedRuleSetJson, Is.EqualTo(ruleSetJson));
        }

        [Test]
        public void SyncTeamWithTeamSettings_ClearsBlockedRuleSetJsonWhenNull()
        {
            var team = new Team { BlockedRuleSetJson = "{\"version\":1,\"mode\":\"and\",\"conditions\":[]}" };
            var dto = new TeamSettingDto { BlockedRuleSetJson = null, WorkTrackingSystemConnectionId = 1 };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.BlockedRuleSetJson, Is.Null);
        }

        [Test]
        public void SyncWithPortfolioSettings_SyncsBlockedRuleSetJson()
        {
            var portfolio = new Portfolio();
            var teamRepoMock = new Mock<IRepository<Team>>();
            var ruleSetJson = "{\"version\":1,\"mode\":\"and\",\"conditions\":[]}";
            var dto = new PortfolioSettingDto { BlockedRuleSetJson = ruleSetJson, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.BlockedRuleSetJson, Is.EqualTo(ruleSetJson));
        }

        [Test]
        public void SyncWithPortfolioSettings_ClearsBlockedRuleSetJsonWhenNull()
        {
            var portfolio = new Portfolio { BlockedRuleSetJson = "{\"version\":1,\"mode\":\"and\",\"conditions\":[]}" };
            var teamRepoMock = new Mock<IRepository<Team>>();
            var dto = new PortfolioSettingDto { BlockedRuleSetJson = null, WorkTrackingSystemConnectionId = 1 };

            portfolio.SyncWithPortfolioSettings(dto, teamRepoMock.Object);

            Assert.That(portfolio.BlockedRuleSetJson, Is.Null);
        }
    }
}
