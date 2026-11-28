using System.Runtime.CompilerServices;
using System.Text.Json;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Domain;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Modules.WorkItems.Application.Dependencies;
using Lighthouse.Backend.Modules.WorkItems.Application.Policies;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies;
using Lighthouse.Backend.Platform.DemoData;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.UpdateQueue;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.OneStructure
{
    /// <summary>The installations whose numbers are recorded.</summary>
    public enum Installation
    {
        DemoData,
        UpgradeFixture,
    }

    /// <summary>
    /// Every number a unit shows, one line each: the unit named by kind, id and name, then what the number is.
    /// Two recordings are equal when every line holds the same value.
    /// </summary>
    public sealed record UnitNumbers(SortedDictionary<string, string> Lines)
    {
        public IReadOnlyList<string> DifferencesFrom(UnitNumbers expected)
        {
            var differences = new List<string>();

            foreach (var (number, value) in expected.Lines)
            {
                differences.Add(Lines.TryGetValue(number, out var now)
                    ? (now == value ? string.Empty : $"{number}: was {value}, now {now}")
                    : $"{number}: was {value}, now not shown");
            }

            differences.AddRange(Lines.Keys.Where(number => !expected.Lines.ContainsKey(number))
                .Select(number => $"{number}: was not shown, now {Lines[number]}"));

            return differences.Where(difference => difference.Length > 0).ToList();
        }
    }

    /// <summary>
    /// Loads an installation into a database of its own, refreshes every Team and then every Portfolio from its
    /// tracker, forecasts every Portfolio with its random draws pinned, and reads each unit's numbers back through
    /// the services the product shows them with: item counts by type, 90-day throughput, Work Item Age, and every
    /// Feature's When forecast at 50, 70, 85 and 95 percent. No web host runs.
    /// </summary>
    public static class UnitNumbersRecorder
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

        private const int PinnedRandomNumberSeed = 20_240_917;

        private const long PinnedDrawStartingNumber = 7_310_492_118;

        private const int RunsPerForecast = 200;

        private const int ThroughputWindowDays = 90;

        private static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

        private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

        public static async Task<UnitNumbers> Record(Installation installation)
        {
            using var harness = new ServicePortHarness(RegisterWhatTheNumbersAreReadWith);

            await Load(harness, installation);
            await RefreshEveryUnit(harness);
            await ForecastEveryPortfolio(harness);

            return ReadTheNumbers(harness);
        }

        public static UnitNumbers Expected(Installation installation)
        {
            var all = JsonSerializer.Deserialize<Dictionary<string, SortedDictionary<string, string>>>(File.ReadAllText(ExpectationFile()))!;

            return all.TryGetValue(installation.ToString(), out var lines)
                ? new UnitNumbers(lines)
                : new UnitNumbers([]);
        }

        /// <summary>Writes what was recorded beside the test run, so a reader can compare it with the expectation.</summary>
        public static string KeepForComparison(Installation installation, UnitNumbers recorded)
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"unit-numbers.{installation}.recorded.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new Dictionary<string, SortedDictionary<string, string>> { [installation.ToString()] = recorded.Lines }, Indented));
            return path;
        }

        private static string ExpectationFile([CallerFilePath] string here = "")
            => Path.Combine(Path.GetDirectoryName(here)!, "unit-numbers.expected.json");

        // --- Loading ---

        private static async Task Load(ServicePortHarness harness, Installation installation)
        {
            using var scope = harness.NewScope();

            if (installation == Installation.DemoData)
            {
                var demoData = scope.ServiceProvider.GetRequiredService<IDemoDataService>();
                await demoData.LoadScenarios([.. demoData.GetAllScenarios()]);
                return;
            }

            UpgradeFixture.WriteTheInstallation(scope.ServiceProvider, Today);
        }

        private static async Task RefreshEveryUnit(ServicePortHarness harness)
        {
            foreach (var teamId in Ids<Team>(harness))
            {
                using var scope = harness.NewScope();
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
                await scope.ServiceProvider.GetRequiredService<ITeamDataService>().UpdateTeamData(team);
            }

            foreach (var portfolioId in Ids<Portfolio>(harness))
            {
                using var scope = harness.NewScope();
                var portfolios = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
                await scope.ServiceProvider.GetRequiredService<IWorkItemService>().UpdateFeaturesForPortfolio(portfolios.GetById(portfolioId)!);
                await portfolios.Save();
            }
        }

        private static async Task ForecastEveryPortfolio(ServicePortHarness harness)
        {
            foreach (var portfolioId in Ids<Portfolio>(harness))
            {
                using var scope = harness.NewScope();
                var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>().GetById(portfolioId)!;
                await scope.ServiceProvider.GetRequiredService<IForecastService>().UpdateForecastsForPortfolio(portfolio, Today);
            }
        }

        // --- Reading ---

        private static UnitNumbers ReadTheNumbers(ServicePortHarness harness)
        {
            var lines = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var windowStart = Now.UtcDateTime.Date.AddDays(1 - ThroughputWindowDays);
            var windowEnd = Now.UtcDateTime.Date;

            using var scope = harness.NewScope();
            var services = scope.ServiceProvider;

            foreach (var team in services.GetRequiredService<IRepository<Team>>().GetAll().OrderBy(team => team.Id).ToList())
            {
                var unit = $"Team {team.Id} {team.Name}";
                var metrics = services.GetRequiredService<ITeamMetricsService>();
                var items = services.GetRequiredService<IWorkItemRepository>().GetAllByPredicate(item => item.TeamId == team.Id).ToList();

                CountsByType(lines, unit, items.Select(item => item.Type));
                lines[$"{unit} | throughput over 90 days"] = $"{metrics.GetThroughputForTeam(team, windowStart, windowEnd).Total}";
                lines[$"{unit} | total Work Item Age"] = $"{metrics.GetTotalWorkItemAge(team, windowEnd)}";
                lines[$"{unit} | Work Item Age percentiles"] = Percentiles(metrics.GetWorkItemAgePercentilesForTeam(team, windowEnd));
            }

            foreach (var portfolio in services.GetRequiredService<IPortfolioRepository>().GetAll().OrderBy(portfolio => portfolio.Id).ToList())
            {
                var unit = $"Portfolio {portfolio.Id} {portfolio.Name}";
                var metrics = services.GetRequiredService<IPortfolioMetricsService>();

                CountsByType(lines, unit, portfolio.Features.Select(feature => feature.Type));
                lines[$"{unit} | throughput over 90 days"] = $"{metrics.GetThroughputForPortfolio(portfolio, windowStart, windowEnd).Total}";
                lines[$"{unit} | total Work Item Age"] = $"{metrics.GetTotalWorkItemAge(portfolio, windowEnd)}";
                lines[$"{unit} | Work Item Age percentiles"] = Percentiles(metrics.GetWorkItemAgePercentilesForPortfolio(portfolio, windowEnd));

                foreach (var feature in portfolio.Features)
                {
                    lines[$"{unit} | Feature {feature.ReferenceId} When forecast 50/70/85/95"] = string.Join(" ", TheFourPercentiles.Select(feature.Forecast.GetProbability));
                }
            }

            return new UnitNumbers(lines);
        }

        private static void CountsByType(SortedDictionary<string, string> lines, string unit, IEnumerable<string> types)
        {
            foreach (var group in types.GroupBy(type => type, StringComparer.Ordinal))
            {
                lines[$"{unit} | {group.Key} count"] = $"{group.Count()}";
            }
        }

        private static string Percentiles(IEnumerable<Lighthouse.Backend.Models.Metrics.PercentileValue> percentiles)
            => string.Join(" ", percentiles.OrderBy(value => value.Percentile).Select(value => $"{value.Percentile}:{value.Value}"));

        private static List<int> Ids<TUnit>(ServicePortHarness harness) where TUnit : WorkTrackingSystemOptionsOwner
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            return [.. dbContext.Set<TUnit>().Select(unit => unit.Id).OrderBy(id => id)];
        }

        // --- The services as the product builds them, with the trackers and the random draws pinned ---

        private static void RegisterWhatTheNumbersAreReadWith(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Now));
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => RefreshSettingsAsSeeded());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<UpdateCancellationContext>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            services.AddScoped<IWorkItemBlockedTransitionRepository, WorkItemBlockedTransitionRepository>();
            services.AddScoped<IFeatureBlockedTransitionRepository, FeatureBlockedTransitionRepository>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            services.AddScoped<IRepository<TerminologyEntry>, TerminologyRepository>();
            services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            services.AddScoped<IDeliveryMetricSnapshotRepository, DeliveryMetricSnapshotRepository>();
            services.AddScoped<ISizingLogRepository, SizingLogRepository>();

            services.AddScoped<TerminologySeeder>();
            services.AddScoped<ITerminologyService, TerminologyService>();
            services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();
            services.AddScoped<IDemoDataFactory, DemoDataFactory>();
            services.AddScoped<IDemoDataService, DemoDataService>();

            services.AddSingleton(_ => UpgradeFixture.Trackers(Today));
            services.AddScoped<CsvWorkTrackingConnector>();
            services.AddScoped<IWorkTrackingConnectorFactory, TrackersAsTheFixtureHoldsThem>();

            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IBlockedItemService, BlockedItemService>();
            services.AddScoped<IDependencyReconciler, DependencyReconciler>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IDependencyRefreshReporter, DependencyRefreshReporter>();
            services.AddScoped<IWhatTheForecastWaitsFor, WhatTheForecastWaitsFor>();

            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<ForecastFilterRuleService>();
            services.AddScoped<IForecastFilterRuleService>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<ForecastingAnswersToFlowMetrics>();
            services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<IHowManyForecaster>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();

            services.AddScoped<IWorkItemService, WorkItemService>();
            services.AddScoped<ITeamDataService, TeamDataService>();

            services.AddScoped<IDomainEventHandler<WorkItemBlocked>, WorkItemBlockedTransitionCaptureHandler>();
            services.AddScoped<IDomainEventHandler<WorkItemUnblocked>, WorkItemBlockedTransitionCloseHandler>();
            services.AddScoped<IDomainEventHandler<FeatureBlocked>, FeatureBlockedTransitionCaptureHandler>();
            services.AddScoped<IDomainEventHandler<FeatureUnblocked>, FeatureBlockedTransitionCloseHandler>();

            services.AddSingleton<IRandomNumberService>(new SeededRandomNumberService(PinnedRandomNumberSeed));
            services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(PinnedDrawStartingNumber));
            services.AddSingleton(new ForecastSimulationLimits(RunsPerForecast, ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover)
            {
                CoresSeen = 4,
            });
            services.AddSingleton<ForecastSimulation>();
            services.AddScoped<IForecastService, ForecastService>();
        }

        /// <summary>The two refresh intervals a newly seeded database holds; the metrics a refresh renews read them.</summary>
        private static IAppSettingService RefreshSettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 15 });
            return settings.Object;
        }
    }
}
