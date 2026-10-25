using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureDependencyRenderingTest
    {
        private static readonly string[] OnlyCheckout = ["NW-1"];

        private static readonly int[] OnlyPlatform = [1];

        private static readonly Dictionary<int, int> NoPositions = [];

        private Mock<IFeatureListReader> readerMock = null!;
        private Mock<IRbacAdministrationService> rbacMock = null!;
        private Mock<IDependencyDecision> decisionMock = null!;

        [SetUp]
        public void SetUp()
        {
            readerMock = new Mock<IFeatureListReader>();
            readerMock.Setup(reader => reader.WholeGraphWithoutHistograms()).Returns([]);
            rbacMock = new Mock<IRbacAdministrationService>();
            decisionMock = new Mock<IDependencyDecision>();
            decisionMock.Setup(decision => decision.About(It.IsAny<IReadOnlyCollection<Feature>>(), It.IsAny<IReadOnlyDictionary<int, int>>()))
                .Returns(new HonouredDependencies([]));
        }

        [Test]
        public async Task A_list_that_is_already_the_whole_graph_is_used_as_it_stands_without_reading_the_graph_again()
        {
            var checkout = AFeature("NW-1", portfolioId: 1);
            rbacMock.Setup(rbac => rbac.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OnlyPlatform);

            var read = await ARendering().WhatTheseFeaturesWaitOn(new ClaimsPrincipal(), [checkout], NoPositions, everyFeatureIsLoaded: true, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Blockers.Keys, Is.EqualTo(OnlyCheckout));
                readerMock.Verify(reader => reader.WholeGraphWithoutHistograms(), Times.Never);
            }
        }

        [Test]
        public async Task Where_nothing_limits_what_the_reader_may_read_every_Portfolio_the_blockers_sit_in_is_readable()
        {
            var checkout = AFeature("NW-1", portfolioId: 1);
            rbacMock.Setup(rbac => rbac.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<int>)null!);

            var read = await ARendering().WhatTheseFeaturesWaitOn(new ClaimsPrincipal(), [checkout], NoPositions, everyFeatureIsLoaded: true, CancellationToken.None);

            Assert.That(read.ReadablePortfolioIds, Is.EqualTo(OnlyPlatform));
        }

        private FeatureDependencyRendering ARendering()
            => new(readerMock.Object, rbacMock.Object, decisionMock.Object);

        private static Feature AFeature(string referenceId, int portfolioId)
        {
            var feature = new Feature(new WorkItemBase { ReferenceId = referenceId, Name = referenceId, State = "In Progress", StateCategory = StateCategories.Doing })
            {
                Id = 1,
            };
            feature.Portfolios.Add(new Portfolio { Id = portfolioId, Name = "Northwind Platform" });

            return feature;
        }
    }
}
