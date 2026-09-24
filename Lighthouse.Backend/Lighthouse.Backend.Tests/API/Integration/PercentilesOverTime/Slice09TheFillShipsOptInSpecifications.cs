using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice09 - the fill ships opt-in.
    ///
    /// The switch is thrown the way a System Admin throws it, through the behaviour-settings endpoint;
    /// charts are opened the way a flow coach opens them, through the two series endpoints. Nothing here
    /// calls a reconciler, a filler or a switch to make something happen or not happen.
    ///
    /// Two things are watched that the shared host does not watch, and both only here: the log, because
    /// dropping a waiting pass is said there and nowhere else; and the commands the store is sent,
    /// because what reading the switch costs a chart load is the one claim about it no chart can show.
    /// </summary>
    public partial class Slice09TheFillShipsOptInTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private const int ThirtyDays = 30;

        /// <summary>What a dropped waiting pass says, as the operator reads it in the log.</summary>
        private const string DroppedAWaitingPass = "dropped a waiting pass";

        /// <summary>The table the switch lives in, as it appears in a command sent to the store.</summary>
        private const string WhereTheSwitchIsStored = "OptionalFeatures";

        private static readonly int[] EveryCycleTimeLookBack = [30, 60, 90];

        private CapturedLogMessages capturedLogs = null!;

        private CommandsSentToTheStore commandsSent = null!;

        /// <summary>
        /// The log is taken through the same two places production sends it: everything to a capture a
        /// scenario can read, and Warning or worse to the instance's own Recent Problems, exactly as the
        /// shipped logger does. Only the first is new; the second is how a scenario can tell that what it
        /// saw at Information never reached the Task Manager.
        /// </summary>
        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            capturedLogs = new CapturedLogMessages();
            commandsSent = new CommandsSentToTheStore();

            services.ConfigureDbContext<LighthouseAppContext>((_, options) => options.AddInterceptors(commandsSent));

            var recentProblems = services.Single(descriptor => descriptor.ServiceType == typeof(IRecentProblems))
                .ImplementationInstance as ILogEventSink
                ?? throw new InvalidOperationException(
                    "Recent Problems is no longer registered as the log sink the shipped logger writes to, so this slice cannot " +
                    "tell whether anything it logs would reach the Task Manager.");

            services.RemoveAll<ILoggerFactory>();
            services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .WriteTo.Sink(capturedLogs)
                    .WriteTo.Sink(recentProblems, LogEventLevel.Warning)
                    .CreateLogger(),
                dispose: true));
        }

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenAPortfolioStillBeingRefreshed() => SeedPortfolioObservedUntil(TodayDay);

        /// <summary>
        /// One item finished on each day of the span, each taking two days: every day inside it has
        /// something the thirty days before it finished, so every day of a period inside it can be filled.
        /// </summary>
        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
            => SeedItemsFinishedOn(teamId, from, to, _ => 1);

        private void GivenThePortfolioFinishedOneDeliveryADayFrom(int portfolioId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedDeliveryFinishedOn(portfolioId, $"{portfolioId}-{day:yyyyMMdd}", day.AddDays(-4), day);
            }
        }

        private void GivenTheTeamHasOneItemStillInProgressSince(int teamId, DateOnly startedOn)
            => SeedItemStillInProgressSince(teamId, $"{teamId}-still-in-progress", startedOn);

        private Task GivenTheFillIsSwitchedOn() => TheFillIsSwitched(on: true);

        private Task GivenTheFillIsSwitchedOff() => TheFillIsSwitched(on: false);

        private void GivenTheInstanceStoresNoFillSwitch() => TheFillSwitchIsNotStoredAtAll();

        /// <summary>
        /// The chart was opened while the switch was on and the fill ran. Checked rather than assumed:
        /// the scenarios that build on it are about what happens to these days next, and they say nothing
        /// if the days were never there.
        /// </summary>
        private async Task GivenTheChartWasFilledInFrom(int teamId, DateOnly from, DateOnly to)
        {
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, from, to);
            await WhenTheChartHasFinishedFillingIn();

            Assert.That(CycleTimeDaysHeldFor(teamId, OwnerType.Team), Is.SupersetOf(EveryDayFrom(from, to)),
                $"The fill was on and the chart was opened, yet {from:yyyy-MM-dd}..{to:yyyy-MM-dd} did not fill in, so there is " +
                "nothing for switching off to keep or lose.");
        }

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

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheThroughputLimits(int teamId, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, ProcessBehaviorMetricType.Throughput, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensThePortfolioThroughputLimits(int portfolioId, DateOnly from, DateOnly to)
            => ReadPortfolioLimitTrend(portfolioId, ProcessBehaviorMetricType.Throughput, from, to);

        private Task WhenTheAdminSwitchesTheFillOn() => TheFillIsSwitched(on: true);

        private Task WhenTheAdminSwitchesTheFillOff() => TheFillIsSwitched(on: false);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        private Task WhenTheTeamsRefreshRuns(int teamId) => TheTeamsRefreshCompletes(teamId);

        private void WhenTheInstanceIsUpgraded() => TheInstanceIsUpgraded();

        private Task<HttpStatusCode> WhenSomeoneWhoAdministersOnlyTheirTeamTriesToSwitchTheFillOff(int teamId)
            => SomeoneAsksToSwitchTheFill(on: false, client => client.AsTeamAdmin(teamId));

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

        private void ThenNothingWasFilledAnywhere()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TotalPercentileDaysHeld(), Is.Zero,
                    "A percentile day was written while the fill was switched off, so something other than the switch decides " +
                    "whether past days are filled - and \"off\" does not mean off.");

                Assert.That(TotalLimitDaysHeld(), Is.Zero,
                    "A limits day was written while the fill was switched off, so something other than the switch decides " +
                    "whether past days are filled - and \"off\" does not mean off.");
            }
        }

        private void ThenTheCycleTimeTrendCoversEveryDayFrom(int ownerId, OwnerType ownerType, DateOnly from, DateOnly to)
        {
            var held = CycleTimeDaysHeldFor(ownerId, ownerType);
            var expected = EveryDayFrom(from, to);

            Assert.That(held, Is.EquivalentTo(expected),
                $"With the fill on, the {ownerType} {ownerId} cycle-time trend must cover every day from {from:yyyy-MM-dd} to " +
                $"{to:yyyy-MM-dd} and nothing else. Missing: {string.Join(", ", expected.Except(held))}. " +
                $"Unexpected: {string.Join(", ", held.Except(expected))}.");
        }

        private void ThenTheCycleTimeTrendAlsoCoversEveryDayFrom(int teamId, DateOnly from, DateOnly to)
        {
            var expected = EveryDayFrom(from, to);

            Assert.That(CycleTimeDaysHeldFor(teamId, OwnerType.Team), Is.SupersetOf(expected),
                $"Switched back on, reopening {from:yyyy-MM-dd}..{to:yyyy-MM-dd} must fill it. Missing: " +
                string.Join(", ", expected.Except(CycleTimeDaysHeldFor(teamId, OwnerType.Team))));
        }

        private void ThenTheOwnerHoldsNoOverTimeDays(int ownerId, OwnerType ownerType)
        {
            Assert.That(EverythingTheChartsHoldFor(ownerId, ownerType), Is.Empty,
                $"{ownerType} {ownerId} was still waiting when the fill was switched off, yet it was filled. A waiting pass has to " +
                "be dropped when its turn comes, or switching off takes effect only after every queued owner has been filled.");
        }

        private void ThenTheChartsHoldExactly(int ownerId, OwnerType ownerType, List<HeldDay> expected)
        {
            var held = EverythingTheChartsHoldFor(ownerId, ownerType);

            Assert.That(held, Is.EqualTo(expected),
                $"What {ownerType} {ownerId}'s charts hold changed. Gone: {string.Join(", ", expected.Except(held))}. " +
                $"New or rewritten: {string.Join(", ", held.Except(expected))}.");
        }

        private void ThenEveryDayTheFillWroteBeforeReadsAsItDid(int ownerId, OwnerType ownerType, List<HeldDay> before)
        {
            var held = EverythingTheChartsHoldFor(ownerId, ownerType);

            Assert.That(held, Is.SupersetOf(before),
                "Switching the fill back on rewrote or removed a day it had already filled: " +
                string.Join(", ", before.Except(held)));
        }

        private void ThenTheCycleTimeTrendAnswersWithEveryDayItHolds(SeriesResponse response, int teamId, DateOnly from, DateOnly to)
        {
            var held = CycleTimeDaysHeldFor(teamId, OwnerType.Team).Where(day => day >= from && day <= to).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(held, Is.Not.Empty,
                    "Nothing was filled before the switch went off, so there is nothing whose survival this could show.");

                Assert.That(DatesIn(response), Is.EquivalentTo(held),
                    "With the fill switched off, the cycle-time trend must still plot every day the fill wrote - a filled day is " +
                    "a day like any other, and switching off takes none of them back.");
            }
        }

        private void ThenTheThroughputLimitsAnswerWithEveryDayTheyHold(SeriesResponse response, int teamId, DateOnly from, DateOnly to)
        {
            var held = LimitDaysHeldFor(teamId, OwnerType.Team, ProcessBehaviorMetricType.Throughput)
                .Select(day => day.RecordedAt)
                .Where(day => day >= from && day <= to)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(held, Is.Not.Empty,
                    "No throughput limits were filled before the switch went off, so there is nothing whose survival this could show.");

                Assert.That(DatesIn(response), Is.EquivalentTo(held),
                    "With the fill switched off, the limits chart must still plot every day the fill wrote.");
            }
        }

        private void ThenTheWaitingPassWasDroppedAtInformationAndTheRunningOneWasNot(int waitingTeamId, int fillingTeamId)
        {
            var dropped = capturedLogs.At(LogEventLevel.Information)
                .Where(message => message.Contains(DroppedAWaitingPass, StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dropped.Where(message => NamesTheTeam(message, waitingTeamId)).ToList(), Has.Count.EqualTo(1),
                    $"Dropping team {waitingTeamId}'s waiting pass must be said once, at Information, so an operator reading the " +
                    $"log can see the switch took effect. Said: {string.Join(" | ", dropped)}");

                Assert.That(dropped.Where(message => NamesTheTeam(message, fillingTeamId)).ToList(), Is.Empty,
                    $"Team {fillingTeamId}'s pass was already running when the switch went off. It finishes, and has nothing to " +
                    "say about the switch.");
            }
        }

        /// <summary>
        /// Checked against a warning of this scenario's own first, because a Recent Problems that saw
        /// nothing at all would report nothing about the fill too, and the assertion would pass unread.
        /// </summary>
        private void ThenNothingAboutTheFillReachedRecentProblems()
        {
            var canary = $"Positive control for Recent Problems {Guid.NewGuid():N}";
            Factory.Services.GetRequiredService<ILogger<Slice09TheFillShipsOptInTest>>().LogWarning("{Canary}", canary);

            var problems = Factory.Services.GetRequiredService<IRecentProblems>().MostRecentFirst();

            var aboutTheFill = problems
                .Where(problem => problem.Source.Contains(nameof(OverTimeHistoryFiller), StringComparison.Ordinal)
                               || problem.Message.Contains("reconstruction", StringComparison.OrdinalIgnoreCase))
                .Select(problem => $"{problem.Level}: {problem.Message}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problems.Any(problem => problem.Message.Contains(canary, StringComparison.Ordinal)), Is.True,
                    "A warning logged here never reached Recent Problems, so nothing below can show what did or did not reach it.");

                Assert.That(aboutTheFill, Is.Empty,
                    "Switching the fill off put an entry in the Task Manager's Recent Problems. Dropping a waiting pass is the " +
                    "switch doing its job, not a problem, and has to stay below Warning.");
            }
        }

        private static void ThenTheWriteIsRefused(HttpStatusCode answer)
        {
            Assert.That(answer, Is.EqualTo(HttpStatusCode.Forbidden),
                "Only a System Admin may switch the fill. Anything but a refusal lets whoever administers one team decide, for the " +
                "whole instance, to write past days nobody can take back.");
        }

        private void ThenTheFillIsStoredSwitched(bool on)
        {
            Assert.That(TheStoredFillSwitch(), Is.EqualTo(on),
                $"The fill switch should be stored {(on ? "on" : "off")}.");
        }

        private void ThenTodayWasRecordedOverEveryCycleTimeLookBack(int teamId)
        {
            var lookBacksWithoutToday = EveryCycleTimeLookBack
                .Where(horizon => !PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, horizon)
                    .Any(day => day.RecordedAt == TodayDay))
                .ToList();

            Assert.That(lookBacksWithoutToday, Is.Empty,
                "The daily recording did not write today with the fill switched off. The switch governs filling in the past, " +
                $"never recording the present. Look-backs without today: {string.Join(", ", lookBacksWithoutToday)}");
        }

        private void ThenNothingButTodayWasWritten(int teamId)
        {
            var otherDays = EverythingTheChartsHoldFor(teamId, OwnerType.Team)
                .Select(held => held.Day)
                .Where(day => day != TodayDay)
                .Distinct()
                .ToList();

            Assert.That(otherDays, Is.Empty,
                "A refresh with the fill switched off wrote a day other than today: " + string.Join(", ", otherDays));
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
                "blank - with the fill off exactly as with it on. Written: " + string.Join(", ", writtenForToday));
        }

        private void ThenTheLogSaysTheFillWasSwitched(bool wasOn, bool isOn)
        {
            var whichWay = $"switched from {wasOn} to {isOn}";
            var lines = capturedLogs.At(LogEventLevel.Information)
                .Where(message => message.Contains(TheFillSwitchKey, StringComparison.Ordinal)
                               && message.Contains(whichWay, StringComparison.Ordinal))
                .ToList();

            Assert.That(lines, Has.Count.EqualTo(1),
                $"Switching the fill {(isOn ? "on" : "off")} from {(wasOn ? "on" : "off")} must leave one Information line naming the " +
                $"setting and saying '{whichWay}', so the log can answer when this instance started filling in its past.");
        }

        private void ThenNothingAboutTheSwitchWasLoggedAsAProblem()
        {
            var problems = capturedLogs.AtOrAbove(LogEventLevel.Warning)
                .Where(message => message.Contains(TheFillSwitchKey, StringComparison.Ordinal))
                .ToList();

            Assert.That(problems, Is.Empty,
                "Switching a behaviour setting is not a problem, so it must not reach the Task Manager: " + string.Join(" | ", problems));
        }

        private static void ThenTheSwitchWasNotLookedAt(List<string> commands)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(commands, Is.Not.Empty,
                    "Opening the chart sent the store nothing at all, so the count below is a count of nothing - the capture is not " +
                    "attached to the store the chart reads.");

                Assert.That(commands.Where(LooksAtTheSwitch).ToList(), Is.Empty,
                    "A chart missing nothing looked the switch up. That is the settled, everyday case, and the lookup belongs after " +
                    "the chart has found days to ask for.");
            }
        }

        private static void ThenTheSwitchWasLookedAtExactlyOnce(List<string> commands, List<string> comparedWith)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(commands.Where(LooksAtTheSwitch).ToList(), Has.Count.EqualTo(1),
                    "A chart missing days must look the switch up exactly once - never cached, because a cached answer is how " +
                    "switching needs a restart, and never more than once.");

                Assert.That(commands, Has.Count.EqualTo(comparedWith.Count + 1),
                    $"A chart missing days sent {commands.Count} commands against {comparedWith.Count} for one missing nothing. " +
                    "The switch is the one extra either way; anything more is a cost the read did not have before the switch.");
            }
        }

        // --- Reading what the charts hold ---

        private static bool LooksAtTheSwitch(string command) => command.Contains(WhereTheSwitchIsStored, StringComparison.Ordinal);

        private static bool NamesTheTeam(string message, int teamId)
            => message.Contains($"for {OwnerType.Team} {teamId} (", StringComparison.Ordinal);

        private List<DateOnly> CycleTimeDaysHeldFor(int ownerId, OwnerType ownerType)
            => [.. PercentileDaysHeldFor(ownerId, ownerType, MetricType.CycleTime, ThirtyDays).Select(day => day.RecordedAt)];

        private static List<DateOnly> EveryDayFrom(DateOnly from, DateOnly to)
        {
            var days = new List<DateOnly>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                days.Add(day);
            }

            return days;
        }

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
