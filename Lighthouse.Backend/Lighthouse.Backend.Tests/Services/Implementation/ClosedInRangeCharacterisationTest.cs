using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// What "closed in the selected range" means, for a Portfolio's Features and for a Team's Work
    /// Items. The Portfolio and the Team each carry their own copy of that rule. These tests pin the
    /// answers both owners give, so that putting the copies together into one rule cannot move a
    /// single item in or out of a range.
    ///
    /// The range is 1 to 30 September 2026. Every item sits at an edge the rule turns on: closed on the
    /// first or the last day, one day outside either end, done without a closed date, or in a state
    /// that is not done at all. Two items close in the evening in UTC, which is already the next
    /// morning in Auckland, so the same instant lands on a different day there. The instance's time
    /// zone decides which day an item closed on, so each answer is pinned in both zones.
    /// </summary>
    [TestFixture]
    [Category("story-6251-team-feature-size")]
    [Category("slice-01")]
    [Category("contract-shape:unbounded-preservation")]
    [Category("us-01")]
    public class ClosedInRangeCharacterisationTest
    {
        private const string Utc = "UTC";
        private const string Auckland = "Pacific/Auckland";

        private const string ClosedOnTheFirstDay = "closed on the first day";
        private const string ClosedOnTheLastDay = "closed on the last day";
        private const string ClosedTheDayBefore = "closed the day before the range";
        private const string ClosedTheDayAfter = "closed the day after the range";
        private const string DoneWithoutAClosedDate = "done without a closed date";
        private const string StillToDo = "still to do";
        private const string InProgress = "in progress";
        private const string InAnUnknownState = "in an unknown state, with a closed date in the range";
        private const string ClosedTheEveningBeforeInUtc = "closed the evening before the range, in UTC";
        private const string ClosedTheLastEveningInUtc = "closed the evening of the last day, in UTC";

        private static readonly DateTime FirstDay = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime LastDay = new(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);

        private static readonly (string Name, StateCategories State, DateTime? ClosedAt, int Size)[] Universe =
        [
            (ClosedOnTheFirstDay, StateCategories.Done, At(2026, 9, 1, 12), 1),
            (ClosedOnTheLastDay, StateCategories.Done, At(2026, 9, 30, 5), 6),
            (ClosedTheDayBefore, StateCategories.Done, At(2026, 8, 31, 5), 4),
            (ClosedTheDayAfter, StateCategories.Done, At(2026, 10, 1, 12), 8),
            (DoneWithoutAClosedDate, StateCategories.Done, null, 16),
            (StillToDo, StateCategories.ToDo, null, 32),
            (InProgress, StateCategories.Doing, null, 64),
            (InAnUnknownState, StateCategories.Unknown, At(2026, 9, 15, 12), 128),
            (ClosedTheEveningBeforeInUtc, StateCategories.Done, At(2026, 8, 31, 20), 3),
            (ClosedTheLastEveningInUtc, StateCategories.Done, At(2026, 9, 30, 20), 5),
        ];

        private static readonly string[] ClosedInTheRangeInUtc = [ClosedOnTheFirstDay, ClosedOnTheLastDay, ClosedTheLastEveningInUtc];

        private static readonly string[] ClosedInTheRangeInAuckland = [ClosedOnTheFirstDay, ClosedOnTheLastDay, ClosedTheEveningBeforeInUtc];

        private readonly Portfolio portfolio = new() { Id = 1, Name = "Ocean Explorer" };
        private readonly Team team = new() { Id = 1, Name = "Gravity" };

        private static IEnumerable<TestCaseData> EachZone()
        {
            yield return new TestCaseData(Utc, ClosedInTheRangeInUtc).SetArgDisplayNames(Utc);
            yield return new TestCaseData(Auckland, ClosedInTheRangeInAuckland).SetArgDisplayNames(Auckland);
        }

        [TestCaseSource(nameof(EachZone))]
        public void A_Portfolio_s_Cycle_Time_data_holds_the_Features_closed_in_the_range(string zone, string[] closedInTheRange)
        {
            var features = PortfolioMetrics(zone).GetCycleTimeDataForPortfolio(portfolio, FirstDay, LastDay);

            Assert.That(features.Select(f => f.Name), Is.EquivalentTo(closedInTheRange));
        }

        [TestCaseSource(nameof(EachZone))]
        public void A_Portfolio_s_Feature_Size_chart_holds_the_Features_closed_in_the_range_and_every_open_one(string zone, string[] closedInTheRange)
        {
            var features = PortfolioMetrics(zone).GetAllFeaturesForSizeChart(portfolio, FirstDay, LastDay);

            Assert.That(features.Select(f => f.Name), Is.EquivalentTo(closedInTheRange.Concat([StillToDo, InProgress])));
        }

        // The sizes closed in the range are 1, 5 and 6 in UTC, and 1, 3 and 6 in Auckland. Of three
        // sizes, the 50th is the smallest and the other three are the middle one, so the middle size
        // differs between the zones and an answer that ignored the instance's zone would be caught.
        [TestCase(Utc, 1, 5, 5, 5)]
        [TestCase(Auckland, 1, 3, 3, 3)]
        public void A_Portfolio_s_size_percentiles_come_from_the_Features_closed_in_the_range(string zone, int p50, int p70, int p85, int p95)
        {
            var percentiles = PortfolioMetrics(zone).GetSizePercentilesForPortfolio(portfolio, FirstDay, LastDay);

            Assert.That(
                percentiles.ToDictionary(percentile => percentile.Percentile, percentile => percentile.Value),
                Is.EquivalentTo(new Dictionary<int, int> { [50] = p50, [70] = p70, [85] = p85, [95] = p95 }));
        }

        [TestCaseSource(nameof(EachZone))]
        public void A_Team_s_closed_Work_Items_are_the_ones_closed_in_the_range(string zone, string[] closedInTheRange)
        {
            var workItems = TeamMetrics(zone).GetClosedItemsForTeam(team, FirstDay, LastDay);

            Assert.That(workItems.Select(i => i.Name), Is.EquivalentTo(closedInTheRange));
        }

        private PortfolioMetricsService PortfolioMetrics(string zone)
        {
            var features = Universe.Select(AFeatureOfThePortfolio).ToList();

            var featureRepository = new Mock<IRepository<Feature>>();
            featureRepository
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var appSettings = new Mock<IAppSettingService>();
            appSettings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            return new PortfolioMetricsService(
                Mock.Of<ILogger<PortfolioMetricsService>>(),
                featureRepository.Object,
                appSettings.Object,
                AFreshCache(),
                Mock.Of<IFeatureStateTransitionRepository>(),
                ClockIn(zone));
        }

        private TeamMetricsService TeamMetrics(string zone)
        {
            var workItems = Universe.Select(AWorkItemOfTheTeam).ToList();

            var workItemRepository = new Mock<IWorkItemRepository>();
            workItemRepository
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());

            var appSettings = new Mock<IAppSettingService>();
            appSettings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            return new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepository.Object,
                Mock.Of<IRepository<Feature>>(),
                appSettings.Object,
                AFreshCache(),
                Mock.Of<IBlackoutPeriodService>(),
                Mock.Of<IForecastFilterRuleService>(),
                Mock.Of<IWorkItemStateTransitionRepository>(),
                ClockIn(zone));
        }

        private Feature AFeatureOfThePortfolio((string Name, StateCategories State, DateTime? ClosedAt, int Size) item)
        {
            var feature = new Feature
            {
                Name = item.Name,
                ReferenceId = item.Name,
                StateCategory = item.State,
                ClosedDate = item.ClosedAt,
            };
            feature.Portfolios.Add(portfolio);
            feature.FeatureWork.Add(new FeatureWork(team, remainingWork: 0, totalWorkItems: item.Size, feature));
            return feature;
        }

        private WorkItem AWorkItemOfTheTeam((string Name, StateCategories State, DateTime? ClosedAt, int Size) item) => new()
        {
            Name = item.Name,
            ReferenceId = item.Name,
            TeamId = team.Id,
            Team = team,
            StateCategory = item.State,
            ClosedDate = item.ClosedAt,
        };

        private static IServiceProvider AFreshCache()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(provider => provider.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider
                .Setup(provider => provider.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());
            return serviceProvider.Object;
        }

        private static FakeLighthouseClock ClockIn(string zone) => new(
            new DateTimeOffset(2026, 10, 13, 9, 0, 0, TimeSpan.Zero),
            zone == Utc ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(zone));

        private static DateTime At(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
    }
}
