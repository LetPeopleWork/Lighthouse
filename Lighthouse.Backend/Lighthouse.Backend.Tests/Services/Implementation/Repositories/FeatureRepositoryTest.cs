using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class FeatureRepositoryTest : IntegrationTestBase
    {
        // ForecastService consumes this sequence to decide which Features a team's simulated throughput lands on.
        private static readonly string[] LadderOrder = ["0", "1", "2", "2", "3", "", "", "abc"];
        private static readonly string[] LadderOrderWithoutTheBug = ["1", "2", "2", "3", "", "", "abc"];

        [Test]
        public async Task GetAll_OrdersFeaturesByTheFeatureComparerLadder()
        {
            var subject = await GivenTheLadderFixture();

            var orders = subject.GetAll().Select(feature => feature.Order).ToList();

            Assert.That(orders, Is.EqualTo(LadderOrder));
        }

        // ADR-135's position map numbers rows by this sequence, so a tie has to resolve the same way twice.
        [Test]
        public async Task GetAll_FeaturesTiedOnOrder_ComeBackInAscendingId()
        {
            var subject = await GivenTheLadderFixture();

            var all = subject.GetAll().ToList();

            var tiedOnTwo = all.Where(feature => feature.Order == "2").Select(feature => feature.Id).ToList();
            var tiedOnEmptyOrder = all.Where(feature => string.IsNullOrEmpty(feature.Order)).Select(feature => feature.Id).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tiedOnTwo, Has.Count.EqualTo(2));
                Assert.That(tiedOnTwo, Is.Ordered.Ascending);
                Assert.That(tiedOnEmptyOrder, Has.Count.EqualTo(2));
                Assert.That(tiedOnEmptyOrder, Is.Ordered.Ascending);
            }
        }

        [Test]
        public async Task GetAllByPredicate_AppliesTheSameOrderingToTheFilteredSet()
        {
            var subject = await GivenTheLadderFixture();

            var matching = subject.GetAllByPredicate(feature => feature.Type == "Feature").ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matching.Select(feature => feature.Order).ToList(), Is.EqualTo(LadderOrderWithoutTheBug));
                Assert.That(matching.Where(feature => feature.Order == "2").Select(feature => feature.Id).ToList(), Is.Ordered.Ascending);
            }
        }

        [Test]
        public async Task GetById_ReturnsTheFeatureWithThatId()
        {
            var subject = await GivenTheLadderFixture();
            var expected = subject.GetAll().Single(feature => feature.ReferenceId == "F-abc");

            var found = subject.GetById(expected.Id);

            Assert.That(found, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(found.Id, Is.EqualTo(expected.Id));
                Assert.That(found.Order, Is.EqualTo("abc"));
            }
        }

        [Test]
        public async Task GetByPredicate_ReturnsTheSingleMatchingFeature()
        {
            var subject = await GivenTheLadderFixture();

            var found = subject.GetByPredicate(feature => feature.ReferenceId == "F-2b");

            Assert.That(found, Is.Not.Null);
            Assert.That(found.Order, Is.EqualTo("2"));
        }

        [Test]
        public void GetById_NoSuchId_ReturnsNull()
        {
            var subject = CreateSubject();

            Assert.That(subject.GetById(42), Is.Null);
        }

        private async Task<FeatureRepository> GivenTheLadderFixture()
        {
            var subject = CreateSubject();

            // Seeded out of ladder order so the ordering has something to do.
            subject.Add(AFeature("F-3", "3", "Feature"));
            subject.Add(AFeature("F-empty-a", "", "Feature"));
            subject.Add(AFeature("F-1", "1", "Feature"));
            subject.Add(AFeature("F-2a", "2", "Feature"));
            subject.Add(AFeature("F-2b", "2", "Feature"));
            subject.Add(AFeature("F-abc", "abc", "Feature"));
            subject.Add(AFeature("F-empty-b", "", "Feature"));
            subject.Add(AFeature("F-0", "0", "Bug"));
            await subject.Save();

            return subject;
        }

        private static Feature AFeature(string referenceId, string order, string type)
        {
            return new Feature
            {
                ReferenceId = referenceId,
                Name = referenceId,
                Order = order,
                Type = type,
            };
        }

        private FeatureRepository CreateSubject()
        {
            return new FeatureRepository(DatabaseContext, FeatureOrderingTestHelper.FollowingTheTracker(), Mock.Of<ILogger<FeatureRepository>>());
        }
    }
}
