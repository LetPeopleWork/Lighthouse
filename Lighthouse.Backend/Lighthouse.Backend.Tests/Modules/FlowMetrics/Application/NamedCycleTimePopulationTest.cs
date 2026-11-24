using System.Reflection;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    /// <summary>
    /// A named Cycle Time "Analysis to Ready for Release" is answered from its own Work Items: every item that last
    /// moved into Ready for Release inside the chosen dates, whether it was closed afterwards or not, measured from
    /// Analysis to Ready for Release and placed on the day it reached Ready for Release. The default Cycle Time keeps
    /// answering with the items closed inside the dates. Each scenario asks the Team's or the Portfolio's metrics
    /// service, with a database of its own and no web host.
    /// </summary>
    [TestFixture]
    public class NamedCycleTimePopulationTest
    {
        private ReleaseWorkflow given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new ReleaseWorkflow();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_named_Cycle_Time_shows_every_item_that_reached_the_end_state_in_the_dates_closed_or_not(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheAnalysisToReadyForReleaseMeasure(ownerKind);
            given.TheItemsReleasedAroundNovember();

            var answer = given.TheNamedCycleTimeDataForNovember();

            ReleaseWorkflow.ThenTheNamedItemsAre(answer,
                new ExpectedNamedCycleTime("A", 10, new DateOnly(2026, 11, 5)),
                new ExpectedNamedCycleTime("B", 6, new DateOnly(2026, 11, 12)));
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_named_Cycle_Time_percentiles_count_only_the_items_that_reached_the_end_state_in_the_dates(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheAnalysisToReadyForReleaseMeasure(ownerKind);
            given.TheItemsReleasedAroundNovember();

            var percentiles = given.TheNamedCycleTimePercentilesForNovember();

            ReleaseWorkflow.ThenEveryPercentileIs(percentiles, 6, "The named Cycle Time percentiles were");
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_named_Cycle_Time_percentiles_widget_counts_only_the_items_that_reached_the_end_state_in_the_dates(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheAnalysisToReadyForReleaseMeasure(ownerKind);
            given.TheItemsReleasedAroundNovember();

            var percentiles = given.TheNamedCycleTimePercentilesWidgetForNovember();

            ReleaseWorkflow.ThenEveryPercentileIs(percentiles, 6, "The named Cycle Time percentiles widget showed");
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_named_Cycle_Time_outliers_are_judged_among_the_items_that_reached_the_end_state_in_the_dates(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheAnalysisToReadyForReleaseMeasure(ownerKind);
            given.EightItemsReleasedInThreeDaysEachInNovember();
            given.AnItemStillWaitingInReadyForReleaseAfterSixWeeks();
            given.AnItemThatTookTwoHundredDaysAndWasReleasedInOctober();

            var outliers = given.TheNamedCycleTimeOutliersForNovember();

            given.ThenTheOutliersAre(outliers, "B");
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_default_Cycle_Time_still_shows_the_items_closed_in_the_dates(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheAnalysisToReadyForReleaseMeasure(ownerKind);
            given.TheItemsReleasedAroundNovember();

            var closed = given.TheDefaultCycleTimeDataForNovember();

            ReleaseWorkflow.ThenTheDefaultItemsAre(closed, "A", "C");
        }

        public enum OwnerKind
        {
            Team,
            Portfolio,
        }

        private sealed record ExpectedNamedCycleTime(string Reference, int Days, DateOnly EndDay);

        private sealed record NamedCycleTimeSeen(string Reference, int? Days, DateOnly? EndDay);

        /// <summary>
        /// A Team or a Portfolio whose workflow runs Analysis, In Progress, Ready for Release, In Production, Done, with
        /// one named Cycle Time from Analysis to Ready for Release, on an instance where it is 5 December 2026.
        /// </summary>
        private sealed class ReleaseWorkflow : IDisposable
        {
            private const string Analysis = "Analysis";
            private const string InProgress = "In Progress";
            private const string ReadyForRelease = "Ready for Release";
            private const string InProduction = "In Production";
            private const string Done = "Done";
            private const int AnalysisToReadyForRelease = 7;

            private static readonly DateTime FirstOfNovember = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            private static readonly DateTime LastOfNovember = new(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);
            private static readonly int[] PercentilesShown = [50, 70, 85, 95];

            private readonly ServicePortHarness harness = new(Register);
            private readonly Dictionary<string, int> idsByReference = [];
            private OwnerKind kind;
            private int ownerId;

            public void Dispose() => harness.Dispose();

            public void AnOwnerWithTheAnalysisToReadyForReleaseMeasure(OwnerKind ownerKind)
            {
                kind = ownerKind;
                using var scope = harness.NewScope();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                };
                List<CycleTimeDefinition> measures =
                [
                    new CycleTimeDefinition { Id = AnalysisToReadyForRelease, Name = "Analysis to Ready for Release", StartState = Analysis, EndState = ReadyForRelease },
                ];

                if (kind == OwnerKind.Team)
                {
                    var team = new Team
                    {
                        Name = "Team Release Train",
                        WorkTrackingSystemConnection = connection,
                        DataRetrievalValue = "project = RELEASE",
                        WorkItemTypes = ["User Story"],
                        ToDoStates = ["New"],
                        DoingStates = [Analysis, InProgress, ReadyForRelease, InProduction],
                        DoneStates = [Done],
                        DoneItemsCutoffDays = 0,
                        CycleTimeDefinitions = measures,
                    };
                    var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                    teams.Add(team);
                    teams.Save().GetAwaiter().GetResult();
                    ownerId = team.Id;
                    return;
                }

                var portfolio = new Portfolio
                {
                    Name = "Portfolio Release Train",
                    WorkTrackingSystemConnection = connection,
                    DataRetrievalValue = "project = RELEASE",
                    ToDoStates = ["New"],
                    DoingStates = [Analysis, InProgress, ReadyForRelease, InProduction],
                    DoneStates = [Done],
                    DoneItemsCutoffDays = 0,
                    CycleTimeDefinitions = measures,
                };
                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                portfolios.Add(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            /// <summary>
            /// A reached Ready for Release on 5 Nov after 10 days and was Done on 20 Nov; B reached it on 12 Nov after
            /// 6 days and is still there; C reached it on 28 Oct after 28 days and was Done on 3 Nov; D is In Progress.
            /// </summary>
            public void TheItemsReleasedAroundNovember()
            {
                AnItemWent("A", (Analysis, Oct(27)), (InProgress, Oct(30)), (ReadyForRelease, Nov(5)), (InProduction, Nov(15)), (Done, Nov(20)));
                AnItemWent("B", (Analysis, Nov(7)), (InProgress, Nov(9)), (ReadyForRelease, Nov(12)));
                AnItemWent("C", (Analysis, Oct(1)), (InProgress, Oct(10)), (ReadyForRelease, Oct(28)), (Done, Nov(3)));
                AnItemWent("D", (Analysis, Nov(10)), (InProgress, Nov(15)));
            }

            public void EightItemsReleasedInThreeDaysEachInNovember()
            {
                for (var day = 2; day <= 9; day++)
                {
                    AnItemWent($"S{day}", (Analysis, Nov(day)), (InProgress, Nov(day + 1)), (ReadyForRelease, Nov(day + 2)), (Done, Nov(day + 3)));
                }
            }

            public void AnItemStillWaitingInReadyForReleaseAfterSixWeeks()
                => AnItemWent("B", (Analysis, Oct(1)), (InProgress, Oct(20)), (ReadyForRelease, Nov(12)));

            public void AnItemThatTookTwoHundredDaysAndWasReleasedInOctober()
                => AnItemWent("C", (Analysis, new DateTime(2026, 4, 12, 9, 0, 0, DateTimeKind.Utc)), (InProgress, Oct(10)), (ReadyForRelease, Oct(28)), (Done, Nov(3)));

            public IReadOnlyList<NamedCycleTimeSeen> TheNamedCycleTimeDataForNovember()
            {
                using var scope = harness.NewScope();
                if (kind == OwnerKind.Team)
                {
                    var teamAnswer = (IEnumerable<CycleTimeWorkItem>)AskForTheMeasure<ITeamMetricsService>(scope, nameof(ITeamMetricsService.GetCycleTimeDataForTeam), TheTeam(scope))!;
                    return [.. teamAnswer.Select(entry => Seen(entry.WorkItem.ReferenceId, entry.NamedCycleTimes))];
                }

                var portfolioAnswer = (IEnumerable<CycleTimeFeature>)AskForTheMeasure<IPortfolioMetricsService>(scope, nameof(IPortfolioMetricsService.GetNamedCycleTimeDataForPortfolio), ThePortfolio(scope))!;
                return [.. portfolioAnswer.Select(entry => Seen(entry.Feature.ReferenceId, entry.NamedCycleTimes))];
            }

            public IReadOnlyList<string> TheDefaultCycleTimeDataForNovember()
            {
                using var scope = harness.NewScope();
                if (kind == OwnerKind.Team)
                {
                    return [.. scope.ServiceProvider.GetRequiredService<ITeamMetricsService>()
                        .GetCycleTimeDataForTeam(TheTeam(scope), FirstOfNovember, LastOfNovember)
                        .Select(entry => entry.WorkItem.ReferenceId)];
                }

                return [.. scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>()
                    .GetNamedCycleTimeDataForPortfolio(ThePortfolio(scope), FirstOfNovember, LastOfNovember)
                    .Select(entry => entry.Feature.ReferenceId)];
            }

            public IReadOnlyList<(int Percentile, int Value)> TheNamedCycleTimePercentilesForNovember()
            {
                using var scope = harness.NewScope();
                var percentiles = kind == OwnerKind.Team
                    ? scope.ServiceProvider.GetRequiredService<ITeamMetricsService>()
                        .GetNamedCycleTimePercentilesForTeam(TheTeam(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease)
                    : scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>()
                        .GetNamedCycleTimePercentilesForPortfolio(ThePortfolio(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease);

                return [.. percentiles.Select(entry => (entry.Percentile, entry.Value))];
            }

            public IReadOnlyList<(int Percentile, int Value)> TheNamedCycleTimePercentilesWidgetForNovember()
            {
                using var scope = harness.NewScope();
                var widget = kind == OwnerKind.Team
                    ? scope.ServiceProvider.GetRequiredService<ITeamMetricsService>()
                        .GetCycleTimePercentilesInfoForTeam(TheTeam(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease)
                    : scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>()
                        .GetCycleTimePercentilesInfoForPortfolio(ThePortfolio(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease);

                return [.. widget.Percentiles.Select(entry => (entry.Percentile, entry.Value))];
            }

            public OutlierVerdict TheNamedCycleTimeOutliersForNovember()
            {
                using var scope = harness.NewScope();
                return kind == OwnerKind.Team
                    ? scope.ServiceProvider.GetRequiredService<ITeamMetricsService>()
                        .GetCycleTimeOutliers(TheTeam(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease)
                    : scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>()
                        .GetCycleTimeOutliers(ThePortfolio(scope), FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease);
            }

            public static void ThenTheNamedItemsAre(IReadOnlyList<NamedCycleTimeSeen> answer, params ExpectedNamedCycleTime[] expected)
            {
                var shown = answer.Select(entry => entry.Reference).ToList();
                Assert.That(shown, Is.EquivalentTo(expected.Select(entry => entry.Reference)),
                    $"The named Cycle Time showed [{string.Join(", ", shown)}]. It must show exactly the items whose last move into " +
                    "Ready for Release falls in the chosen dates, closed or not: an item still in Ready for Release belongs, an item " +
                    "closed in the dates that reached Ready for Release before them does not, nor does one that never reached it. " +
                    "Select the items by the day they reached the end state, not by the day they were closed.");

                using (Assert.EnterMultipleScope())
                {
                    foreach (var item in expected)
                    {
                        var seen = answer.First(entry => entry.Reference == item.Reference);
                        Assert.That(seen.Days, Is.EqualTo(item.Days),
                            $"{item.Reference} took {item.Days} days from entering Analysis to reaching Ready for Release, counting both days, " +
                            $"but the named value was {seen.Days?.ToString() ?? "missing"}. Measure each shown item with the named Cycle Time.");
                        Assert.That(seen.EndDay, Is.EqualTo(item.EndDay),
                            $"{item.Reference} reached Ready for Release on {item.EndDay:yyyy-MM-dd}, but its named value carried the end date " +
                            $"{seen.EndDay?.ToString("yyyy-MM-dd") ?? "of nothing"}. The chart places the item on that day, so each named value " +
                            "must carry the date of the item's last move into the end state.");
                    }
                }
            }

            public static void ThenEveryPercentileIs(IReadOnlyList<(int Percentile, int Value)> percentiles, int expectedDays, string whatWasAnswered)
            {
                Assert.That(percentiles, Is.EquivalentTo(PercentilesShown.Select(percentile => (percentile, expectedDays))),
                    $"{whatWasAnswered} [{string.Join(", ", percentiles.Select(entry => $"P{entry.Percentile}={entry.Value}"))}]. Over the " +
                    $"items that reached Ready for Release in the dates, 10 and 6 days, every percentile is {expectedDays}; a value of 10 " +
                    "means the item still waiting in Ready for Release was left out or the item that reached it before the dates was " +
                    "counted. Compute the percentiles over the same items the named Cycle Time shows.");
            }

            public void ThenTheOutliersAre(OutlierVerdict verdict, params string[] expectedReferences)
            {
                Assert.That(verdict, Is.InstanceOf<OutlierVerdict.Limited>(),
                    "Eight items released in November give the named Cycle Time a limit, so the outliers must be judged; with no limit " +
                    "nothing can be set apart. Judge the named Cycle Time over the items that reached the end state in the dates.");

                var references = idsByReference.ToDictionary(entry => entry.Value, entry => entry.Key);
                var outliers = ((OutlierVerdict.Limited)verdict).OutlierWorkItemIds
                    .Select(id => references.GetValueOrDefault(id, $"#{id}"))
                    .ToList();
                Assert.That(outliers, Is.EquivalentTo(expectedReferences),
                    $"The outliers were [{string.Join(", ", outliers)}]. Among eight items of 3 days and B, still waiting in Ready for " +
                    "Release after 43 days, B stands out; C took 200 days but reached Ready for Release in October, so it is neither " +
                    "shown nor part of the limit. Judge the outliers, and work out their limit, over the items that reached the end " +
                    "state in the dates.");
            }

            public static void ThenTheDefaultItemsAre(IReadOnlyList<string> shown, params string[] expected)
            {
                Assert.That(shown, Is.EquivalentTo(expected),
                    $"Without a named Cycle Time the answer showed [{string.Join(", ", shown)}]. The default Cycle Time shows the items " +
                    "closed in the dates, whatever the named Cycle Times do. Leave the answer without a definition as it was.");
            }

            private void AnItemWent(string reference, params (string State, DateTime On)[] journey)
            {
                using var scope = harness.NewScope();
                var (currentState, lastMove) = journey[^1];
                var isDone = currentState == Done;

                if (kind == OwnerKind.Team)
                {
                    var team = TheTeam(scope);
                    var item = new WorkItem
                    {
                        Team = team,
                        TeamId = team.Id,
                        ReferenceId = reference,
                        Name = $"Story {reference}",
                        Type = "User Story",
                        Url = $"https://example.test/items/{reference}",
                        Order = reference,
                    };
                    var items = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
                    items.Add(Moved(item, journey, currentState, lastMove, isDone));
                    items.Save().GetAwaiter().GetResult();
                    idsByReference[reference] = item.Id;

                    var transitions = scope.ServiceProvider.GetRequiredService<IWorkItemStateTransitionRepository>();
                    foreach (var (from, to, at) in Moves(journey))
                    {
                        transitions.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = from, ToState = to, TransitionedAt = at });
                    }

                    transitions.Save().GetAwaiter().GetResult();
                    return;
                }

                var feature = new Feature
                {
                    ReferenceId = reference,
                    Name = $"Feature {reference}",
                    Type = "Epic",
                    Url = $"https://example.test/features/{reference}",
                    Order = reference,
                };
                feature.Portfolios.Add(ThePortfolio(scope));
                var features = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
                features.Add(Moved(feature, journey, currentState, lastMove, isDone));
                features.Save().GetAwaiter().GetResult();
                idsByReference[reference] = feature.Id;

                var featureTransitions = scope.ServiceProvider.GetRequiredService<IFeatureStateTransitionRepository>();
                foreach (var (from, to, at) in Moves(journey))
                {
                    featureTransitions.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = from, ToState = to, TransitionedAt = at });
                }

                featureTransitions.Save().GetAwaiter().GetResult();
            }

            private static TItem Moved<TItem>(TItem item, (string State, DateTime On)[] journey, string currentState, DateTime lastMove, bool isDone)
                where TItem : WorkItemBase
            {
                item.State = currentState;
                item.StateCategory = isDone ? StateCategories.Done : StateCategories.Doing;
                item.CreatedDate = journey[0].On.AddDays(-3);
                item.StartedDate = journey[0].On;
                item.ClosedDate = isDone ? lastMove : null;
                return item;
            }

            private static IEnumerable<(string From, string To, DateTime At)> Moves((string State, DateTime On)[] journey)
                => journey.Zip(journey.Skip(1), (from, to) => (from.State, to.State, to.On));

            private Team TheTeam(IServiceScope scope)
                => scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)!;

            private Portfolio ThePortfolio(IServiceScope scope)
                => scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId)!;

            // Looked up by name rather than called directly, so that a port which does not yet take a definition is
            // reported as a failed expectation instead of stopping the whole test project from building.
            private static object? AskForTheMeasure<TPort>(IServiceScope scope, string operation, WorkTrackingSystemOptionsOwner owner)
                where TPort : notnull
            {
                var asksWithADefinition = typeof(TPort).GetMethods().SingleOrDefault(method =>
                    method.Name == operation
                    && method.GetParameters() is { Length: 4 } parameters
                    && parameters[3].Name == "definitionId");

                Assert.That(asksWithADefinition, Is.Not.Null,
                    $"{typeof(TPort).Name}.{operation} takes no definitionId, so the Cycle Time scatterplot cannot ask for a named " +
                    "Cycle Time's own items. Give it an optional definitionId after the dates; without one it answers as before.");

                return asksWithADefinition!.Invoke(
                    scope.ServiceProvider.GetRequiredService<TPort>(),
                    [owner, FirstOfNovember, LastOfNovember, AnalysisToReadyForRelease]);
            }

            private static NamedCycleTimeSeen Seen(string reference, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes)
            {
                var named = namedCycleTimes.FirstOrDefault(value => value.DefinitionId == AnalysisToReadyForRelease);
                return new NamedCycleTimeSeen(reference, named?.Days, named == null ? null : EndDayOf(named));
            }

            private static DateOnly? EndDayOf(NamedCycleTimeValue named)
                => typeof(NamedCycleTimeValue).GetProperty("EndDate", BindingFlags.Public | BindingFlags.Instance)?.GetValue(named) switch
                {
                    DateTime endDate => DateOnly.FromDateTime(endDate),
                    DateTimeOffset endMoment => DateOnly.FromDateTime(endMoment.UtcDateTime),
                    DateOnly endDay => endDay,
                    _ => null,
                };

            private static DateTime Oct(int day) => new(2026, 10, day, 9, 0, 0, DateTimeKind.Utc);

            private static DateTime Nov(int day) => new(2026, 11, day, 9, 0, 0, DateTimeKind.Utc);

            private static void Register(IServiceCollection services)
            {
                services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero)));
                services.AddSingleton(TimeProvider.System);
                services.AddScoped(_ => NoPremiumLicence());
                services.AddScoped(_ => RefreshSettingsOfTeamsAndPortfolios());
                services.AddSingleton<Cache<string, object>>();
                services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

                services.AddScoped<IRepository<Team>, TeamRepository>();
                services.AddScoped<IPortfolioRepository, PortfolioRepository>();
                services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
                services.AddScoped<IFeatureRepository, FeatureRepository>();
                services.AddScoped<IFeatureOrdering, FeatureOrdering>();
                services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
                services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
                services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
                services.AddScoped<IWorkItemRepository, WorkItemRepository>();
                services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
                services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
                services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
                services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
                services.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
                services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
                services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
                services.AddScoped<ForecastFilterRuleService>();
                services.AddScoped<IForecastFilterRuleService>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
                services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
                services.AddScoped<ForecastingAnswersToFlowMetrics>();
                services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
                services.AddScoped<IHowManyForecaster>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
                services.AddScoped<ITeamMetricsService, TeamMetricsService>();
                services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            }

            private static IAppSettingService RefreshSettingsOfTeamsAndPortfolios()
            {
                var refresh = new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 };
                var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
                settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(refresh);
                settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(refresh);
                return settings.Object;
            }

            private static ILicenseService NoPremiumLicence()
            {
                var licence = new Mock<ILicenseService>();
                licence.Setup(service => service.CanUsePremiumFeatures()).Returns(false);
                return licence.Object;
            }
        }
    }
}
