using System.Collections.Concurrent;
using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for what the history fill leaves alone and what it costs. Charts are opened the
    /// way a flow coach opens them, through the series endpoints, and a team's refresh runs the way the
    /// instance runs it. Nothing here calls a reconciler or a filler to make something happen.
    ///
    /// One thing is watched that the shared host does not watch: the commands the store is sent, because
    /// whether opening a chart looks a behaviour setting up is the one claim about it no chart can show.
    /// </summary>
    public partial class Slice09TheFillShipsOptInTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private const int ThirtyDays = 30;

        /// <summary>The table behaviour settings live in, as it appears in a command sent to the store.</summary>
        private const string WhereBehaviourSettingsAreStored = "OptionalFeatures";

        private static readonly int[] EveryCycleTimeLookBack = [30, 60, 90];

        private CommandsSentToTheStore commandsSent = null!;

        /// <summary>
        /// Every command the store is sent is recorded, so a scenario can read back what one chart load
        /// asked of it.
        /// </summary>
        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            commandsSent = new CommandsSentToTheStore();

            services.ConfigureDbContext<LighthouseAppContext>((_, options) => options.AddInterceptors(commandsSent));
        }

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        /// <summary>
        /// One item finished on each day of the span, each taking two days: every day inside it has
        /// something the thirty days before it finished.
        /// </summary>
        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
            => SeedItemsFinishedOn(teamId, from, to, _ => 1);

        private void GivenTheTeamHasOneItemStillInProgressSince(int teamId, DateOnly startedOn)
            => SeedItemStillInProgressSince(teamId, $"{teamId}-still-in-progress", startedOn);

        /// <summary>Every day of the span already carries a reading, so a chart over it is missing nothing.</summary>
        private void GivenEveryDayWasAlreadyRecordedFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedRecordedPercentileDay(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays, day, 2, 2, 3, 4);
            }
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task WhenTheTeamsRefreshRuns(int teamId) => TheTeamsRefreshCompletes(teamId);

        /// <summary>
        /// Every command the store is sent while a flow coach opens the cycle-time trend, and nothing
        /// else: the test host runs no background work, so nothing but the read itself talks to the store
        /// in between.
        /// </summary>
        private async Task<List<string>> WhatOpeningTheCycleTimeTrendAsksOfTheStore(int teamId, DateOnly from, DateOnly to)
        {
            var sentBefore = commandsSent.Count;

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, from, to);

            return commandsSent.SentSince(sentBefore);
        }

        // --- Then ---

        private void ThenTodayWasRecordedOverEveryCycleTimeLookBack(int teamId)
        {
            var lookBacksWithoutToday = EveryCycleTimeLookBack
                .Where(horizon => !PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, horizon)
                    .Any(day => day.RecordedAt == TodayDay))
                .ToList();

            Assert.That(lookBacksWithoutToday, Is.Empty,
                "The daily recording did not write today. Filling in the past never stands in for recording the present. " +
                $"Look-backs without today: {string.Join(", ", lookBacksWithoutToday)}");
        }

        private void ThenNothingButTodayWasWritten(int teamId)
        {
            var otherDays = EverythingTheChartsHoldFor(teamId, OwnerType.Team)
                .Select(held => held.Day)
                .Where(day => day != TodayDay)
                .Distinct()
                .ToList();

            Assert.That(otherDays, Is.Empty,
                "A refresh wrote a day other than today: " + string.Join(", ", otherDays));
        }

        private void ThenTheAgeReadingWasRecordedForToday(int teamId)
        {
            Assert.That(
                PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon)
                    .Select(day => day.RecordedAt),
                Does.Contain(TodayDay),
                "The refresh wrote no age reading for today, though an item is in progress - so the recording never ran for this " +
                "team, and a blank cycle-time day below would prove nothing about quiet days.");
        }

        private void ThenNoCycleTimeReadingWasRecordedForToday(int teamId)
        {
            var writtenForToday = EveryCycleTimeLookBack
                .SelectMany(horizon => PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, horizon))
                .Where(day => day.RecordedAt == TodayDay)
                .ToList();

            Assert.That(writtenForToday, Is.Empty,
                "Nothing finished in any cycle-time look-back ending today, so today has no cycle time to report and must be left " +
                "blank. Written: " + string.Join(", ", writtenForToday));
        }

        private static void ThenNoBehaviourSettingWasLookedUp(List<string> commands)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(commands, Is.Not.Empty,
                    "Opening the chart sent the store nothing at all, so the check below looks at nothing - the capture is not " +
                    "attached to the store the chart reads.");

                Assert.That(commands.Where(LooksUpABehaviourSetting).ToList(), Is.Empty,
                    "Opening a chart looked a behaviour setting up. Filling in a chart depends on no setting, so a lookup here is " +
                    "a gate coming back.");
            }
        }

        // --- Reading what the charts hold ---

        private static bool LooksUpABehaviourSetting(string command)
            => command.Contains(WhereBehaviourSettingsAreStored, StringComparison.Ordinal);

        /// <summary>
        /// Every reading this owner's over-time charts hold, on either chart, with its values - so that
        /// "nothing changed" means no day added, none taken away and none rewritten.
        /// </summary>
        private List<HeldDay> EverythingTheChartsHoldFor(int ownerId, OwnerType ownerType)
        {
            using var scope = Factory.Services.CreateScope();

            var percentiles = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>()
                .GetAll()
                .Where(held => held.OwnerId == ownerId && held.OwnerType == ownerType)
                .AsEnumerable()
                .Select(held => new HeldDay(
                    $"percentiles {held.MetricType} {held.Horizon}", held.RecordedAt, $"{held.P50}/{held.P70}/{held.P85}/{held.P95}"));

            var limits = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>()
                .GetAll()
                .Where(held => held.OwnerId == ownerId && held.OwnerType == ownerType)
                .AsEnumerable()
                .Select(held => new HeldDay($"limits {held.MetricType}", held.RecordedAt, $"{held.Unpl}/{held.Average}/{held.Lnpl}"));

            return [.. percentiles.Concat(limits)
                .OrderBy(held => held.Series, StringComparer.Ordinal)
                .ThenBy(held => held.Day)];
        }

        private readonly record struct HeldDay(string Series, DateOnly Day, string Reading)
        {
            public override string ToString() => $"{Series} {Day:yyyy-MM-dd} {Reading}";
        }

        /// <summary>
        /// Every command this host sends the store, in the order it was sent. A host of this slice's own,
        /// so a scenario running beside it on another fixture cannot add to the count.
        /// </summary>
        private sealed class CommandsSentToTheStore : DbCommandInterceptor
        {
            private readonly ConcurrentQueue<string> sent = new();

            public int Count => sent.Count;

            public List<string> SentSince(int alreadySent) => [.. sent.Skip(alreadySent)];

            public override InterceptionResult<DbDataReader> ReaderExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                sent.Enqueue(command.CommandText);
                return result;
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                sent.Enqueue(command.CommandText);
                return ValueTask.FromResult(result);
            }

            public override InterceptionResult<int> NonQueryExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                sent.Enqueue(command.CommandText);
                return result;
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                sent.Enqueue(command.CommandText);
                return ValueTask.FromResult(result);
            }

            public override InterceptionResult<object> ScalarExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
            {
                sent.Enqueue(command.CommandText);
                return result;
            }

            public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
            {
                sent.Enqueue(command.CommandText);
                return ValueTask.FromResult(result);
            }
        }
    }
}
