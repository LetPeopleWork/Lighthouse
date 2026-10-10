using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class EstimateNormalizerEstimateOfTest
    {
        private const string Pending = "Not built yet: one reader for every estimate. Unskip one at a time while building it.";
        private const int EstimationFieldId = 42;
        private const int SomeOtherFieldId = 7;

        private static Team NumericEstimationTeam() => new()
        {
            Name = "Team Zenith",
            EstimationAdditionalFieldDefinitionId = EstimationFieldId,
            EstimationUnit = "Story Points",
        };

        private static Team TShirtEstimationTeam() => new()
        {
            Name = "Team Voyager",
            EstimationAdditionalFieldDefinitionId = EstimationFieldId,
            UseNonNumericEstimation = true,
            EstimationCategoryValues = ["XS", "S", "M", "L"],
        };

        private static WorkItem ItemEstimatedAs(string referenceId, string? estimate)
        {
            var item = new WorkItem { ReferenceId = referenceId, Name = referenceId };
            item.AdditionalFieldValues[SomeOtherFieldId] = "13";
            if (estimate != null)
            {
                item.AdditionalFieldValues[EstimationFieldId] = estimate;
            }

            return item;
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimateOf_OwnerWithoutAnEstimationField_IsNothing()
        {
            var team = new Team { Name = "Team Pulsar" };

            var result = EstimateNormalizer.EstimateOf(team, ItemEstimatedAs("ST-1", "3"));

            Assert.That(result, Is.Null);
        }

        // @us-01 @slice-01a @example @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimateOf_NumericField_IsTheNumberTheChartPlots()
        {
            var result = EstimateNormalizer.EstimateOf(NumericEstimationTeam(), ItemEstimatedAs("ST-1", "3.5"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result?.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result?.NumericValue, Is.EqualTo(3.5));
                Assert.That(result?.DisplayValue, Is.EqualTo("3.5"));
            }
        }

        // @us-01 @slice-01a @example @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimateOf_CategoryField_IsTheCategorysPlaceInTheOwnersListAndItsName()
        {
            var result = EstimateNormalizer.EstimateOf(TShirtEstimationTeam(), ItemEstimatedAs("ST-1", "m"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result?.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result?.NumericValue, Is.EqualTo(2));
                Assert.That(result?.DisplayValue, Is.EqualTo("M"));
            }
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimateOf_CategoryMissingFromTheOwnersList_IsUnmapped()
        {
            var result = EstimateNormalizer.EstimateOf(TShirtEstimationTeam(), ItemEstimatedAs("ST-1", "XXL"));

            Assert.That(result?.Status, Is.EqualTo(EstimateNormalizationStatus.Unmapped));
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [TestCase("")]
        [TestCase("about three")]
        [Ignore(Pending)]
        public void EstimateOf_NothingUsableInANumericField_IsInvalid(string estimate)
        {
            var result = EstimateNormalizer.EstimateOf(NumericEstimationTeam(), ItemEstimatedAs("ST-1", estimate));

            Assert.That(result?.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [TestCase(false)]
        [TestCase(true)]
        [Ignore(Pending)]
        public void EstimateOf_ItemThatNeverReceivedTheField_IsInvalidWithNothingToShow(bool categories)
        {
            var team = categories ? TShirtEstimationTeam() : NumericEstimationTeam();
            var item = ItemEstimatedAs("ST-1", null);

            var result = EstimateNormalizer.EstimateOf(team, item);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.AdditionalFieldValues.ContainsKey(EstimationFieldId), Is.False);
                Assert.That(result?.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
                Assert.That(result?.DisplayValue, Is.Empty);
            }
        }

        // @us-01 @slice-01a @error @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimatesOf_OwnerWithoutAnEstimationField_IsNothing()
        {
            var team = new Team { Name = "Team Pulsar" };

            var result = EstimateNormalizer.EstimatesOf(team, [ItemEstimatedAs("ST-1", "3")]);

            Assert.That(result, Is.Null);
        }

        // @us-01 @slice-01a @example @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void EstimatesOf_CountsTheItemsTheChartCanPlaceAndThoseItCannot()
        {
            List<WorkItemBase> items =
            [
                ItemEstimatedAs("ST-1", "M"),
                ItemEstimatedAs("ST-2", "XXL"),
                ItemEstimatedAs("ST-3", null),
                ItemEstimatedAs("ST-4", "XS"),
            ];

            var result = EstimateNormalizer.EstimatesOf(TShirtEstimationTeam(), items);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result?.TotalCount, Is.EqualTo(4));
                Assert.That(result?.MappedCount, Is.EqualTo(2));
                Assert.That(result?.UnmappedCount, Is.EqualTo(1));
                Assert.That(result?.InvalidCount, Is.EqualTo(1));
            }
        }

        // @us-01 @slice-01a @example @contract-shape:pure-function
        [TestCaseSource(nameof(EstimatesAcrossBothModes))]
        [Ignore(Pending)]
        public void EstimateOf_AgreesWithTheBatchTheChartReads(bool categories, string? estimate)
        {
            var team = categories ? TShirtEstimationTeam() : NumericEstimationTeam();
            var item = ItemEstimatedAs("ST-1", estimate);

            var single = EstimateNormalizer.EstimateOf(team, item);
            var batch = EstimateNormalizer.EstimatesOf(team, [item]);

            Assert.That(single, Is.EqualTo(batch?.Results[0]));
        }

        private static IEnumerable<TestCaseData> EstimatesAcrossBothModes()
        {
            foreach (var estimate in new[] { "3", "0.25", " 8 ", "M", "xs", "XXL", "", null, "five" })
            {
                yield return new TestCaseData(false, estimate);
                yield return new TestCaseData(true, estimate);
            }
        }
    }
}
