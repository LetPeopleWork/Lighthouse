using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    /// <summary>
    /// How a named Cycle Time answer is put together and remembered: the order of its items, an answer per measure and
    /// per date range, an empty widget when nothing reached the end state, and an owner with more than one measure. The
    /// workflow runs New, Analysis, In Progress, Ready for Release, Done, on an instance where it is 5 December 2026.
    /// </summary>
    [TestFixture]
    public class NamedCycleTimeAnswerTest
    {
        private const string Analysis = "Analysis";
        private const string InProgress = "In Progress";
        private const string ReadyForRelease = "Ready for Release";
        private const string Done = "Done";
        private const int AnalysisToReadyForRelease = 7;
        private const int InProgressToReadyForRelease = 8;

        private static readonly DateTime FirstOfNovember = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime LastOfNovember = new(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FirstOfOctober = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime LastOfOctober = new(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);
        private static readonly FakeLighthouseClock FifthOfDecember = new(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero));
        private static readonly (int Percentile, int Value)[] TheOnlyClosedItemTookTwentyFiveDays = [(50, 25), (70, 25), (85, 25), (95, 25)];

        [Test]
        public void The_named_items_are_listed_by_the_day_they_reached_the_end_state_and_by_id_on_the_same_instant()
        {
            var team = new TeamWithReleaseWorkflow();
            team.AnItemWent(3, "C", (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));
            team.AnItemWent(1, "A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)));
            team.AnItemWent(2, "B", (Analysis, Nov(8)), (InProgress, Nov(10)), (ReadyForRelease, Nov(12)));

            var shown = team.NamedCycleTimeData(FirstOfNovember, LastOfNovember);

            Assert.That(shown, Is.EqualTo(new List<string> { "A", "B", "C" }),
                "A reached Ready for Release first; B and C reached it at the same moment, and B has the lower id.");
        }

        [Test]
        public void A_named_answer_is_remembered_apart_from_the_default_answer_and_from_other_dates()
        {
            var team = new TeamWithReleaseWorkflow();
            team.AnItemWent(1, "A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)), (Done, Nov(20)));
            team.AnItemWent(2, "B", (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));
            team.AnItemWent(3, "C", (Analysis, Oct(1)), (InProgress, Oct(10)), (ReadyForRelease, Oct(28)), (Done, Nov(3)));

            var closedInNovember = team.DefaultCycleTimeData(FirstOfNovember, LastOfNovember);
            var reachedInNovember = team.NamedCycleTimeData(FirstOfNovember, LastOfNovember);
            var reachedInOctober = team.NamedCycleTimeData(FirstOfOctober, LastOfOctober);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedInNovember, Is.EquivalentTo(new List<string> { "A", "C" }), "The default Cycle Time shows the items closed in November.");
                Assert.That(reachedInNovember, Is.EquivalentTo(new List<string> { "A", "B" }),
                    "The named Cycle Time must not be handed the default answer remembered for the same dates.");
                Assert.That(reachedInOctober, Is.EquivalentTo(new List<string> { "C" }),
                    "The named Cycle Time for October must not be handed the answer remembered for November.");
            }
        }

        [Test]
        public void The_named_percentiles_widget_shows_no_percentiles_when_nothing_reached_the_end_state_in_the_dates()
        {
            var team = new TeamWithReleaseWorkflow();
            team.AnItemWent(1, "A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)));

            var widget = team.PercentilesWidget(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Utc), AnalysisToReadyForRelease);

            Assert.That(widget.Percentiles, Is.Empty, "No item reached Ready for Release in December, so there is nothing to take a percentile of.");
        }

        [Test]
        public void The_outliers_of_one_of_several_named_measures_are_judged()
        {
            var team = new TeamWithReleaseWorkflow();
            team.AnItemWent(1, "A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)));
            team.AnItemWent(2, "B", (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));

            var verdict = team.Outliers(FirstOfNovember, LastOfNovember, InProgressToReadyForRelease);

            Assert.That(verdict, Is.InstanceOf<OutlierVerdict.Limited>(),
                "Two items reached Ready for Release in November, which is enough for a limit, even though the Team has a second named measure.");
        }

        [Test]
        public void A_Portfolio_remembers_the_named_outliers_apart_from_the_default_outliers()
        {
            var portfolio = new PortfolioWithReleaseWorkflow();
            portfolio.AFeatureWent(1, (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)));
            portfolio.AFeatureWent(2, (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));
            portfolio.AFeatureWent(3, (Analysis, Oct(1)), (InProgress, Oct(10)), (ReadyForRelease, Oct(28)), (Done, Nov(3)));

            var defaultVerdict = portfolio.Outliers(FirstOfNovember, LastOfNovember, null);
            var namedVerdict = portfolio.Outliers(FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultVerdict, Is.InstanceOf<OutlierVerdict.NoLimits>(), "Only one Feature was closed in November, too few for a limit.");
                Assert.That(namedVerdict, Is.InstanceOf<OutlierVerdict.Limited>(),
                    "Two Features reached Ready for Release in November; the named outliers must not be handed the default answer remembered for the same dates.");
            }
        }

        [Test]
        public void The_default_percentiles_widget_of_a_Team_counts_the_items_closed_in_the_dates()
        {
            var team = new TeamWithReleaseWorkflow();
            team.AnItemWent(1, "A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)), (Done, Nov(20)));
            team.AnItemWent(2, "B", (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));

            var widget = team.PercentilesWidget(FirstOfNovember, LastOfNovember, null);

            Assert.That(widget.Percentiles.Select(entry => (entry.Percentile, entry.Value)), Is.EqualTo(TheOnlyClosedItemTookTwentyFiveDays),
                "Only A was closed in November, after 25 days counting both 27 October and 20 November.");
        }

        [Test]
        public void The_default_percentiles_widget_of_a_Portfolio_counts_the_Features_closed_in_the_dates()
        {
            var portfolio = new PortfolioWithReleaseWorkflow();
            portfolio.AFeatureWent(1, (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)), (Done, Nov(20)));
            portfolio.AFeatureWent(2, (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));

            var widget = portfolio.PercentilesWidget(FirstOfNovember, LastOfNovember, null);

            Assert.That(widget.Percentiles.Select(entry => (entry.Percentile, entry.Value)), Is.EqualTo(TheOnlyClosedItemTookTwentyFiveDays),
                "Only the first Feature was closed in November, after 25 days counting both 27 October and 20 November.");
        }

        private static DateTime Oct(int day) => new(2026, 10, day, 9, 0, 0, DateTimeKind.Utc);

        private static DateTime Nov(int day) => new(2026, 11, day, 9, 0, 0, DateTimeKind.Utc);

        private static List<CycleTimeDefinition> TwoMeasures() =>
        [
            new CycleTimeDefinition { Id = AnalysisToReadyForRelease, Name = "Analysis to Ready for Release", StartState = Analysis, EndState = ReadyForRelease },
            new CycleTimeDefinition { Id = InProgressToReadyForRelease, Name = "In Progress to Ready for Release", StartState = InProgress, EndState = ReadyForRelease },
        ];

        private static TItem Moved<TItem>(TItem item, (string State, DateTime On)[] journey) where TItem : WorkItemBase
        {
            var (currentState, lastMove) = journey[^1];
            var isDone = currentState == Done;
            item.State = currentState;
            item.StateCategory = isDone ? StateCategories.Done : StateCategories.Doing;
            item.CreatedDate = journey[0].On.AddDays(-3);
            item.StartedDate = journey[0].On;
            item.ClosedDate = isDone ? lastMove : null;
            return item;
        }

        private static IEnumerable<(string From, string To, DateTime At)> Moves((string State, DateTime On)[] journey)
            => journey.Zip(journey.Skip(1), (from, to) => (from.State, to.State, to.On));

        private sealed class TeamWithReleaseWorkflow
        {
            private readonly List<WorkItem> items = [];
            private readonly List<WorkItemStateTransition> transitions = [];
            private readonly Team team = new()
            {
                Id = 1,
                Name = "Team Release Train",
                ToDoStates = ["New"],
                DoingStates = [Analysis, InProgress, ReadyForRelease],
                DoneStates = [Done],
                DoneItemsCutoffDays = 0,
                CycleTimeDefinitions = TwoMeasures(),
            };

            private readonly TeamMetricsService subject;

            public TeamWithReleaseWorkflow()
            {
                var workItemRepository = new Mock<IWorkItemRepository>();
                workItemRepository.AnswersTeamReadsFrom(items);
                var transitionRepository = new Mock<IWorkItemStateTransitionRepository>();
                transitionRepository.AnswersTransitionReadsFrom(transitions);
                var featureRepository = new Mock<IFeatureRepository>();
                featureRepository.Holds([]);
                var blackoutPeriods = new Mock<IBlackoutPeriodService>();
                blackoutPeriods.Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Array.Empty<BlackoutPeriod>());
                var appSettings = new Mock<IAppSettingService>();
                appSettings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });
                var throughputFilter = new Mock<IThroughputFilter>();
                var serviceProvider = ServiceProviderWithACache();

                subject = new TeamMetricsService(Mock.Of<ILogger<TeamMetricsService>>(), workItemRepository.Object, featureRepository.Object, appSettings.Object,
                    serviceProvider.Object, blackoutPeriods.Object, throughputFilter.Object, new ForecastingAnswersToFlowMetrics(serviceProvider.Object),
                    transitionRepository.Object, FifthOfDecember);
            }

            public void AnItemWent(int id, string reference, params (string State, DateTime On)[] journey)
            {
                items.Add(Moved(new WorkItem { Id = id, Team = team, TeamId = team.Id, ReferenceId = reference, Name = $"Story {reference}", Type = "User Story" }, journey));
                transitions.AddRange(Moves(journey).Select(move => new WorkItemStateTransition { WorkItemId = id, FromState = move.From, ToState = move.To, TransitionedAt = move.At }));
            }

            public IReadOnlyList<string> NamedCycleTimeData(DateTime startDate, DateTime endDate)
                => [.. subject.GetCycleTimeDataForTeam(team, startDate, endDate, AnalysisToReadyForRelease).Select(entry => entry.WorkItem.ReferenceId)];

            public IReadOnlyList<string> DefaultCycleTimeData(DateTime startDate, DateTime endDate)
                => [.. subject.GetCycleTimeDataForTeam(team, startDate, endDate).Select(entry => entry.WorkItem.ReferenceId)];

            public CycleTimePercentilesInfoDto PercentilesWidget(DateTime startDate, DateTime endDate, int? definitionId)
                => subject.GetCycleTimePercentilesInfoForTeam(team, startDate, endDate, definitionId);

            public OutlierVerdict Outliers(DateTime startDate, DateTime endDate, int? definitionId)
                => subject.GetCycleTimeOutliers(team, startDate, endDate, definitionId);
        }

        private sealed class PortfolioWithReleaseWorkflow
        {
            private readonly List<Feature> features = [];
            private readonly List<FeatureStateTransition> transitions = [];
            private readonly Portfolio portfolio = new()
            {
                Id = 1,
                Name = "Portfolio Release Train",
                ToDoStates = ["New"],
                DoingStates = [Analysis, InProgress, ReadyForRelease],
                DoneStates = [Done],
                DoneItemsCutoffDays = 0,
                CycleTimeDefinitions = TwoMeasures(),
            };

            private readonly PortfolioMetricsService subject;

            public PortfolioWithReleaseWorkflow()
            {
                var featureRepository = new Mock<IFeatureRepository>();
                featureRepository.AnswersPortfolioReadsFrom(() => features);
                var transitionRepository = new Mock<IFeatureStateTransitionRepository>();
                transitionRepository.AnswersTransitionReadsFrom(transitions);
                var appSettings = new Mock<IAppSettingService>();
                appSettings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });

                subject = new PortfolioMetricsService(Mock.Of<ILogger<PortfolioMetricsService>>(), featureRepository.Object, appSettings.Object,
                    ServiceProviderWithACache().Object, transitionRepository.Object, FifthOfDecember);
            }

            public void AFeatureWent(int id, params (string State, DateTime On)[] journey)
            {
                var feature = Moved(new Feature { Id = id, ReferenceId = $"F{id}", Name = $"Feature {id}", Type = "Epic" }, journey);
                feature.Portfolios.Add(portfolio);
                features.Add(feature);
                transitions.AddRange(Moves(journey).Select(move => new FeatureStateTransition { FeatureId = id, FromState = move.From, ToState = move.To, TransitionedAt = move.At }));
            }

            public CycleTimePercentilesInfoDto PercentilesWidget(DateTime startDate, DateTime endDate, int? definitionId)
                => subject.GetCycleTimePercentilesInfoForPortfolio(portfolio, startDate, endDate, definitionId);

            public OutlierVerdict Outliers(DateTime startDate, DateTime endDate, int? definitionId)
                => subject.GetCycleTimeOutliers(portfolio, startDate, endDate, definitionId);
        }

        private static Mock<IServiceProvider> ServiceProviderWithACache()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(provider => provider.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider.Setup(provider => provider.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());
            serviceProvider.Setup(provider => provider.GetService(typeof(IHowManyForecaster)))
                .Returns(() => new ForecastingAnswersToFlowMetrics(serviceProvider.Object));
            return serviceProvider;
        }
    }
}
