using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Where publishing hangs. It is the forecast rather than the fetch, because the numbers being
    /// broadcast are the forecast's and the two no longer run in the same execution - and it is a
    /// handler rather than a line inside the forecast, so a remote that would not take today's numbers
    /// cannot cost the refresh that produced them.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-04")]
    public class DeliveryForecastPublishingHandlerTest
    {
        private const int ThePortfolio = 1;

        // Hoisted rather than inline: CA1861 fires on a constant array inside an assertion, and the
        // Sonar gate is zero new issues of any severity.
        private static readonly string[] PublishedThenSaved = ["published", "saved"];

        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IDeliveryRepository> deliveryRepositoryMock;
        private Mock<IDeliveryForecastPublishingService> publishingServiceMock;
        private DeliveryForecastPublishingHandler subject;

        [SetUp]
        public void SetUp()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            publishingServiceMock = new Mock<IDeliveryForecastPublishingService>();

            deliveryRepositoryMock
                .Setup(repository => repository.TrySaveRecomputedDeliveries())
                .ReturnsAsync(true);

            subject = new DeliveryForecastPublishingHandler(
                portfolioRepositoryMock.Object,
                deliveryRepositoryMock.Object,
                publishingServiceMock.Object,
                Mock.Of<ILogger<DeliveryForecastPublishingHandler>>());
        }

        [Test]
        public async Task A_finished_forecast_is_published_to_the_sources_the_Deliveries_follow()
        {
            var portfolio = GivenAPortfolio();
            var deliveries = GivenTheRecordableDeliveries();

            await subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None);

            publishingServiceMock.Verify(service => service.PublishForPortfolio(portfolio, deliveries), Times.Once);
        }

        /// <summary>
        /// The only thing publishing writes to a Delivery is that its Release turned out not to be there,
        /// and that finding is worth nothing unless it survives the round. Asserted as an order rather
        /// than as a count, because a save that runs before the publishing would satisfy a count while
        /// dropping every finding the publishing made.
        /// </summary>
        [Test]
        public async Task What_publishing_found_out_about_a_Release_is_written_down_afterwards()
        {
            GivenAPortfolio();
            GivenTheRecordableDeliveries();
            var whatHappenedInWhichOrder = new List<string>();
            publishingServiceMock
                .Setup(service => service.PublishForPortfolio(It.IsAny<Portfolio>(), It.IsAny<RecordableDeliveries>()))
                .Callback(() => whatHappenedInWhichOrder.Add("published"))
                .Returns(Task.CompletedTask);
            deliveryRepositoryMock
                .Setup(repository => repository.TrySaveRecomputedDeliveries())
                .Callback(() => whatHappenedInWhichOrder.Add("saved"))
                .ReturnsAsync(true);

            await subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None);

            Assert.That(whatHappenedInWhichOrder, Is.EqualTo(PublishedThenSaved));
        }

        /// <summary>
        /// Somebody editing a Delivery while the round was running wins, and the round has to survive
        /// that: the save is refused whole, and everything else the Portfolio produced this round is
        /// already written. The next round asks the Release again.
        /// </summary>
        [Test]
        public void A_save_refused_because_somebody_was_editing_does_not_take_the_round_down()
        {
            GivenAPortfolio();
            GivenTheRecordableDeliveries();
            deliveryRepositoryMock
                .Setup(repository => repository.TrySaveRecomputedDeliveries())
                .ReturnsAsync(false);

            Assert.DoesNotThrowAsync(() => subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None));
        }

        /// <summary>
        /// Retired Deliveries never reach the publisher, because the Deliveries a background pass may
        /// write to are read once, in the repository, rather than filtered again at every pass.
        /// </summary>
        [Test]
        public async Task Only_the_Deliveries_a_background_pass_may_write_to_are_offered_for_publishing()
        {
            GivenAPortfolio();
            GivenTheRecordableDeliveries();

            await subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None);

            deliveryRepositoryMock.Verify(repository => repository.GetRecordableByPortfolio(ThePortfolio), Times.Once);
        }

        [Test]
        public async Task A_Portfolio_that_is_gone_by_the_time_the_forecast_lands_publishes_nothing()
        {
            portfolioRepositoryMock.Setup(repository => repository.GetById(ThePortfolio)).Returns((Portfolio?)null);

            await subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None);

            publishingServiceMock.Verify(
                service => service.PublishForPortfolio(It.IsAny<Portfolio>(), It.IsAny<RecordableDeliveries>()),
                Times.Never);
        }

        /// <summary>
        /// Publishing is the last thing a refresh round does and the least important. A remote that would
        /// not take today's numbers must not take the round down with it, or every other number the
        /// refresh produced is lost over a Jira nobody could write to.
        /// </summary>
        [Test]
        public void A_publish_that_failed_outright_does_not_take_the_forecast_round_down()
        {
            GivenAPortfolio();
            GivenTheRecordableDeliveries();
            publishingServiceMock
                .Setup(service => service.PublishForPortfolio(It.IsAny<Portfolio>(), It.IsAny<RecordableDeliveries>()))
                .ThrowsAsync(new InvalidOperationException("the connection could not be resolved"));

            Assert.DoesNotThrowAsync(() => subject.HandleAsync(new PortfolioForecastsUpdated(ThePortfolio), CancellationToken.None));
        }

        [Test]
        public void An_event_that_is_nothing_at_all_is_refused()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => subject.HandleAsync(null!, CancellationToken.None));
        }

        private Portfolio GivenAPortfolio()
        {
            var portfolio = new Portfolio { Id = ThePortfolio, Name = "Zenith" };
            portfolioRepositoryMock.Setup(repository => repository.GetById(ThePortfolio)).Returns(portfolio);

            return portfolio;
        }

        private RecordableDeliveries GivenTheRecordableDeliveries()
        {
            var deliveries = new RecordableDeliveries([]);
            deliveryRepositoryMock.Setup(repository => repository.GetRecordableByPortfolio(ThePortfolio)).Returns(deliveries);

            return deliveries;
        }
    }
}
