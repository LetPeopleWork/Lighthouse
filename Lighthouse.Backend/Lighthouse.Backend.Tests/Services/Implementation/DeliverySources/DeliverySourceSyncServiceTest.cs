using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// The pass that makes a bound Delivery follow its Release without anyone asking it to. It runs
    /// inside the Portfolio refresh, so the thing it must never do is take the refresh down with it:
    /// a source that cannot be read, a connection that has stopped offering the source at all, and a
    /// remote answer the aggregate refuses all leave their own Delivery on its last known values and
    /// every other Delivery syncing.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-02")]
    public class DeliverySourceSyncServiceTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ASecondSourceKey = "jira-fix-version";
        private const string TheRelease = "10412";
        private const string ASecondRelease = "10999";
        private const string TheNameItHasNow = "2026 Q4 (slipped)";
        private const string TrackedItem = "LGH-1";

        private static readonly DateTime TheDateItHasNow = new(2026, 12, 19, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeOffset TheRefreshRanAt = new(2026, 8, 25, 7, 30, 0, TimeSpan.Zero);

        private Mock<IDeliverySourceResolver> resolverMock;
        private FakeLighthouseClock clock;
        private DeliverySourceSyncService subject;

        [SetUp]
        public void SetUp()
        {
            resolverMock = new Mock<IDeliverySourceResolver>();
            clock = new FakeLighthouseClock(TheRefreshRanAt);

            subject = new DeliverySourceSyncService(
                resolverMock.Object, clock, Mock.Of<ILogger<DeliverySourceSyncService>>());
        }

        [Test]
        public async Task A_bound_Delivery_takes_the_name_date_and_Features_its_Release_now_has()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            var theWorkTheReleaseNowCarries = AFeatureNamed(TrackedItem);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow), theWorkTheReleaseNowCarries);

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(TheNameItHasNow));
                Assert.That(delivery.Date, Is.EqualTo(TheDateItHasNow));
                Assert.That(delivery.Features, Is.EqualTo(theWorkTheReleaseNowCarries));
            }
        }

        [Test]
        public async Task A_synced_Delivery_records_the_moment_the_refresh_heard_from_its_Release()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(TheRefreshRanAt.UtcDateTime));
        }

        /// <summary>
        /// A Portfolio with fifty bound Deliveries must cost the refresh what one does, so every
        /// Delivery on the same source is asked about together rather than one call each.
        /// </summary>
        [Test]
        public async Task Every_Delivery_following_the_same_kind_of_source_is_asked_about_in_one_go()
        {
            var portfolio = APortfolio();
            var first = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            var second = ADeliveryFollowing(ReleaseSourceKey, ASecondRelease);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([first, second]));

            resolverMock.Verify(
                resolver => resolver.ResolveForPortfolio(
                    portfolio,
                    ReleaseSourceKey,
                    It.Is<IReadOnlyList<string>>(references => references.Count == 2
                        && references.Contains(TheRelease)
                        && references.Contains(ASecondRelease))),
                Times.Once);
        }

        [Test]
        public async Task Deliveries_following_different_kinds_of_source_are_each_asked_of_the_source_they_follow()
        {
            var portfolio = APortfolio();
            var followingARelease = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            var followingSomethingElse = ADeliveryFollowing(ASecondSourceKey, ASecondRelease);
            GivenNothingResolves();

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([followingARelease, followingSomethingElse]));

            using (Assert.EnterMultipleScope())
            {
                resolverMock.Verify(resolver => resolver.ResolveForPortfolio(portfolio, ReleaseSourceKey, It.IsAny<IReadOnlyList<string>>()), Times.Once);
                resolverMock.Verify(resolver => resolver.ResolveForPortfolio(portfolio, ASecondSourceKey, It.IsAny<IReadOnlyList<string>>()), Times.Once);
            }
        }

        [TestCaseSource(nameof(EveryDeliveryNobodyElseMaintains))]
        public async Task A_Delivery_whose_name_and_date_are_somebody_to_edit_is_never_asked_about(Delivery theirs)
        {
            var portfolio = APortfolio();

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([theirs]));

            resolverMock.Verify(
                resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()),
                Times.Never);
        }

        private static IEnumerable<TestCaseData> EveryDeliveryNobodyElseMaintains()
        {
            yield return new TestCaseData(ADeliveryChosenByHand()).SetName("Chosen by hand");

            var byRule = ADeliveryChosenByHand();
            byRule.SelectFeaturesByRule("{\"conditions\":[]}", 1);
            yield return new TestCaseData(byRule).SetName("Chosen by rule");
        }

        [TestCaseSource(nameof(EveryAnswerThatIsNotALiveRelease))]
        public async Task A_Release_that_did_not_come_back_as_a_live_one_leaves_the_Delivery_exactly_as_it_was(
            DeliverySourceResolution whatCameBack)
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            var whatItSaidBefore = (delivery.Name, delivery.Date, delivery.ConcurrencyToken);
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>
            {
                [TheRelease] = new(whatCameBack, [], 0),
            });

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((delivery.Name, delivery.Date, delivery.ConcurrencyToken), Is.EqualTo(whatItSaidBefore));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null,
                    "an answer that resolved to nothing is not the Release having been heard from.");
            }
        }

        private static IEnumerable<TestCaseData> EveryAnswerThatIsNotALiveRelease()
        {
            yield return new TestCaseData(new DeliverySourceResolution.NotFound()).SetName("The Release is gone");
            yield return new TestCaseData(new DeliverySourceResolution.NoDate("2026 Q4")).SetName("The Release lost its date");
            yield return new TestCaseData(
                    new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.CapabilityWithdrawn))
                .SetName("The Release could not be read");
        }

        [Test]
        public async Task A_reference_the_answer_says_nothing_about_leaves_its_Delivery_exactly_as_it_was()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            var whatItSaidBefore = delivery.Name;
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>());

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(whatItSaidBefore));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
            }
        }

        /// <summary>
        /// A connection that has stopped offering the source throws rather than answering, and the
        /// refresh this pass runs inside carries every other number on the Portfolio. Letting that
        /// reach the refresh would lose all of them over one Delivery nobody can currently read.
        /// </summary>
        [Test]
        public async Task A_source_that_throws_instead_of_answering_leaves_the_rest_of_the_refresh_standing()
        {
            var portfolio = APortfolio();
            var onASourceThatThrows = ADeliveryFollowing(ASecondSourceKey, ASecondRelease);
            var onASourceThatAnswers = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            GivenTheSourceCannotBeAskedAtAll(ASecondSourceKey);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([onASourceThatThrows, onASourceThatAnswers]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(onASourceThatThrows.SourceLastSyncedOn, Is.Null);
                Assert.That(onASourceThatAnswers.Name, Is.EqualTo(TheNameItHasNow),
                    "one unreadable source must not cost every other Delivery its refresh.");
            }
        }

        /// <summary>
        /// A remote can answer with something the Delivery refuses - a Release with a blank name is
        /// the one the aggregate names outright. The Delivery it happened to is left alone, and the
        /// ones beside it are not punished for it.
        /// </summary>
        [Test]
        public async Task An_answer_the_Delivery_refuses_costs_that_Delivery_its_refresh_and_no_other()
        {
            var portfolio = APortfolio();
            var theOneWithTheBadAnswer = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            var theOneBesideIt = ADeliveryFollowing(ReleaseSourceKey, ASecondRelease);
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>
            {
                [TheRelease] = new(new DeliverySourceResolution.Resolved(new DeliverySourceSnapshot(string.Empty, TheDateItHasNow, [])), [], 0),
                [ASecondRelease] = new(new DeliverySourceResolution.Resolved(new DeliverySourceSnapshot(TheNameItHasNow, TheDateItHasNow, [])), [], 0),
            });

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([theOneWithTheBadAnswer, theOneBesideIt]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theOneWithTheBadAnswer.SourceLastSyncedOn, Is.Null);
                Assert.That(theOneBesideIt.Name, Is.EqualTo(TheNameItHasNow));
            }
        }

        [Test]
        public async Task A_Portfolio_with_nothing_following_a_source_asks_no_remote_anything()
        {
            var portfolio = APortfolio();

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([]));

            resolverMock.Verify(
                resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()),
                Times.Never);
        }

        /// <summary>
        /// Two Deliveries following the same Release is a normal thing to set up, and asking about it
        /// twice in one pass would cost the refresh a call it does not need.
        /// </summary>
        [Test]
        public async Task Two_Deliveries_following_the_same_Release_are_asked_about_once_and_both_take_the_answer()
        {
            var portfolio = APortfolio();
            var first = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            var second = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([first, second]));

            using (Assert.EnterMultipleScope())
            {
                resolverMock.Verify(
                    resolver => resolver.ResolveForPortfolio(
                        portfolio, ReleaseSourceKey, It.Is<IReadOnlyList<string>>(references => references.Count == 1)),
                    Times.Once);
                Assert.That(first.Name, Is.EqualTo(TheNameItHasNow));
                Assert.That(second.Name, Is.EqualTo(TheNameItHasNow));
            }
        }

        private static DeliverySourceSnapshot ARelease(string name, DateTime date)
        {
            return new DeliverySourceSnapshot(name, date, []);
        }

        private static List<Feature> AFeatureNamed(string referenceId)
        {
            return [new Feature { Id = 7, ReferenceId = referenceId, Name = referenceId }];
        }

        private void GivenTheReleaseResolvesTo(string sourceReference, DeliverySourceSnapshot snapshot, List<Feature>? trackedFeatures = null)
        {
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>
            {
                [sourceReference] = new(new DeliverySourceResolution.Resolved(snapshot), trackedFeatures ?? [], 0),
            });
        }

        private void GivenTheResolverAnswers(string sourceKey, Dictionary<string, PortfolioSourcePreview> previews)
        {
            resolverMock
                .Setup(resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), sourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(previews);
        }

        private void GivenNothingResolves()
        {
            resolverMock
                .Setup(resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(new Dictionary<string, PortfolioSourcePreview>());
        }

        private void GivenTheSourceCannotBeAskedAtAll(string sourceKey)
        {
            resolverMock
                .Setup(resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), sourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ThrowsAsync(new ArgumentException($"This connection does not offer a delivery source called '{sourceKey}'."));
        }

        private static Portfolio APortfolio()
        {
            return new Portfolio { Id = 1, Name = "Zenith" };
        }

        private static (Portfolio Portfolio, Delivery Delivery) APortfolioWithADeliveryFollowingTheRelease()
        {
            return (APortfolio(), ADeliveryFollowing(ReleaseSourceKey, TheRelease));
        }

        private static Delivery ADeliveryChosenByHand()
        {
            return new Delivery("2026 Q4", new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Utc), 1);
        }

        private static Delivery ADeliveryFollowing(string sourceKey, string sourceReference)
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(sourceKey, sourceReference);

            return delivery;
        }
    }
}
