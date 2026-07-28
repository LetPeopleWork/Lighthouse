using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    // Story #5570 / ADR-112 D8: one un-forecastable feature makes the whole delivery un-forecastable.
    public class DeliveryUnknownForecastDtoTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        [Test]
        public void FromDelivery_OneFeatureCannotBeForecast_ReportsNoDeliveryLikelihood()
        {
            var delivery = DeliveryWith(ForecastableFeature(), UnforecastableFeature("Team Pulsar"));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.LikelihoodPercentage, Is.Null);
                Assert.That(dto.CompletionDates, Is.Empty);
                Assert.That(dto.TeamsWithoutForecast, Is.EqualTo(new[] { "Team Pulsar" }));
            }
        }

        [Test]
        public void FromDelivery_EveryFeatureCanBeForecast_StillReportsANumber()
        {
            var delivery = DeliveryWith(ForecastableFeature(), ForecastableFeature());

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.LikelihoodPercentage, Is.Not.Null);
                Assert.That(dto.TeamsWithoutForecast, Is.Empty);
            }
        }

        [Test]
        public void FromDelivery_SeveralTeamsCannotBeForecast_NamesThemAlphabeticallyWithoutRepeating()
        {
            var delivery = DeliveryWith(
                UnforecastableFeature("Team Voyager"),
                UnforecastableFeature("Team Pulsar"),
                UnforecastableFeature("Team Voyager"));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            Assert.That(dto.TeamsWithoutForecast, Is.EqualTo(new[] { "Team Pulsar", "Team Voyager" }));
        }

        [Test]
        public void FromDelivery_UnforecastableFeature_ReportsNoLikelihoodAndNoDatesForThatFeature()
        {
            var delivery = DeliveryWith(UnforecastableFeature("Team Pulsar"));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            var featureLikelihood = dto.FeatureLikelihoods.Single();

            using (Assert.EnterMultipleScope())
            {
                // The hazard this story exists to close: the empty-histogram path used to report 100.
                Assert.That(featureLikelihood.LikelihoodPercentage, Is.Null);
                Assert.That(featureLikelihood.CompletionDates, Is.Empty);
                Assert.That(featureLikelihood.TeamsWithoutForecast, Is.EqualTo(new[] { "Team Pulsar" }));
            }
        }

        [Test]
        public void FromDelivery_EveryFeatureUnforecastableAndInsufficient_StillReportsInsufficientData()
        {
            // AC-02.4: the signals compose. With no forecastable feature to govern the delivery, the
            // sufficiency answer has to come from the features themselves rather than defaulting to true.
            var delivery = DeliveryWith(
                UnforecastableFeature("Team Pulsar", hasSufficientData: false),
                UnforecastableFeature("Team Voyager", hasSufficientData: true));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            // One insufficient contributor is enough - every feature must be sufficient, not merely one.
            Assert.That(dto.HasSufficientData, Is.False);
        }

        [Test]
        public void FromDelivery_EveryFeatureUnforecastableButSufficient_KeepsReportingSufficientData()
        {
            var delivery = DeliveryWith(
                UnforecastableFeature("Team Pulsar"),
                UnforecastableFeature("Team Voyager"));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.True);
        }

        [Test]
        public void FromDelivery_AForecastableFeatureGovernsSufficiency_EvenWhenAnotherIsUnknown()
        {
            // The governing feature still answers when one exists; the all-features fallback only
            // applies once none of them can be forecast.
            var insufficient = ForecastableFeature();
            insufficient.Forecasts.Single().HasSufficientData = false;

            var delivery = DeliveryWith(insufficient, UnforecastableFeature("Team Pulsar"));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.False);
        }

        [Test]
        public void FromDelivery_ForecastableFeatureIsSufficientButAnUnknownOneIsNot_TheGoverningFeatureStillAnswers()
        {
            // Pins the precedence: the all-features fallback must not take over while a feature that
            // can be forecast is still there to govern the delivery.
            var delivery = DeliveryWith(
                ForecastableFeature(),
                UnforecastableFeature("Team Pulsar", hasSufficientData: false));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, TestToday.Ambient, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.True);
        }

        [Test]
        public void FromDelivery_OneFeatureMissesSeveralTeams_NamesThemAlphabeticallyOnThatFeature()
        {
            var pulsar = new Team { Id = 11, Name = "Team Pulsar" };
            var voyager = new Team { Id = 12, Name = "Team Voyager" };

            var feature = new Feature([(voyager, 3, 3), (pulsar, 2, 2)]);
            feature.SetFeatureForecasts([
                new WhenForecast([]) { Team = voyager, TeamId = voyager.Id },
                new WhenForecast([]) { Team = pulsar, TeamId = pulsar.Id },
            ]);

            var dto = DeliveryWithLikelihoodDto.FromDelivery(DeliveryWith(feature), TestToday.Ambient, NoBlackoutPeriods);

            Assert.That(dto.FeatureLikelihoods.Single().TeamsWithoutForecast, Is.EqualTo(new[] { "Team Pulsar", "Team Voyager" }));
        }

        private static Delivery DeliveryWith(params Feature[] features)
        {
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
            };

            foreach (var feature in features)
            {
                delivery.Features.Add(feature);
            }

            return delivery;
        }

        private static Feature ForecastableFeature()
        {
            var team = new Team { Id = 1, Name = "Team Gravity" };
            var feature = new Feature([(team, 5, 5)]);
            feature.SetFeatureForecasts([new WhenForecast(new Dictionary<int, int> { { 10, 100 } }) { Team = team, TeamId = team.Id, HasSufficientData = true }]);

            return feature;
        }

        private static Feature UnforecastableFeature(string teamName, bool hasSufficientData = true)
        {
            var team = new Team { Id = teamName.GetHashCode(StringComparison.Ordinal), Name = teamName };
            var feature = new Feature([(team, 3, 3)]);
            feature.SetFeatureForecasts([new WhenForecast([]) { Team = team, TeamId = team.Id, HasSufficientData = hasSufficientData }]);

            return feature;
        }
    }
}
