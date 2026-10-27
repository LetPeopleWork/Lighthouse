using System.Reflection;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    /// <summary>
    /// Two rows of one demo scenario sharing a ReferenceId make the second silently overwrite or collide
    /// with the first once the data is refreshed, so the demo shows fewer items than its CSV lists.
    /// </summary>
    [TestFixture]
    public class DemoDataReferenceIdUniquenessTest
    {
        [TestCaseSource(nameof(TeamScenarios))]
        public async Task EveryWorkItemOfADemoTeamHasItsOwnReferenceId(string teamName)
        {
            var subject = CreateSubject();
            var team = subject.CreateDemoTeam(teamName);
            team.WorkTrackingSystemConnection = subject.CreateDemoWorkTrackingSystemConnection();

            var items = await CreateConnector().GetWorkItemsForTeam(team, CancellationToken.None);

            AssertUnique(teamName, items.Select(item => item.ReferenceId));
        }

        [TestCaseSource(nameof(PortfolioScenarios))]
        public async Task EveryFeatureOfADemoPortfolioHasItsOwnReferenceId(string portfolioName)
        {
            var subject = CreateSubject();
            var portfolio = subject.CreateDemoProject(portfolioName);
            portfolio.WorkTrackingSystemConnection = subject.CreateDemoWorkTrackingSystemConnection();

            var features = await CreateConnector().GetFeaturesForProject(portfolio, CancellationToken.None);

            AssertUnique(portfolioName, features.Select(feature => feature.ReferenceId));
        }

        private static IEnumerable<string> TeamScenarios() => NamesOf(typeof(DemoTeamNames));

        private static IEnumerable<string> PortfolioScenarios() => NamesOf(typeof(DemoProjectNames));

        private static IEnumerable<string> NamesOf(Type namesClass)
            => namesClass.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(property => (string)property.GetValue(null)!);

        private static void AssertUnique(string scenario, IEnumerable<string> referenceIds)
        {
            var all = referenceIds.ToList();
            var repeated = all.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(all, Is.Not.Empty, $"{scenario} yields no items, so the check proves nothing.");
                Assert.That(repeated, Is.Empty, $"{scenario} repeats ReferenceIds: {string.Join(", ", repeated)}");
            }
        }

        private static CsvWorkTrackingConnector CreateConnector()
            => new(Mock.Of<ILogger<CsvWorkTrackingConnector>>());

        private static DemoDataFactory CreateSubject()
            => new(new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>()), new FakeLighthouseClock(DateTimeOffset.UtcNow));
    }
}
