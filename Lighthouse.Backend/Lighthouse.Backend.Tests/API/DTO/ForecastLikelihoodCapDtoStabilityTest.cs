using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ForecastLikelihoodCapDtoStabilityTest
    {
        [Test]
        public void LikelihoodFields_RemainDouble()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(typeof(ManualForecastDto).GetProperty(nameof(ManualForecastDto.Likelihood))!.PropertyType, Is.EqualTo(typeof(double)));
                Assert.That(typeof(DeliveryWithLikelihoodDto).GetProperty(nameof(DeliveryWithLikelihoodDto.LikelihoodPercentage))!.PropertyType, Is.EqualTo(typeof(double)));
                Assert.That(typeof(FeatureLikelihoodDto).GetProperty(nameof(FeatureLikelihoodDto.LikelihoodPercentage))!.PropertyType, Is.EqualTo(typeof(double)));
            }
        }

        [Test]
        public void NoCapPresentationSiblingFields_AreIntroduced()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(typeof(ManualForecastDto).GetProperty("IsCapped"), Is.Null);
                Assert.That(typeof(ManualForecastDto).GetProperty("LikelihoodBand"), Is.Null);
                Assert.That(typeof(DeliveryWithLikelihoodDto).GetProperty("IsCapped"), Is.Null);
                Assert.That(typeof(DeliveryWithLikelihoodDto).GetProperty("LikelihoodBand"), Is.Null);
                Assert.That(typeof(FeatureLikelihoodDto).GetProperty("IsCapped"), Is.Null);
                Assert.That(typeof(FeatureLikelihoodDto).GetProperty("LikelihoodBand"), Is.Null);
            }
        }
    }
}
