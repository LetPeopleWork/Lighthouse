using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class FeatureRankSeederTest : IntegrationTestBase
    {
        private static readonly string[] TheTrackersSequence = ["Top of the tracker", "Middle of the tracker", "Bottom of the tracker"];

        [Test]
        public async Task SeedMissingRanks_WhenThisInstanceAlreadyOwnsTheOrder_StillPlacesInTheOrderTheTrackerGave()
        {
            // The rows go in back to front, so a seed that numbered them in the sequence the store hands
            // them over comes out exactly reversed rather than accidentally right.
            await GivenFeaturesAddedBackToFront();

            var subject = CreateSubjectWhereThisInstanceOwnsTheOrder();

            await subject.SeedMissingRanks();

            var placed = DatabaseContext.Features
                .OrderBy(feature => feature.ManualRank)
                .Select(feature => feature.Name)
                .ToList();

            Assert.That(placed, Is.EqualTo(TheTrackersSequence).AsCollection);
        }

        private async Task GivenFeaturesAddedBackToFront()
        {
            DatabaseContext.Features.AddRange(
                new Feature { ReferenceId = "F3", Name = "Bottom of the tracker", Order = "30" },
                new Feature { ReferenceId = "F2", Name = "Middle of the tracker", Order = "20" },
                new Feature { ReferenceId = "F1", Name = "Top of the tracker", Order = "10" });

            await DatabaseContext.SaveChangesAsync();
        }

        private FeatureRankSeeder CreateSubjectWhereThisInstanceOwnsTheOrder()
        {
            var policyProvider = new Mock<IFeatureOrderingPolicyProvider>();
            policyProvider.Setup(provider => provider.GetPolicy()).Returns(FeatureOrderingPolicy.ManualOrder);

            return new FeatureRankSeeder(DatabaseContext, new FeatureOrdering(policyProvider.Object));
        }
    }
}
