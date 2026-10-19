using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class EstimationFieldDtoTest
    {
        [Test]
        public void TeamSettingDto_FromTeam_MapsEstimationFieldDefinitionId()
        {
            var team = new Team().OwningEstimationField(42);

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.EqualTo(42));
        }

        [Test]
        public void TeamSettingDto_FromTeam_NullEstimationFieldRemainsNull()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsEstimationFieldDefinitionId()
        {
            var portfolio = new Portfolio().OwningEstimationField(7);

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

        [Test]
        public void TeamSettingDto_FromTeam_MapsEstimationUnit()
        {
            var team = new Team().OwningEstimationUnit("Points");

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationUnit, Is.EqualTo("Points"));
        }

        [Test]
        public void TeamSettingDto_FromTeam_NullEstimationUnitRemainsNull()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationUnit, Is.Null);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsEstimationUnit()
        {
            var portfolio = new Portfolio().OwningEstimationUnit("T-Shirt");

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.EstimationUnit, Is.EqualTo("T-Shirt"));
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_NullEstimationUnitRemainsNull()
        {
            var portfolio = new Portfolio();

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.EstimationUnit, Is.Null);
        }

        [Test]
        public void TeamSettingDto_FromTeam_MapsUseNonNumericEstimation()
        {
            var team = new Team().OwningNonNumericEstimation(true);

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.UseNonNumericEstimation, Is.True);
        }

        [Test]
        public void TeamSettingDto_FromTeam_DefaultUseNonNumericEstimationIsFalse()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.UseNonNumericEstimation, Is.False);
        }

        [Test]
        public void TeamSettingDto_FromTeam_MapsEstimationCategoryValues()
        {
            var team = new Team().OwningEstimationCategories(["XS", "S", "M", "L", "XL"]);

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationCategoryValues, Is.EqualTo(new List<string> { "XS", "S", "M", "L", "XL" }));
        }

        [Test]
        public void TeamSettingDto_FromTeam_EmptyEstimationCategoryValues()
        {
            var team = new Team();

            var dto = new TeamSettingDto(team, TestToday.Ambient);

            Assert.That(dto.EstimationCategoryValues, Is.Empty);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsUseNonNumericEstimation()
        {
            var portfolio = new Portfolio().OwningNonNumericEstimation(true);

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.UseNonNumericEstimation, Is.True);
        }

        [Test]
        public void PortfolioSettingDto_FromPortfolio_MapsEstimationCategoryValues()
        {
            var portfolio = new Portfolio().OwningEstimationCategories(["S", "M", "L"]);

            var dto = new PortfolioSettingDto(portfolio);

            Assert.That(dto.EstimationCategoryValues, Is.EqualTo(new List<string> { "S", "M", "L" }));
        }

        [Test]
        [Category("story-6313")]
        public void TeamSettingDto_SentWithNullCategories_SendsTheEstimationWithoutCategories()
        {
            var dto = new TeamSettingDto { EstimationAdditionalFieldDefinitionId = 42, EstimationUnit = "Points", EstimationCategoryValues = null };

            Assert.That(dto.EstimationAsSent(), Is.EqualTo(new EstimationBundle(42, "Points", false, [])));
        }
    }
}
