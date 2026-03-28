using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class StateMappingDtoRoundTripTest
    {
        [Test]
        public void TeamSettingDto_FromTeamWithStateMappings_IncludesStateMappings()
        {
            var team = new Team { Name = "Test Team" };
            team.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            team.StateMappings.Add(new StateMapping { Name = "Waiting", States = ["Blocked"] });

            var dto = new TeamSettingDto(team);

            Assert.That(dto.StateMappings, Has.Count.EqualTo(2));
            Assert.That(dto.StateMappings[0].Name, Is.EqualTo("In Progress"));
            Assert.That(dto.StateMappings[0].States, Is.EquivalentTo(new[] { "Active", "Resolved" }));
            Assert.That(dto.StateMappings[1].Name, Is.EqualTo("Waiting"));
            Assert.That(dto.StateMappings[1].States, Is.EquivalentTo(new[] { "Blocked" }));
        }

        [Test]
        public void TeamSettingDto_FromTeamWithNoStateMappings_ReturnsEmptyList()
        {
            var team = new Team { Name = "Test Team" };

            var dto = new TeamSettingDto(team);

            Assert.That(dto.StateMappings, Is.Empty);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolioWithStateMappings_IncludesStateMappings()
        {
            var portfolio = new Portfolio { Name = "Test Portfolio" };
            portfolio.StateMappings.Add(new StateMapping { Name = "Dev Done", States = ["Resolved", "Verified"] });

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.StateMappings, Has.Count.EqualTo(1));
            Assert.That(dto.StateMappings[0].Name, Is.EqualTo("Dev Done"));
            Assert.That(dto.StateMappings[0].States, Is.EquivalentTo(new[] { "Resolved", "Verified" }));
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolioWithNoStateMappings_ReturnsEmptyList()
        {
            var portfolio = new Portfolio { Name = "Test Portfolio" };

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.StateMappings, Is.Empty);
        }
    }
}
