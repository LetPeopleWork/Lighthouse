using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class BaselineSettingsDtoTest
    {
        [Test]
        public void TeamSettingDto_FromTeam_MapsBaselineStartDate()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var team = new Team { ProcessBehaviourChartBaselineStartDate = start };

            var dto = new TeamSettingDto(team);

            Assert.That(dto.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
        }

        [Test]
        public void TeamSettingDto_FromTeam_MapsBaselineEndDate()
        {
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var team = new Team { ProcessBehaviourChartBaselineEndDate = end };

            var dto = new TeamSettingDto(team);

            Assert.That(dto.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
        }

        [Test]
        public void TeamSettingDto_FromTeam_NullBaselineDatesRemainNull()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.ProcessBehaviourChartBaselineStartDate, Is.Null);
                Assert.That(dto.ProcessBehaviourChartBaselineEndDate, Is.Null);
            }
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsBaselineStartDate()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var portfolio = new Portfolio { ProcessBehaviourChartBaselineStartDate = start };

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsBaselineEndDate()
        {
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var portfolio = new Portfolio { ProcessBehaviourChartBaselineEndDate = end };

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_NullBaselineDatesRemainNull()
        {
            var portfolio = new Portfolio();

            var dto = new PortfolioSettingDto(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.ProcessBehaviourChartBaselineStartDate, Is.Null);
                Assert.That(dto.ProcessBehaviourChartBaselineEndDate, Is.Null);
            }
        }
    }
}
