using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class EstimationFieldDtoTest
    {
        [Test]
        public void TeamSettingDto_FromTeam_MapsEstimationFieldDefinitionId()
        {
            var team = new Team { EstimationAdditionalFieldDefinitionId = 42 };

            var dto = new TeamSettingDto(team);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.EqualTo(42));
        }

        [Test]
        public void TeamSettingDto_FromTeam_NullEstimationFieldRemainsNull()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsEstimationFieldDefinitionId()
        {
            var portfolio = new Portfolio { EstimationAdditionalFieldDefinitionId = 7 };

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.EqualTo(7));
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_NullEstimationFieldRemainsNull()
        {
            var portfolio = new Portfolio();

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.Null);
        }
    }
}
