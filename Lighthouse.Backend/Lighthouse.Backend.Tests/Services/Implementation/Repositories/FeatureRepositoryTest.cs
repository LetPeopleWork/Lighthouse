using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class FeatureRepositoryTest : IntegrationTestBase
    {
        // ForecastService consumes this sequence to decide which Features a team's simulated throughput lands on.
        private static readonly string[] LadderOrder = ["0", "1", "2", "2", "3", "", "", "abc"];
        private static readonly string[] LadderReferenceIds = ["F-3", "F-empty-a", "F-1", "F-2a", "F-2b", "F-abc", "F-empty-b", "F-0"];
        private static readonly string[] LadderOrderWithoutTheBug = ["1", "2", "2", "3", "", "", "abc"];

        [Test]
        public async Task GetAll_OrdersFeaturesByTheFeatureComparerLadder()
        {
            var subject = await GivenTheLadderFixture();

            var orders = subject.GetAll().Select(feature => feature.Order).ToList();

            Assert.That(orders, Is.EqualTo(LadderOrder));
        }

        // Ties must resolve the same way on every read, or rows swap between reads.
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
        public async Task GetByPredicate_NoFeatureMatches_ReturnsNull()
        {
            var subject = await GivenTheLadderFixture();

            var found = subject.GetByPredicate(feature => feature.ReferenceId == "F-missing");

            Assert.That(found, Is.Null);
        }

        [Test]
        public async Task GetAllReferenceIds_ListsTheReferenceIdOfEveryStoredFeature()
        {
            var subject = await GivenTheLadderFixture();

            var referenceIds = subject.GetAllReferenceIds();

            Assert.That(referenceIds, Is.EquivalentTo(LadderReferenceIds));
        }

        [Test]
        public void GetById_NoSuchId_ReturnsNull()
        {
            var subject = CreateSubject();

            Assert.That(subject.GetById(42), Is.Null);
        }

        /// <summary>
        /// The one read a refresh makes to learn which candidates are Features this instance holds. It covers
        /// every Portfolio and the level above their Features alike, because any stored Feature counts.
        /// </summary>
        [Test]
        [Category("story-6301-only-features-as-parent-candidates")]
        [Category("slice-01")]
        public async Task The_stored_Feature_snapshot_holds_every_Feature_with_its_created_date_the_level_above_included()
        {
            var subject = CreateSubject();
            subject.Add(AFeatureCreatedOn("PAY-88", TheFirstOfNovember));
            subject.Add(AFeatureCreatedOn("PAY-7", TheFifteenthOfJanuary, isTheLevelAbove: true));
            await subject.Save();

            var snapshot = subject.GetStoredFeatureCreatedDates();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.CreatedDateOf("PAY-88"), Is.EqualTo(TheFirstOfNovember), "A Portfolio's own Feature, with the date the pick falls back on.");
                Assert.That(snapshot.CreatedDateOf("PAY-7"), Is.EqualTo(TheFifteenthOfJanuary), "The level above is stored as a Feature too, and counts as one.");
                Assert.That(snapshot.Holds("INC-5531"), Is.False, "An issue Lighthouse never stored is not a Feature it holds.");
            }
        }

        /// <summary>
        /// One reference id can be stored more than once. The earliest date that is actually known wins, and
        /// the empty date Jira sends for some migrated issues is not a known one.
        /// </summary>
        [Test]
        [Category("story-6301-only-features-as-parent-candidates")]
        [Category("slice-01")]
        [Category("edge")]
        public async Task A_reference_id_stored_twice_keeps_its_earliest_known_created_date()
        {
            var subject = CreateSubject();
            subject.Add(AFeatureCreatedOn("PAY-88", TheFifteenthOfJanuary));
            subject.Add(AFeatureCreatedOn("PAY-88", DateTime.MinValue));
            subject.Add(AFeatureCreatedOn("PAY-88", TheFirstOfNovember, isTheLevelAbove: true));
            await subject.Save();

            var snapshot = subject.GetStoredFeatureCreatedDates();

            Assert.That(snapshot.CreatedDateOf("PAY-88"), Is.EqualTo(TheFirstOfNovember),
                "Read at face value, the empty date would make this Feature the oldest of all and hand it every tie.");
        }

        /// <summary>
        /// A Feature whose only stored date is the empty one Jira sends for some migrated issues is still a
        /// Feature this instance holds. Dropping it would refuse it as a parent for want of a date.
        /// </summary>
        [Test]
        [Category("story-6301-only-features-as-parent-candidates")]
        [Category("slice-01")]
        [Category("edge")]
        public async Task A_Feature_with_only_an_empty_created_date_is_held_with_no_known_date()
        {
            var subject = CreateSubject();
            subject.Add(AFeatureCreatedOn("PAY-88", DateTime.MinValue));
            await subject.Save();

            var snapshot = subject.GetStoredFeatureCreatedDates();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Holds("PAY-88"), Is.True, "A missing date says nothing about whether the Feature is stored.");
                Assert.That(snapshot.CreatedDateOf("PAY-88"), Is.Null, "The empty date is not a known one.");
            }
        }

        private static readonly DateTime TheFirstOfNovember = new(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime TheFifteenthOfJanuary = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        private static Feature AFeatureCreatedOn(string referenceId, DateTime createdDate, bool isTheLevelAbove = false)
            => new()
            {
                ReferenceId = referenceId,
                Name = referenceId,
                Order = "1",
                Type = "Epic",
                CreatedDate = createdDate,
                IsParentFeature = isTheLevelAbove,
            };

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
