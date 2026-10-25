using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// Asking for Features by reference id on a real relational database: the ids go into the statement
    /// the database runs, so only the Features asked for are read, however many ids are asked.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-03")]
    public class FeaturesWithReferenceIdsRelationalTest
    {
        private static readonly string[] JustCheckout = ["CHECKOUT-1"];
        private static readonly string[] CheckoutAndSearch = ["CHECKOUT-1", "SEARCH-1"];
        private static readonly string[] CheckoutSearchAndAnIdNobodyHolds = ["CHECKOUT-1", "SEARCH-1", "NOT-HELD"];
        private static readonly string[] SearchThenCheckout = ["SEARCH-1", "CHECKOUT-1"];
        private static readonly string[] NoIds = [];
        private static readonly string[] JustPlatform = ["Platform"];
        private static readonly string[] JustAtlas = ["Atlas"];

        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;
        private StatementsSent statementsSent = null!;

        [SetUp]
        public async Task SetUp()
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            statementsSent = new StatementsSent();
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(connection, sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .AddInterceptors(statementsSent)
                .Options;

            using (var context = AContext())
            {
                context.Database.EnsureCreated();
            }

            await SeedThreeFeatures();
            statementsSent.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Only_the_Features_asked_for_are_read_in_forecast_order()
        {
            var read = TheFeaturesWith(CheckoutAndSearch);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(SearchThenCheckout));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void An_id_nothing_holds_has_no_Feature_in_the_answer()
        {
            var read = TheFeaturesWith(CheckoutSearchAndAnIdNobodyHolds);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(SearchThenCheckout));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void The_Features_come_back_whole_with_their_Portfolios_and_work_per_Team()
        {
            var checkout = TheFeaturesWith(JustCheckout).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checkout.Portfolios.Select(portfolio => portfolio.Name), Is.EqualTo(JustPlatform));
                Assert.That(checkout.FeatureWork.Select(work => work.Team.Name), Is.EqualTo(JustAtlas));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Asking_for_no_id_asks_the_database_nothing()
        {
            var read = TheFeaturesWith(NoIds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.Empty);
                Assert.That(statementsSent.Texts, Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void The_ids_are_filtered_in_the_database_and_the_statements_do_not_grow_with_the_ids_asked()
        {
            TheFeaturesWith(JustCheckout);
            var forOneId = statementsSent.Texts.ToList();
            statementsSent.Clear();

            TheFeaturesWith(CheckoutSearchAndAnIdNobodyHolds);
            var forThreeIds = statementsSent.Texts.ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forThreeIds, Has.Count.EqualTo(forOneId.Count), "A statement per id asked would grow with the ids.");
                Assert.That(forThreeIds, Has.All.Contain("IN ("), $"A statement read Features without filtering them by id: {string.Join(" | ", forThreeIds)}");
            }
        }

        private IReadOnlyList<Feature> TheFeaturesWith(IReadOnlyCollection<string> referenceIds)
        {
            var ordering = new Mock<IFeatureOrdering>();
            ordering.Setup(o => o.Order(It.IsAny<IEnumerable<Feature>>()))
                .Returns((IEnumerable<Feature> features) => features.OrderByDescending(feature => feature.ReferenceId, StringComparer.Ordinal));

            return new FeatureRepository(AContext(), ordering.Object, NullLogger<FeatureRepository>.Instance).FeaturesWithReferenceIds(referenceIds);
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task SeedThreeFeatures()
        {
            using var context = AContext();
            var team = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };

            var features = new[] { ("CHECKOUT-1", "Checkout"), ("SEARCH-1", "Search"), ("LEGACY-1", "Legacy") }
                .Select(held => new Feature(team, 2) { Name = held.Item2, ReferenceId = held.Item1, Order = held.Item1 })
                .ToList();
            features.ForEach(feature => feature.Portfolios.Add(platform));

            context.Features.AddRange(features);
            await context.SaveChangesAsync();
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };

        private sealed class StatementsSent : DbCommandInterceptor
        {
            private readonly List<string> texts = [];

            public IReadOnlyList<string> Texts => texts;

            public void Clear() => texts.Clear();

            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                texts.Add(command.CommandText);
                return result;
            }
        }
    }
}
