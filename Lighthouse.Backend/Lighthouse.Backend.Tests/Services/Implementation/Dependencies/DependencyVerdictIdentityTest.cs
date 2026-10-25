using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    /// <summary>
    /// The dependency decision is given a lighter read of the Features than it used to be. Over a bounded set of
    /// generated graphs, seeded so a failure can be replayed, every verdict and every Feature's place comes out
    /// the same from the full read as from the lighter one.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-03")]
    [Category("contract-shape:unbounded-preservation")]
    public class DependencyVerdictIdentityTest
    {
        private const int Graphs = 12;

        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;

        [SetUp]
        public void SetUp()
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite(connection).Options;

            using var context = AContext();
            context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        private static IEnumerable<int> Seeds() => Enumerable.Range(1, Graphs);

        [TestCaseSource(nameof(Seeds))]
        public async Task Every_verdict_and_place_is_the_same_through_the_lighter_read(int seed)
        {
            await Seed(new Random(seed));

            var full = ReadFull(out var fullOrder);
            var light = ReadLight(out var lightOrder);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lightOrder, Is.EqualTo(fullOrder), "The Features did not come back in the same order.");
                Assert.That(light, Is.EqualTo(full), "A verdict differs between the full read and the lighter one.");
                Assert.That(full, Is.Not.Empty);
            }
        }

        private List<string> ReadFull(out List<int> order)
        {
            using var context = AContext();
            var features = Repository(context).GetAllByPredicate(_ => true).ToList();

            return Verdicts(features, out order);
        }

        private List<string> ReadLight(out List<int> order)
        {
            using var context = AContext();
            var features = Repository(context).WholeGraphWithoutHistograms();

            return Verdicts(features, out order);
        }

        private static List<string> Verdicts(IReadOnlyCollection<Feature> features, out List<int> order)
        {
            order = [.. features.Select(feature => feature.Id)];
            var positions = features.Select((feature, index) => (feature.Id, Place: index + 1)).ToDictionary(entry => entry.Id, entry => entry.Place);
            var licence = Mock.Of<ILicenseService>(l => l.CanUsePremiumFeatures());

            return [.. new DependencyDecision(new DependencyHonourPolicy(), licence).About(features, positions).Verdicts
                .Select(verdict => $"{verdict.DependentReferenceId}>{verdict.BlockerReferenceId}:{verdict.Reason}:{verdict.BlockerPositionedBelow}")
                .Order(StringComparer.Ordinal)];
        }

        private static FeatureRepository Repository(LighthouseAppContext context)
        {
            var ordering = new Mock<IFeatureOrdering>();
            ordering.Setup(o => o.Order(It.IsAny<IEnumerable<Feature>>()))
                .Returns((IEnumerable<Feature> features) => features.OrderBy(feature => feature.Order, StringComparer.Ordinal).ThenBy(feature => feature.Id));

            return new FeatureRepository(context, ordering.Object, NullLogger<FeatureRepository>.Instance);
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task Seed(Random random)
        {
            using var context = AContext();
            var teams = Enumerable.Range(0, 3).Select(n => new Team { Name = $"Team {n}", WorkTrackingSystemConnection = AConnection($"T{n}") }).ToList();
            var portfolios = Enumerable.Range(0, 3)
                .Select(n => new Portfolio { Name = $"Portfolio {n}", IgnoreDependencies = n == 2 && random.Next(2) == 0, WorkTrackingSystemConnection = AConnection($"P{n}") })
                .ToList();

            var count = random.Next(6, 13);
            var ids = Enumerable.Range(0, count).Select(n => $"F-{n}").ToList();
            var features = new List<Feature>();

            foreach (var id in ids)
            {
                var dependsOn = Enumerable.Range(0, random.Next(0, 4))
                    .Select(_ => random.Next(10) == 0 ? "UNKNOWN-1" : ids[random.Next(count)])
                    .Distinct()
                    .Select(reference => new FeatureDependencyReference(0, reference, DependencySource.TrackerLink))
                    .ToList();
                var feature = new Feature(dependsOn) { Name = id, ReferenceId = random.Next(12) == 0 ? ids[0] : id, Order = random.Next(1, 20).ToString("D2") };

                foreach (var team in teams.Where(_ => random.Next(2) == 0))
                {
                    feature.FeatureWork.Add(new FeatureWork(team, random.Next(0, 5), 5, feature));
                    feature.Forecasts.Add(new WhenForecast { Team = team, TotalTrials = random.Next(4) == 0 ? 0 : 200, CreationTime = DateTime.UtcNow });
                }

                feature.Portfolios.AddRange(portfolios.Where(_ => random.Next(3) == 0).DefaultIfEmpty(portfolios[0]));
                features.Add(feature);
            }

            context.Features.AddRange(features);
            await context.SaveChangesAsync();
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
    }
}
