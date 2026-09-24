using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ProcessBehaviorSnapshotWriter : IProcessBehaviorSnapshotWriter
    {
        // The same 90 days the portfolio metrics page always opens on, so a recorded day's limits are
        // the ones a reader sees there.
        private const int PortfolioLookbackDays = 90;

        // TeamMetricsView falls back to a 30-day range when the team pins fixed throughput dates,
        // because a fixed past window is not an as-of-today window.
        private const int FixedDatesTeamLookbackDays = 30;

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IProcessBehaviorSnapshotRepository snapshotRepository;
        private readonly ILighthouseClock clock;

        public ProcessBehaviorSnapshotWriter(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IProcessBehaviorSnapshotRepository snapshotRepository,
            ILighthouseClock clock)
        {
            this.teamMetricsService = teamMetricsService;
            this.portfolioMetricsService = portfolioMetricsService;
            this.snapshotRepository = snapshotRepository;
            this.clock = clock;
        }

        // Five families for a team, six for a portfolio. The asymmetry is structural, not a filter:
        // Feature Size describes how big a portfolio's items are, and there is no team-side read
        // method to call for it. Dropping a line here is a silent capability loss, so the recorder
        // tests assert the exact family SET each scope produces.
        public IReadOnlyList<ProcessBehaviorFamilyReader> FamiliesFor(Team team)
        {
            var lookbackDays = LookbackDaysFor(team);

            return
            [
                new(ProcessBehaviorMetricType.Throughput, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.WorkItemAge, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.Wip, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetWipProcessBehaviourChart(team, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.CycleTime, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetCycleTimeProcessBehaviourChart(team, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.Arrivals, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetArrivalsProcessBehaviourChart(team, startDate, endDate, asOf)),
            ];
        }

        public IReadOnlyList<ProcessBehaviorFamilyReader> FamiliesFor(Portfolio portfolio)
        {
            return
            [
                new(ProcessBehaviorMetricType.Throughput, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetThroughputProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.WorkItemAge, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.Wip, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetWipProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.CycleTime, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetCycleTimeProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.Arrivals, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetArrivalsProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
                new(ProcessBehaviorMetricType.FeatureSize, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate, asOf)),
            ];
        }

        public void RecordToday(int ownerId, OwnerType ownerType, ProcessBehaviorFamilyReader family)
        {
            // The day comes from the clock rather than from reducing an instant here: an instant reduced
            // without the instance's time zone lands on the wrong day around midnight.
            var day = clock.Today;

            WriteUnlessTheChartHasNoProcessToShow(
                ownerId, ownerType, family, day, clock.TodayAsUtcMidnight,
                StoredRow(ownerId, ownerType, family.MetricType, day));
        }

        public void FillDayIfAbsent(int ownerId, OwnerType ownerType, ProcessBehaviorFamilyReader family, DateOnly day)
        {
            // A day that already carries limits was measured while that day was current, against the
            // data as it stood then. Recomputing it now would replace what was observed with a guess
            // assembled from what the tracker reports today, which is the worse of the two answers.
            if (StoredRow(ownerId, ownerType, family.MetricType, day) != null)
            {
                return;
            }

            WriteUnlessTheChartHasNoProcessToShow(
                ownerId, ownerType, family, day, InstanceCalendar.AsUtcMidnight(day), stored: null, asOf: day);
        }

        public async Task SaveFilledDay()
        {
            // Every iteration that absorbs a refusal drops at least one staged row, and there are
            // finitely many, so the loop runs at most once per row this day carries.
            while (true)
            {
                try
                {
                    await snapshotRepository.Save();
                    return;
                }
                catch (DbUpdateException refused)
                {
                    var somebodyElseGotThereFirst = EveryRefusedRowIsAlreadyStored(refused);

                    // Dropped either way. A refused row left staged would be retried on the next
                    // day's save and fail again, so one bad day would take the rest of the walk.
                    DropFromTheStagingArea(refused);

                    if (!somebodyElseGotThereFirst)
                    {
                        throw;
                    }
                }
            }
        }

        private bool EveryRefusedRowIsAlreadyStored(DbUpdateException refused)
        {
            return refused.Entries.Count > 0 && refused.Entries.All(entry =>
                entry.State == EntityState.Added &&
                entry.Entity is ProcessBehaviorSnapshot row &&
                StoredRow(row.OwnerId, row.OwnerType, row.MetricType, row.RecordedAt) != null);
        }

        private static void DropFromTheStagingArea(DbUpdateException refused)
        {
            foreach (var entry in refused.Entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        // Both the day written as it happens and a day worked out afterwards pass through here, so the
        // two cannot disagree about which days are worth a row.
        private void WriteUnlessTheChartHasNoProcessToShow(
            int ownerId,
            OwnerType ownerType,
            ProcessBehaviorFamilyReader family,
            DateOnly day,
            DateTime windowEnd,
            ProcessBehaviorSnapshot? stored,
            DateOnly? asOf = null)
        {
            var chart = family.ReadChart(windowEnd.AddDays(-family.LookbackDays), windowEnd, asOf);

            // Honesty gate: ProcessBehaviourChart.NotReady returns Average = UNPL = LNPL = 0.
            // Persisting that triple would draw three flat lines pinned at zero — a process the
            // owner never had. An absent row is the honest empty state.
            if (chart.Status != BaselineStatus.Ready)
            {
                return;
            }

            // A Ready chart can still carry a fully collapsed band: XmRCalculator.Calculate returns
            // Average = UNPL = LNPL = 0 for an empty or all-zero baseline, and every chart builder
            // still stamps Status = Ready for it. Persisting that triple has the same effect as
            // persisting NotReady, so it is refused here too. LowerNaturalProcessLimit is
            // deliberately NOT part of the predicate — the calculator clamps a negative lower limit
            // to zero for zero-bounded data, so a real, busy process routinely reports Lnpl == 0.
            if (chart.Average == 0 && chart.UpperNaturalProcessLimit == 0)
            {
                return;
            }

            if (stored == null)
            {
                snapshotRepository.Add(NewRow(ownerId, ownerType, family.MetricType, day, chart));
                return;
            }

            Apply(chart, stored);
        }

        private static int LookbackDaysFor(Team team)
        {
            if (team.UseFixedDatesForThroughput)
            {
                return FixedDatesTeamLookbackDays;
            }

            // Only the SPAN of the rolling window is wanted, never its position on the calendar: every
            // day's window is placed to end on that day. Team.GetThroughputSettings makes that span
            // ThroughputHistory - 1.
            return team.ThroughputHistory - 1;
        }

        private ProcessBehaviorSnapshot? StoredRow(
            int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly day)
        {
            return snapshotRepository.GetByPredicate(
                snapshot => snapshot.OwnerId == ownerId &&
                            snapshot.OwnerType == ownerType &&
                            snapshot.MetricType == metricType &&
                            snapshot.RecordedAt == day);
        }

        private static ProcessBehaviorSnapshot NewRow(
            int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly day,
            ProcessBehaviourChart chart)
        {
            var row = new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = metricType,
                RecordedAt = day,
            };

            Apply(chart, row);

            return row;
        }

        private static void Apply(ProcessBehaviourChart chart, ProcessBehaviorSnapshot row)
        {
            row.Unpl = chart.UpperNaturalProcessLimit;
            row.Average = chart.Average;
            row.Lnpl = chart.LowerNaturalProcessLimit;
        }
    }
}
