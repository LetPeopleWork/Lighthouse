using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListServiceTest
    {
        private const int Platform = 1;
        private const int Strategy = 2;
        private const int Atlas = 10;
        private const int Cobalt = 11;

        private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        private static readonly int[] OnlyAtlas = [Atlas];

        private static readonly int[] OnlyPlatform = [Platform];

        private static readonly string[] ParentsOfTheRows = ["ST-1", "ST-2"];

        private static readonly string[] CheckoutThenSearch = ["Checkout redesign", "Search revamp"];

        private static readonly string[] OnlyCheckout = ["NW-1"];

        private static readonly string[] OnlyCheckoutName = ["Checkout redesign"];

        private static readonly int[] PositionsOneAndTwo = [1, 2];

        private static readonly bool[] BothMovable = [true, true];

        private static readonly string[] OnlyTheReadableParent = ["ST-1"];

        private readonly ClaimsPrincipal reader = new();

        private Mock<IFeatureListReader> featureListReaderMock = null!;
        private Mock<IRbacAdministrationService> rbacMock = null!;
        private Mock<ITeamMetricsService> teamMetricsMock = null!;
        private Team atlas = null!;
        private Team cobalt = null!;

        [SetUp]
        public void SetUp()
        {
            atlas = new Team { Id = Atlas, Name = "Atlas" };
            cobalt = new Team { Id = Cobalt, Name = "Cobalt" };

            featureListReaderMock = new Mock<IFeatureListReader>();
            featureListReaderMock.Setup(r => r.WholeGraphWithoutHistograms()).Returns(() => []);
            featureListReaderMock.Setup(r => r.ParentsOf(It.IsAny<IReadOnlyCollection<string>>())).Returns([]);
            rbacMock = new Mock<IRbacAdministrationService>();
            teamMetricsMock = new Mock<ITeamMetricsService>();

            rbacMock.Setup(rbac => rbac.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OnlyPlatform);
            rbacMock.Setup(rbac => rbac.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OnlyAtlas);
        }

        [Test]
        public async Task The_rows_come_in_the_order_the_reader_gives_them_carrying_their_position_and_move_verdict()
        {
            var checkout = AFeature(1, "NW-1", "Checkout redesign");
            var search = AFeature(2, "NW-2", "Search revamp");
            featureListReaderMock.Setup(r => r.FeaturesOfPortfolio(Platform)).Returns([checkout, search]);

            var list = await TheListOfThePlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Features.Select(row => row.Name), Is.EqualTo(CheckoutThenSearch));
                Assert.That(list.Features.Select(feature => list.Positions[feature.Id]), Is.EqualTo(PositionsOneAndTwo));
                Assert.That(list.Features.Select(feature => list.Verdicts[feature.Id].CanMove), Is.EqualTo(BothMovable));
            }
        }

        [Test]
        public async Task A_parent_is_named_only_when_the_reader_may_read_a_Portfolio_it_sits_in()
        {
            var checkout = AFeature(1, "NW-1", "Checkout redesign", parentReferenceId: "ST-1");
            var search = AFeature(2, "NW-2", "Search revamp", parentReferenceId: "ST-2");
            featureListReaderMock.Setup(r => r.FeaturesOfPortfolio(Platform)).Returns([checkout, search]);
            featureListReaderMock
                .Setup(r => r.ParentsOf(It.Is<IReadOnlyCollection<string>>(references => references.OrderBy(reference => reference).SequenceEqual(ParentsOfTheRows))))
                .Returns([
                    new FeatureParent("ST-1", "Platform 2027", "https://tracker.example/ST-1", [Platform]),
                    new FeatureParent("ST-2", "Hidden strategy", "https://tracker.example/ST-2", [Strategy]),
                ]);

            var list = await TheListOfThePlatform();

            Assert.That(list.Parents.Select(parent => parent.ReferenceId), Is.EqualTo(OnlyTheReadableParent));
        }

        [Test]
        public async Task A_Team_the_reader_may_not_read_is_left_out_of_the_work_at_hand_rather_than_refused()
        {
            var checkout = AFeature(1, "NW-1", "Checkout redesign");
            checkout.AddOrUpdateWorkForTeam(atlas, 3, 5);
            checkout.AddOrUpdateWorkForTeam(cobalt, 3, 5);
            featureListReaderMock.Setup(r => r.FeaturesOfPortfolio(Platform)).Returns([checkout]);
            teamMetricsMock.Setup(metrics => metrics.GetInProgressFeatureReferences(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(["NW-1", null, string.Empty]);

            var list = await TheListOfThePlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.ActiveWork.Select(work => work.TeamId), Is.EqualTo(OnlyAtlas));
                Assert.That(list.ActiveWork.Single().FeatureReferenceIds, Is.EqualTo(OnlyCheckout));
            }
        }

        [Test]
        public void The_service_stays_within_seven_constructor_parameters()
        {
            var parameters = typeof(FeatureListService).GetConstructors().Single().GetParameters();

            Assert.That(parameters, Has.Length.LessThanOrEqualTo(7));
        }

        [Test]
        public void The_service_and_its_dependency_rendering_depend_on_the_reader_and_never_on_the_repository()
        {
            var parameterTypes = new[] { typeof(FeatureListService), typeof(FeatureDependencyRendering) }
                .SelectMany(type => type.GetConstructors().Single().GetParameters())
                .Select(parameter => parameter.ParameterType)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parameterTypes, Does.Contain(typeof(IFeatureListReader)));
                Assert.That(parameterTypes, Does.Not.Contain(typeof(IFeatureRepository)));
            }
        }

        [Test]
        public async Task A_Team_s_list_holds_the_Features_the_reader_gives_for_the_Team_read_and_filtered_as_a_Portfolio_s_are()
        {
            var checkout = AFeature(1, "NW-1", "Checkout redesign");
            var hidden = AFeature(2, "NW-2", "Hidden", portfolioId: Strategy);
            featureListReaderMock.Setup(r => r.FeaturesOfTeam(Atlas)).Returns([checkout, hidden]);

            var list = await AService().ForTeamAsync(reader, Atlas, CancellationToken.None);

            Assert.That(list.Features.Select(row => row.Name), Is.EqualTo(OnlyCheckoutName));
        }

        [Test]
        public void A_Team_that_does_not_exist_has_no_list_to_open_while_one_that_does_has()
        {
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.Exists(Atlas)).Returns(true);

            var service = AService(teams.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.TeamExists(Atlas), Is.True);
                Assert.That(service.TeamExists(9999), Is.False);
            }
        }

        private async Task<FeatureListRead> TheListOfThePlatform()
            => await AService().ForPortfolioAsync(reader, Platform, CancellationToken.None);

        private FeatureListService AService(IRepository<Team>? teams = null)
        {
            var clock = new FakeLighthouseClock(Now);
            var positions = new Mock<IFeaturePositionMap>();
            positions.Setup(map => map.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<int, int> { [1] = 1, [2] = 2 });

            var moves = new Mock<IFeatureMoveAuthorization>();
            moves.Setup(authorization => authorization.GetVerdictsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IReadOnlyCollection<Feature>>(), It.IsAny<ISet<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IReadOnlyCollection<Feature> requested, ISet<int> _, CancellationToken _) =>
                    requested.ToDictionary(feature => feature.Id, _ => FeatureMoveVerdict.Allowed));

            var decision = new DependencyDecision(new DependencyHonourPolicy(), Mock.Of<ILicenseService>(licence => licence.CanUsePremiumFeatures()));

            return new FeatureListService(
                featureListReaderMock.Object,
                rbacMock.Object,
                positions.Object,
                moves.Object,
                new FeatureDependencyRendering(featureListReaderMock.Object, rbacMock.Object, decision),
                new FeatureListActiveWork(teamMetricsMock.Object, rbacMock.Object, clock),
                new FeatureListOwners(Mock.Of<IRepository<Portfolio>>(), teams ?? Mock.Of<IRepository<Team>>()));
        }

        private static Feature AFeature(int id, string referenceId, string name, string parentReferenceId = "", int portfolioId = Platform)
        {
            var feature = new Feature(new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = name,
                ParentReferenceId = parentReferenceId,
                State = "In Progress",
                StateCategory = StateCategories.Doing,
            })
            {
                Id = id,
            };

            feature.Portfolios.Add(new Portfolio { Id = portfolioId, Name = "Northwind Platform" });

            return feature;
        }
    }
}
