using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.History;

namespace Lighthouse.Backend.Tests.Models.History
{
    public class FeatureHistoryEntryTest
    {
        private Feature feature;

        [SetUp]
        public void SetUp()
        {
            feature = new Feature { Id = 12, ReferenceId = "42" };

            var team = new Team { Id = 1337 };

            feature.FeatureWork.Add(new FeatureWork(team, 10, 42, feature));
            feature.Forecasts.Add(new WhenForecast { NumberOfItems = 1337 });
        }

        [Test]
        public void Update_NoFeatureWork_SetsNewFeatureWork()
        {
            var subject = CreateSubject();

            subject.Update(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(subject.FeatureWork.Single().RemainingWorkItems, Is.EqualTo(10));
            };
        }

        [Test]
        public void Update_NoForecasts_SetsNewForecasts()
        {
            var subject = CreateSubject();

            subject.Update(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecasts, Has.Count.EqualTo(1));
                Assert.That(subject.Forecasts.Single().NumberOfItems, Is.EqualTo(1337));
            };
        }

        [Test]
        public void Update_ExistingFeatureWork_SetsNewFeatureWork()
        {
            var subject = CreateSubject();
            subject.Update(feature);

            feature.FeatureWork.Single().RemainingWorkItems = 7;
            subject.Update(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(subject.FeatureWork.Single().RemainingWorkItems, Is.EqualTo(7));
            };
        }

        [Test]
        public void Update_ExistingForecasts_SetsNewForecasts()
        {
            var subject = CreateSubject();
            subject.Update(feature);

            feature.Forecasts.Single().NumberOfItems = 42;
            subject.Update(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecasts, Has.Count.EqualTo(1));
                Assert.That(subject.Forecasts.Single().NumberOfItems, Is.EqualTo(42));
            };
        }

        private FeatureHistoryEntry CreateSubject()
        {
            return new FeatureHistoryEntry();
        }
    }
}
