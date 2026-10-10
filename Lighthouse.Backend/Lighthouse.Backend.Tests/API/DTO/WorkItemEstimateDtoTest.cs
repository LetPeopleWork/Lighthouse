using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    public class WorkItemEstimateDtoTest
    {
        private const string Pending = "Not built yet: the estimate on each Work Item row. Unskip one at a time while building it.";
        private const int EstimationFieldId = 42;
        private const string StoryPoints = "Story Points";

        private static Portfolio NumericEstimationPortfolio() => new()
        {
            Name = "Ocean Explorer",
            EstimationAdditionalFieldDefinitionId = EstimationFieldId,
            EstimationUnit = StoryPoints,
        };

        private static Portfolio TShirtEstimationPortfolio() => new()
        {
            Name = "Apollo",
            EstimationAdditionalFieldDefinitionId = EstimationFieldId,
            UseNonNumericEstimation = true,
            EstimationCategoryValues = ["S", "M", "L"],
        };

        private static Feature FeatureEstimatedAs(string? estimate)
        {
            var feature = new Feature { ReferenceId = "FTR-1", Name = "Checkout revamp" };
            if (estimate != null)
            {
                feature.AdditionalFieldValues[EstimationFieldId] = estimate;
            }

            return feature;
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void For_OwnerWithoutAnEstimationField_IsNothing_SoNoEstimateColumnIsOffered()
        {
            var portfolio = new Portfolio { Name = "Orion" };

            var estimate = WorkItemEstimateDto.For(portfolio, FeatureEstimatedAs("5"));

            Assert.That(estimate, Is.Null);
        }

        // @us-01 @slice-01a @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void For_NumericEstimate_CarriesTheNumberItsLabelAndTheOwnersUnit()
        {
            var estimate = WorkItemEstimateDto.For(NumericEstimationPortfolio(), FeatureEstimatedAs("5"));

            Assert.That(estimate, Is.EqualTo(new WorkItemEstimateDto(5, "5", StoryPoints)));
        }

        // @us-01 @slice-01a @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void For_CategoryEstimate_SortsByTheCategorysPlaceAndShowsItsName()
        {
            var estimate = WorkItemEstimateDto.For(TShirtEstimationPortfolio(), FeatureEstimatedAs("l"));

            Assert.That(estimate, Is.EqualTo(new WorkItemEstimateDto(2, "L", null)));
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [TestCase("XXL")]
        [TestCase("")]
        [TestCase(null)]
        [Ignore(Pending)]
        public void For_EstimateTheChartLeavesOut_IsConfiguredButEmpty(string? value)
        {
            var estimate = WorkItemEstimateDto.For(TShirtEstimationPortfolio(), FeatureEstimatedAs(value));

            Assert.That(estimate, Is.EqualTo(new WorkItemEstimateDto(null, null, null)));
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [TestCase("big")]
        [TestCase(null)]
        [Ignore(Pending)]
        public void For_NothingUsableInANumericField_IsConfiguredButEmpty_AndKeepsTheUnit(string? value)
        {
            var estimate = WorkItemEstimateDto.For(NumericEstimationPortfolio(), FeatureEstimatedAs(value));

            Assert.That(estimate, Is.EqualTo(new WorkItemEstimateDto(null, null, StoryPoints)));
        }
    }
}
