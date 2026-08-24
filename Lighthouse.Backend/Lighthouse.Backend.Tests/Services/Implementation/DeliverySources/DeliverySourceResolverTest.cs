using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// Turning what a remote says a bound source is into what it means for one Portfolio: the reference
    /// ids that cross the port are narrowed to the Features that Portfolio actually tracks, so a
    /// Feature belonging to somebody else's board can never be shown as coming along with this date.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-01a")]
    public class DeliverySourceResolverTest
    {
        private const string SourceKey = "jira-release";
        private const string TheRelease = "10004";
        private const string TrackedItem = "LGH-1";
        private const string UntrackedItem = "OTHER-9";

        private static readonly DateTime TheReleaseDate = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);
        private static readonly string[] TheBoundReference = [TheRelease];
        private static readonly string[] BothItemsAreTagged = [TrackedItem, UntrackedItem];
        private static readonly string[] OnlyTheUntrackedItemIsTagged = [UntrackedItem];
        private static readonly string[] JustTheReleaseSource = [SourceKey];
        private static readonly string[] NoSourcesAtAll = [];

        private Mock<IWorkTrackingConnectorFactory> connectorFactoryMock;
        private Mock<IDeliverySourceProvider> providerMock;
        private DeliverySourceResolver subject;

        [SetUp]
        public void SetUp()
        {
            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            providerMock = new Mock<IDeliverySourceProvider>();

            subject = new DeliverySourceResolver(connectorFactoryMock.Object);
        }

        [Test]
        public async Task Only_the_tagged_work_this_Portfolio_tracks_comes_back_as_a_Feature()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenTheRemoteAnswers(new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot("Release 1.0", TheReleaseDate, BothItemsAreTagged)));

            var previews = await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            var preview = previews[TheRelease];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(preview.TrackedFeatures.Select(feature => feature.ReferenceId), Is.EqualTo(new[] { TrackedItem }),
                    "work tagged against the source but tracked by somebody else's Portfolio is not this Portfolio's to show.");
                Assert.That(preview.TaggedItemCount, Is.EqualTo(2),
                    "how much the remote tagged is kept, because it is the only thing that tells an untagged source apart from an untracked one.");
            }
        }

        [Test]
        public async Task A_source_whose_tagged_work_this_Portfolio_does_not_track_keeps_the_count_that_says_so()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenTheRemoteAnswers(new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot("Release 1.0", TheReleaseDate, OnlyTheUntrackedItemIsTagged)));

            var previews = await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(previews[TheRelease].TrackedFeatures, Is.Empty);
                Assert.That(previews[TheRelease].TaggedItemCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task Resolving_asks_the_connector_once_for_the_whole_batch_and_nothing_else()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenTheRemoteAnswers(new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot("Release 1.0", TheReleaseDate, BothItemsAreTagged)));

            await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            using (Assert.EnterMultipleScope())
            {
                providerMock.Verify(p => p.ResolveMany(portfolio.WorkTrackingSystemConnection, SourceKey, TheBoundReference), Times.Once);
                providerMock.Verify(p => p.GetOptions(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<string>()), Times.Never,
                    "spelling the membership query a second time here would put a second copy of the remote query language in the codebase.");
            }
        }

        [Test]
        public async Task A_source_the_remote_says_is_gone_brings_no_Features_with_it()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenTheRemoteAnswers(new DeliverySourceResolution.NotFound());

            var previews = await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(previews[TheRelease].Resolution, Is.TypeOf<DeliverySourceResolution.NotFound>());
                Assert.That(previews[TheRelease].TrackedFeatures, Is.Empty);
            }
        }

        [Test]
        public async Task A_connection_whose_connector_cannot_read_sources_at_all_is_unavailable_rather_than_gone()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem))
                .Returns(Mock.Of<IWorkTrackingConnector>());

            var previews = await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            Assert.That(previews[TheRelease].Resolution, Is.TypeOf<DeliverySourceResolution.Unavailable>(),
                "a connector that cannot answer is not the same as a remote that answered 'deleted', and only one of the two may retire a binding.");
        }

        [Test]
        public async Task A_reference_the_remote_left_out_of_its_answer_is_unavailable_rather_than_gone()
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenAConnectorThatReadsSources();
            providerMock
                .Setup(p => p.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), SourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(new Dictionary<string, DeliverySourceResolution>());

            var previews = await subject.ResolveForPortfolio(portfolio, SourceKey, TheBoundReference);

            Assert.That(previews[TheRelease].Resolution, Is.TypeOf<DeliverySourceResolution.Unavailable>(),
                "an answer that simply omits a reference says nothing about whether it exists, so it must not read as deleted.");
        }

        /// <summary>
        /// The Jira connector throws on a source key it does not offer, so a key that arrived in a
        /// hand-written request has to be settled here, against what the connection says it has, before
        /// anything is asked of the remote at all.
        /// </summary>
        [TestCaseSource(nameof(EveryAnswerAConnectionCanGiveAboutWhatItOffers))]
        public void What_a_connection_offers_is_settled_without_asking_the_remote_anything(
            bool theConnectorCanReadSources, string[] theSourcesTheConnectionNames, string theKeyAskedFor, bool expectedToBeOffered)
        {
            var portfolio = GivenAPortfolioTracking(TrackedItem);
            GivenAConnectionOffering(theConnectorCanReadSources, theSourcesTheConnectionNames);

            var offered = subject.OffersSource(portfolio, theKeyAskedFor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.EqualTo(expectedToBeOffered));
                providerMock.Verify(
                    p => p.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()),
                    Times.Never,
                    "a key nobody offers has to be answered without a remote call, or a mistyped key comes back as the remote being unwell.");
                providerMock.Verify(p => p.GetOptions(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<string>()), Times.Never);
            }
        }

        private static IEnumerable<TestCaseData> EveryAnswerAConnectionCanGiveAboutWhatItOffers()
        {
            yield return new TestCaseData(true, JustTheReleaseSource, SourceKey, true)
                .SetName("A connection that names the source offers it");
            yield return new TestCaseData(true, JustTheReleaseSource, "jira-relase", false)
                .SetName("A connection asked for a key one letter out offers nothing by that name");
            yield return new TestCaseData(true, NoSourcesAtAll, SourceKey, false)
                .SetName("A connection that could read sources but names none offers nothing");
            yield return new TestCaseData(false, NoSourcesAtAll, SourceKey, false)
                .SetName("A connection whose connector cannot read sources at all offers nothing");
        }

        private void GivenAConnectionOffering(bool theConnectorCanReadSources, string[] theSourcesTheConnectionNames)
        {
            if (!theConnectorCanReadSources)
            {
                connectorFactoryMock
                    .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                    .Returns(Mock.Of<IWorkTrackingConnector>());
                return;
            }

            GivenAConnectorThatReadsSources();
            providerMock
                .Setup(p => p.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(theSourcesTheConnectionNames.Select(key => new DeliverySourceDescriptor(key, key)).ToList());
        }

        private void GivenTheRemoteAnswers(DeliverySourceResolution resolution)
        {
            GivenAConnectorThatReadsSources();
            providerMock
                .Setup(p => p.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), SourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(new Dictionary<string, DeliverySourceResolution> { [TheRelease] = resolution });
        }

        private void GivenAConnectorThatReadsSources()
        {
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(providerMock.As<IWorkTrackingConnector>().Object);
        }

        private static Portfolio GivenAPortfolioTracking(params string[] referenceIds)
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Lighthouse",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };

            foreach (var referenceId in referenceIds)
            {
                portfolio.Features.Add(new Feature
                {
                    ReferenceId = referenceId,
                    Name = referenceId,
                    Type = "Feature",
                    State = "In Progress",
                });
            }

            return portfolio;
        }
    }
}
