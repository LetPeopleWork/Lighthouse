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

            resolverMock
                .Setup(resolver => resolver.OffersSource(It.IsAny<Portfolio>(), It.IsAny<string>()))
                .Returns(true);

            subject = new DeliverySourceSyncService(
                resolverMock.Object, clock, Mock.Of<ILogger<DeliverySourceSyncService>>());
        }

        /// <summary>
        /// A connection that has stopped offering the source answers a read by throwing, and a throw
        /// says nothing about whether the source is finished or the remote merely unreachable. Asking
        /// first is what keeps those two apart, which is the distinction slice 03's broken-source state
        /// rests on.
        /// </summary>
        [Test]
        public async Task A_connection_that_no_longer_offers_the_source_is_not_asked_about_one()
        {
            var portfolio = APortfolio();
            var delivery = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            resolverMock
                .Setup(resolver => resolver.OffersSource(portfolio, ReleaseSourceKey))
                .Returns(false);

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                resolverMock.Verify(
                    resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()),
                    Times.Never);
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
            }
        }

        [Test]
        public async Task A_connection_that_stopped_offering_one_source_still_syncs_the_Deliveries_on_another()
        {
            var portfolio = APortfolio();
            var onTheSourceThatWent = ADeliveryFollowing(ASecondSourceKey, ASecondRelease);
            var onTheSourceThatStayed = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            resolverMock.Setup(resolver => resolver.OffersSource(portfolio, ASecondSourceKey)).Returns(false);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([onTheSourceThatWent, onTheSourceThatStayed]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(onTheSourceThatWent.SourceLastSyncedOn, Is.Null);
                Assert.That(onTheSourceThatStayed.Name, Is.EqualTo(TheNameItHasNow));
            }
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

            Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(clock.TodayAsUtcMidnight));
        }

        /// <summary>
        /// The day the source was heard from, not the minute. Every bound Delivery of a Portfolio is
        /// saved in one transaction that is dropped whole if any row's version has moved, so a row that
        /// joins that save on every refresh for a value nobody reads to the minute is a row that can
        /// cost every other Delivery its refresh. Writing the same day back is not a change at all.
        /// </summary>
        [Test]
        public async Task Hearing_from_a_Release_twice_in_one_day_records_the_same_thing_both_times()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));
            var afterTheFirstRefresh = delivery.SourceLastSyncedOn;

            clock.SetInstant(TheRefreshRanAt.AddHours(6));
            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(afterTheFirstRefresh));
        }

        [Test]
        public async Task Hearing_from_a_Release_on_a_new_day_records_the_new_day()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));
            var afterTheFirstRefresh = delivery.SourceLastSyncedOn;

            clock.SetInstant(TheRefreshRanAt.AddDays(1));
            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceLastSyncedOn, Is.Not.EqualTo(afterTheFirstRefresh));
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

        /// <summary>
        /// The transition table, read from the outside. A source that resolved to nothing is finished
        /// and says which way; a source that could not be reached says nothing at all, because a
        /// network blip must never read as a Release somebody deleted.
        /// </summary>
        [TestCaseSource(nameof(EveryVerdictAndWhatTheDeliveryEndsUpSaying))]
        public async Task What_a_Delivery_ends_up_saying_about_its_source_follows_from_the_verdict(
            DeliverySourceResolution whatCameBack, DeliverySourceUnavailableReason? expected)
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>
            {
                [TheRelease] = new(whatCameBack, [], 0),
            });

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> EveryVerdictAndWhatTheDeliveryEndsUpSaying()
        {
            yield return new TestCaseData(
                    new DeliverySourceResolution.NotFound(),
                    DeliverySourceUnavailableReason.SourceNotFound)
                .SetName("A Release that is gone says it is gone");

            yield return new TestCaseData(
                    new DeliverySourceResolution.NoDate("2026 Q4"),
                    DeliverySourceUnavailableReason.SourceHasNoDate)
                .SetName("A Release that lost its date says that instead");

            yield return new TestCaseData(
                    new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.CapabilityWithdrawn),
                    DeliverySourceUnavailableReason.CapabilityWithdrawn)
                .SetName("A connection that no longer offers Releases says so");

            yield return new TestCaseData(
                    new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.SourceReadFailed),
                    null)
                .SetName("A Release that could not be read says nothing at all");
        }

        /// <summary>
        /// A connection that stopped offering the source is not asked about one, and every Delivery
        /// following it has to be told - otherwise the one permanent failure that is about the
        /// connection rather than about a Release would be the only one that stays silent.
        /// </summary>
        [Test]
        public async Task A_connection_that_no_longer_offers_the_source_tells_every_Delivery_that_followed_one()
        {
            var portfolio = APortfolio();
            var first = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            var second = ADeliveryFollowing(ReleaseSourceKey, ASecondRelease);
            resolverMock.Setup(resolver => resolver.OffersSource(portfolio, ReleaseSourceKey)).Returns(false);

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([first, second]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.CapabilityWithdrawn));
                Assert.That(second.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.CapabilityWithdrawn));
            }
        }

        /// <summary>
        /// Asking throwing is not the same as the connection having withdrawn the source: a credential
        /// that cannot be decrypted and a socket that went away throw too. Flagging on a throw would put
        /// every Delivery into a broken-source state the first time Jira had a bad minute.
        /// </summary>
        [Test]
        public async Task A_source_that_threw_when_asked_leaves_the_Delivery_saying_nothing_about_it()
        {
            var portfolio = APortfolio();
            var delivery = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            GivenTheSourceCannotBeAskedAtAll(ReleaseSourceKey, new HttpRequestException("the remote closed the connection"));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        [Test]
        public async Task A_Release_that_answers_again_takes_the_notice_off_the_Delivery()
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);
            GivenTheReleaseResolvesTo(TheRelease, ARelease(TheNameItHasNow, TheDateItHasNow));

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
                Assert.That(delivery.Name, Is.EqualTo(TheNameItHasNow));
            }
        }

        [TestCaseSource(nameof(EveryAnswerThatIsNotALiveRelease))]
        public async Task A_Release_that_did_not_come_back_as_a_live_one_leaves_every_value_it_gave_the_Delivery(
            DeliverySourceResolution whatCameBack)
        {
            var (portfolio, delivery) = APortfolioWithADeliveryFollowingTheRelease();
            var whatItSaidBefore = (delivery.Name, delivery.Date);
            GivenTheResolverAnswers(ReleaseSourceKey, new Dictionary<string, PortfolioSourcePreview>
            {
                [TheRelease] = new(whatCameBack, [], 0),
            });

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((delivery.Name, delivery.Date), Is.EqualTo(whatItSaidBefore),
                    "the values are frozen whatever went wrong - they are why the Delivery is still worth reading.");
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
                .SetName("The connection stopped offering the source");
            yield return new TestCaseData(
                    new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.SourceReadFailed))
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
        [TestCaseSource(nameof(EveryWayAskingCanFailOutright))]
        public async Task A_source_that_throws_instead_of_answering_leaves_the_rest_of_the_refresh_standing(
            Exception howItFailed)
        {
            var portfolio = APortfolio();
            var onASourceThatThrows = ADeliveryFollowing(ASecondSourceKey, ASecondRelease);
            var onASourceThatAnswers = ADeliveryFollowing(ReleaseSourceKey, TheRelease);
            GivenTheSourceCannotBeAskedAtAll(ASecondSourceKey, howItFailed);
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

        /// <summary>
        /// A row can come back from the database saying it follows a source while naming none of it.
        /// The aggregate refuses to be put into that state today, but nothing stops a row already in it
        /// - an interrupted write, a hand-edited database, a column added by a later migration - and
        /// asking about it would key the whole batch on a source nobody named.
        /// </summary>
        [TestCaseSource(nameof(EveryHalfWrittenBinding))]
        public async Task A_Delivery_that_says_it_follows_a_source_while_naming_none_is_never_asked_about(
            string? sourceKey, string? sourceReference)
        {
            var portfolio = APortfolio();
            var halfWritten = new Delivery
            {
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = sourceKey,
                SourceReference = sourceReference,
            };

            await subject.ResyncSourceBoundDeliveries(portfolio, new RecordableDeliveries([halfWritten]));

            resolverMock.Verify(
                resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()),
                Times.Never);
        }

        private static IEnumerable<TestCaseData> EveryHalfWrittenBinding()
        {
            yield return new TestCaseData(null, TheRelease).SetName("No source key");
            yield return new TestCaseData(string.Empty, TheRelease).SetName("An empty source key");
            yield return new TestCaseData(ReleaseSourceKey, null).SetName("No reference");
            yield return new TestCaseData(ReleaseSourceKey, string.Empty).SetName("An empty reference");
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

        private void GivenTheSourceCannotBeAskedAtAll(string sourceKey, Exception howItFailed)
        {
            resolverMock
                .Setup(resolver => resolver.ResolveForPortfolio(It.IsAny<Portfolio>(), sourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ThrowsAsync(howItFailed);
        }

        /// <summary>
        /// Three unrelated failures, because the guard exists to catch whatever asking can raise rather
        /// than the one shape a Jira connection happens to throw today. Narrowed to that one shape, a
        /// credential that can no longer be decrypted and a socket that went away would each take a
        /// whole Portfolio's refresh down with them.
        /// </summary>
        private static IEnumerable<TestCaseData> EveryWayAskingCanFailOutright()
        {
            yield return new TestCaseData(new ArgumentException("this connection does not offer that source"))
                .SetName("The connection no longer offers the source");
            yield return new TestCaseData(new HttpRequestException("the remote closed the connection"))
                .SetName("The remote could not be reached");
            yield return new TestCaseData(new InvalidOperationException("the stored credential could not be read"))
                .SetName("The credential could not be read");
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
