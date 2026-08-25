using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// The pass that broadcasts a Delivery's forecast back onto the Release it follows. It hangs off the
    /// forecast, which carries every number on the Portfolio, so what it must never do is take that down
    /// with it: a connection nobody can resolve, a remote that could not be written to and a Delivery
    /// with nothing to say all leave everything as it stands and the Deliveries beside them published.
    ///
    /// The other half of its job is choosing which Deliveries to broadcast at all, and every exclusion
    /// there prevents a specific wrong statement reaching somebody else's Jira.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-04")]
    public class DeliveryForecastPublishingServiceTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string TheRelease = "10412";
        private const string ASecondRelease = "10999";
        private const string TheRenderedBlock = "rendered block";

        private static readonly DateTimeOffset TheRoundRanAt = new(2026, 8, 25, 7, 30, 0, TimeSpan.Zero);
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];
        private static readonly int[] ThePercentilesTheProductShows = [70, 85, 95];

        private Mock<IWorkTrackingConnectorFactory> connectorFactoryMock;
        private Mock<IDeliveryForecastPublisher> publisherMock;
        private Mock<IDeliveryForecastBlockRenderer> rendererMock;
        private FakeLighthouseClock clock;
        private List<DeliveryForecastBlock> blocksComposed;
        private List<DateTime> calendarWindowEnds;
        private DeliveryForecastPublishingService subject;

        [SetUp]
        public void SetUp()
        {
            blocksComposed = [];
            calendarWindowEnds = [];

            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            publisherMock = new Mock<IDeliveryForecastPublisher>();
            rendererMock = new Mock<IDeliveryForecastBlockRenderer>();
            clock = new FakeLighthouseClock(TheRoundRanAt);

            var blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<DateTime, DateTime>((_, end) => calendarWindowEnds.Add(end))
                .Returns(NoBlackoutPeriods);

            rendererMock
                .Setup(renderer => renderer.Render(It.IsAny<DeliveryForecastBlock>()))
                .Callback<DeliveryForecastBlock>(block => blocksComposed.Add(block))
                .Returns(TheRenderedBlock);

            GivenAConnectionThatCanPublish();
            GivenTheRemoteAccepts();

            subject = new DeliveryForecastPublishingService(
                connectorFactoryMock.Object,
                rendererMock.Object,
                new DeliveryMetricValuesProjector(blackoutPeriodServiceMock.Object),
                clock,
                Mock.Of<ILogger<DeliveryForecastPublishingService>>());
        }

        [Test]
        public async Task A_Delivery_switched_on_has_its_forecast_written_to_the_Release_it_follows()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<DeliveryForecastPublication>(publication =>
                        publication.SourceKey == ReleaseSourceKey
                        && publication.SourceReference == TheRelease
                        && publication.BlockText == TheRenderedBlock)),
                Times.Once);
        }

        /// <summary>
        /// The claim the whole slice rests on: what is written onto the Release is the same forecast the
        /// product shows on its own screen. Pinned against the screen's own projection rather than
        /// against three numbers written out again here, because two hand-written lists would agree on
        /// the day they were written and nothing would notice when one of them moved.
        /// </summary>
        [Test]
        public async Task What_reaches_the_Release_is_what_the_Lighthouse_screen_shows()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            var onScreen = DeliveryWithLikelihoodDto.FromDelivery(delivery, clock.Today, NoBlackoutPeriods);

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            var published = blocksComposed.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(published.Percentiles, Is.Not.Empty);
                Assert.That(
                    published.Percentiles.Select(percentile => (percentile.Percentile, percentile.ExpectedDate)),
                    Is.EqualTo(onScreen.CompletionDates.Select(forecast => (forecast.Probability, DateOnly.FromDateTime(forecast.ExpectedDate)))));
                Assert.That(published.LikelihoodPercentage, Is.EqualTo(onScreen.LikelihoodPercentage));
                Assert.That(published.TargetDate, Is.EqualTo(DateOnly.FromDateTime(delivery.Date)));
                Assert.That(published.WrittenOn, Is.EqualTo(clock.Today));
            }
        }

        [Test]
        public async Task The_block_carries_the_three_percentiles_the_product_shows()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(
                blocksComposed.Single().Percentiles.Select(percentile => percentile.Percentile),
                Is.EqualTo(ThePercentilesTheProductShows));
        }

        /// <summary>
        /// Which non-working days a forecast lands on depends on how far ahead the calendar was asked
        /// about: a recurring shutdown is only worked out for the window it falls inside. Asked over the
        /// Deliveries being broadcast rather than over all of them, a Portfolio whose furthest target
        /// belongs to a Delivery nobody broadcasts gets a shorter window here than the screen uses - and
        /// the date written onto somebody's Release ends up days away from the date Lighthouse shows for
        /// the same Delivery.
        /// </summary>
        [Test]
        public async Task The_calendar_is_read_over_the_whole_Portfolio_the_way_the_screen_reads_it()
        {
            var portfolio = APortfolio();
            var broadcasting = ABroadcastingDelivery(TheRelease, AFeatureWithAForecast());
            var theOneNobodyBroadcasts = ADeliveryChosenByHand(AFeatureWithAForecast());
            theOneNobodyBroadcasts.Reschedule(broadcasting.Date.AddDays(365));

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([broadcasting, theOneNobodyBroadcasts]));

            Assert.That(calendarWindowEnds, Has.All.GreaterThan(theOneNobodyBroadcasts.Date),
                "the window has to cover every Delivery of the Portfolio, or the published dates and the shown dates come off two different calendars.");
        }

        /// <summary>
        /// A Release that currently carries no work at all reads as certain rather than as
        /// unforecastable - the Delivery holds no Features, so there is nothing that cannot be forecast.
        /// It still has no dates to publish, and a block with an empty forecast list would say "0%" on a
        /// customer's Release.
        /// </summary>
        [Test]
        public async Task A_Delivery_whose_Release_carries_no_work_yet_writes_nothing()
        {
            var portfolio = APortfolio();
            var delivery = ABroadcastingDelivery(TheRelease);

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()),
                Times.Never);
        }

        /// <summary>
        /// One reading of the clock for the whole round. Read per Delivery, a round that happens to cross
        /// midnight would stamp two Deliveries of one Portfolio with two different write dates, and a
        /// reader comparing two Releases would take the older stamp for a Release that had stopped being
        /// updated.
        /// </summary>
        [Test]
        public async Task Every_Delivery_published_in_one_round_carries_the_same_write_date()
        {
            var portfolio = APortfolio();
            var first = ABroadcastingDelivery(TheRelease, AFeatureWithAForecast());
            var second = ABroadcastingDelivery(ASecondRelease, AFeatureWithAForecast());
            publisherMock
                .Setup(publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .Callback(() => clock.SetInstant(TheRoundRanAt.AddDays(1)))
                .ReturnsAsync(new DeliveryForecastPublishResult.Published());

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([first, second]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blocksComposed, Has.Count.EqualTo(2));
                Assert.That(blocksComposed.Select(block => block.WrittenOn).Distinct().Count(), Is.EqualTo(1));
            }
        }

        [TestCaseSource(nameof(EveryDeliveryThatMustNotBeBroadcast))]
        public async Task A_Delivery_that_is_not_a_live_bound_one_somebody_switched_on_is_left_alone(Func<Delivery> theDelivery)
        {
            var portfolio = APortfolio();

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([theDelivery()]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()),
                Times.Never);
        }

        private static IEnumerable<TestCaseData> EveryDeliveryThatMustNotBeBroadcast()
        {
            yield return new TestCaseData((Func<Delivery>)ABoundDeliveryNobodySwitchedOn)
                .SetName("Nobody switched it on - publishing is opt-in, and a Release shared with a customer must not start carrying our numbers by default");
            yield return new TestCaseData((Func<Delivery>)AForecastableDeliveryChosenByHand)
                .SetName("Chosen by hand - there is no Release to write to");
            yield return new TestCaseData((Func<Delivery>)ABoundDeliveryWhoseReleaseIsGone)
                .SetName("Its Release is finished - the reference no longer resolves, so the write would go nowhere");
            yield return new TestCaseData((Func<Delivery>)ABoundDeliveryNothingHasResolvedYet)
                .SetName("Nothing has resolved it yet - nobody has confirmed the reference names anything");
        }

        /// <summary>
        /// A Delivery nobody can forecast has no dates and no likelihood, so a block written for it
        /// would carry four required things with three of them blank. Whatever was published last stays
        /// where it is, carrying the date that says how old it is.
        /// </summary>
        [Test]
        public async Task A_Delivery_with_no_forecast_to_give_writes_nothing_rather_than_a_block_of_blanks()
        {
            var portfolio = APortfolio();
            var delivery = ABroadcastingDelivery(TheRelease, AFeatureNobodyCanForecast());

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()),
                Times.Never);
        }

        [Test]
        public async Task A_connection_whose_connector_cannot_publish_at_all_writes_nothing()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            connectorFactoryMock
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(Mock.Of<IWorkTrackingConnector>());

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.Null,
                "a connector that cannot publish says nothing about whether the Release is still there.");
        }

        /// <summary>
        /// Reading Releases and writing to them are two capabilities of the same connection. A
        /// connection that answers no here is exactly the state the refusal report exists for, and it
        /// must not be reached by asking the remote and watching it fail.
        /// </summary>
        [Test]
        public async Task A_connection_that_says_it_may_not_publish_is_not_asked_to()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            publisherMock
                .Setup(publisher => publisher.SupportsDeliveryForecastPublishing(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(false);

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()),
                Times.Never);
        }

        [Test]
        public async Task A_connection_that_cannot_be_resolved_at_all_leaves_every_Delivery_as_it_stands()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            connectorFactoryMock
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Throws(new InvalidOperationException("the credential could not be read"));

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        /// <summary>
        /// The Release was deleted between the read that resolved it and the write. That is the same
        /// finding a failed read makes about the same Release, so it raises the same state - and it is
        /// deliberately not reported as a refusal, which would send an administrator to fix a permission
        /// that was never the problem.
        /// </summary>
        [Test]
        public async Task A_Release_that_is_not_there_any_more_puts_the_Delivery_into_the_broken_source_state()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            GivenTheRemoteAnswers(new DeliveryForecastPublishResult.TargetMissing());

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.SourceNotFound));
        }

        [Test]
        public async Task A_refused_write_is_about_the_credential_and_says_nothing_about_the_Release()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            GivenTheRemoteAnswers(new DeliveryForecastPublishResult.Refused("You do not have permission to edit this version."));

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        [Test]
        public async Task A_write_that_went_through_leaves_the_Delivery_exactly_as_it_was()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            var versionBefore = delivery.ConcurrencyToken;

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
                Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionBefore),
                    "publishing reads the Delivery and writes to Jira; moving its version would expire the copy an open browser is holding.");
            }
        }

        /// <summary>
        /// A remote that could not be reached has said nothing about whether the Release still exists,
        /// which is the one mistake this whole vocabulary exists to prevent.
        /// </summary>
        [Test]
        public async Task A_remote_that_could_not_be_written_to_never_reads_as_a_Release_somebody_deleted()
        {
            var (portfolio, delivery) = APortfolioBroadcastingOneDelivery();
            publisherMock
                .Setup(publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .ThrowsAsync(new HttpRequestException("Jira was briefly unreachable"));

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([delivery]));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        [Test]
        public async Task One_Delivery_the_remote_could_not_take_does_not_cost_the_next_one_its_publish()
        {
            var portfolio = APortfolio();
            var theOneThatFails = ABroadcastingDelivery(TheRelease, AFeatureWithAForecast());
            var theOneBesideIt = ABroadcastingDelivery(ASecondRelease, AFeatureWithAForecast());
            publisherMock
                .Setup(publisher => publisher.PublishAsync(
                    It.IsAny<WorkTrackingSystemConnection>(),
                    It.Is<DeliveryForecastPublication>(publication => publication.SourceReference == TheRelease)))
                .ThrowsAsync(new HttpRequestException("Jira was briefly unreachable"));

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([theOneThatFails, theOneBesideIt]));

            publisherMock.Verify(
                publisher => publisher.PublishAsync(
                    It.IsAny<WorkTrackingSystemConnection>(),
                    It.Is<DeliveryForecastPublication>(publication => publication.SourceReference == ASecondRelease)),
                Times.Once);
        }

        [Test]
        public async Task A_Portfolio_with_nothing_to_broadcast_asks_the_connection_nothing_at_all()
        {
            var portfolio = APortfolio();

            await subject.PublishForPortfolio(portfolio, new RecordableDeliveries([AForecastableDeliveryChosenByHand()]));

            connectorFactoryMock.Verify(
                factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()),
                Times.Never,
                "a Portfolio nobody switched on must not pay for a connector resolution on every forecast.");
        }

        private void GivenAConnectionThatCanPublish()
        {
            publisherMock
                .Setup(publisher => publisher.SupportsDeliveryForecastPublishing(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(true);
            connectorFactoryMock
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(publisherMock.As<IWorkTrackingConnector>().Object);
        }

        private void GivenTheRemoteAccepts()
        {
            GivenTheRemoteAnswers(new DeliveryForecastPublishResult.Published());
        }

        private void GivenTheRemoteAnswers(DeliveryForecastPublishResult result)
        {
            publisherMock
                .Setup(publisher => publisher.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .ReturnsAsync(result);
        }

        private static (Portfolio Portfolio, Delivery Delivery) APortfolioBroadcastingOneDelivery()
        {
            return (APortfolio(), ABroadcastingDelivery(TheRelease, AFeatureWithAForecast()));
        }

        private static Portfolio APortfolio()
        {
            return new Portfolio
            {
                Id = 1,
                Name = "Zenith",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Id = 7,
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };
        }

        private static Delivery ADeliveryChosenByHand(params Feature[] features)
        {
            var delivery = new Delivery("2026 Q4", TheRoundRanAt.UtcDateTime.Date.AddDays(30), 1) { Id = 4711 };
            delivery.ReplaceFeatures(features);

            return delivery;
        }

        private static Delivery AForecastableDeliveryChosenByHand()
        {
            return ADeliveryChosenByHand(AFeatureWithAForecast());
        }

        private static Delivery ABoundDelivery(string sourceReference)
        {
            var delivery = ADeliveryChosenByHand(AFeatureWithAForecast());
            delivery.BindToSource(ReleaseSourceKey, sourceReference);

            return delivery;
        }

        private static Delivery ABoundDeliveryNobodySwitchedOn()
        {
            var delivery = ABoundDelivery(TheRelease);
            delivery.SyncFromSource(delivery.Name, delivery.Date, delivery.Features, TheRoundRanAt.UtcDateTime.Date);

            return delivery;
        }

        private static Delivery ABoundDeliveryNothingHasResolvedYet()
        {
            var delivery = ABoundDelivery(TheRelease);
            delivery.SetForecastPublishing(true);

            return delivery;
        }

        private static Delivery ABoundDeliveryWhoseReleaseIsGone()
        {
            var delivery = ABroadcastingDelivery(TheRelease, AFeatureWithAForecast());
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            return delivery;
        }

        private static Delivery ABroadcastingDelivery(string sourceReference, params Feature[] features)
        {
            var delivery = ADeliveryChosenByHand(features);
            delivery.BindToSource(ReleaseSourceKey, sourceReference);
            delivery.SyncFromSource(delivery.Name, delivery.Date, features, TheRoundRanAt.UtcDateTime.Date);
            delivery.SetForecastPublishing(true);

            return delivery;
        }

        private static Feature AFeatureWithAForecast()
        {
            var team = new Team { Id = 1, Name = "Alpha" };
            var feature = new Feature([(team, 3, 6)]);
            feature.SetFeatureForecasts([
                new WhenForecast(new Dictionary<int, int> { { 5, 9000 }, { 15, 1000 } })
                {
                    Team = team,
                    TeamId = team.Id,
                    NumberOfItems = 3,
                    HasSufficientData = true,
                },
            ]);

            return feature;
        }

        private static Feature AFeatureNobodyCanForecast()
        {
            var team = new Team { Id = 2, Name = "Meridian" };
            var feature = new Feature([(team, 3, 6)]);
            feature.SetFeatureForecasts([new WhenForecast([]) { Team = team, TeamId = team.Id, HasSufficientData = true }]);

            return feature;
        }
    }
}
