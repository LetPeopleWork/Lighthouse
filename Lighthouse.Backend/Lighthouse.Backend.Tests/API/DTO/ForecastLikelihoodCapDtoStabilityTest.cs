using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ForecastLikelihoodCapDtoStabilityTest
    {
        [Test]
        public void LikelihoodFields_RemainPlainNumbers()
        {
            // ADR-038 D2 keeps *presentation*-derived state off the wire - ">95%" is a label, not a number.
            // ADR-112 makes the two multi-team-reachable fields nullable, which is a domain fact rather than
            // a display concern: when a contributing team has no throughput the value does not exist, and
            // null is the shape DeliveryMetricSnapshot and DeliveryMetricsHistoryDto already use for it.
            // The manual forecast is single-team and keeps a plain double.
            using (Assert.EnterMultipleScope())
            {
                Assert.That(typeof(ManualForecastDto).GetProperty(nameof(ManualForecastDto.Likelihood))!.PropertyType, Is.EqualTo(typeof(double)));
                Assert.That(typeof(DeliveryWithLikelihoodDto).GetProperty(nameof(DeliveryWithLikelihoodDto.LikelihoodPercentage))!.PropertyType, Is.EqualTo(typeof(double?)));
                Assert.That(typeof(FeatureLikelihoodDto).GetProperty(nameof(FeatureLikelihoodDto.LikelihoodPercentage))!.PropertyType, Is.EqualTo(typeof(double?)));
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
