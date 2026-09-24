using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053 - a System Admin chooses whether this instance
    /// fills in past days. The switch ships off on a fresh instance and an upgraded one alike;
    /// switched on, the next chart open starts filling with no restart; switched off, what was filled
    /// stays and nothing new starts.
    ///
    /// Numbered Slice09 because the directory numbers fixtures, not story slices, and Slice05-Slice08
    /// are taken by the story's first four.
    ///
    /// Almost every scenario here says something was NOT written, and a claim of that shape passes for
    /// free unless the arrangement makes it depend on the switch. So each one carries its own proof that
    /// the very same arrangement does write once the switch is on - in the same scenario, over the same
    /// owners - and a scenario whose second half does not fill is a broken arrangement, not a pass.
    ///
    /// Step definitions live in Slice09TheFillShipsOptInSpecifications.cs (same partial class).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-09")]
    public partial class Slice09TheFillShipsOptInTest
    {
        private const string Pending =
            "Pending: the opt-in switch for filling in past days is not built yet (story 6053, slice 05) - un-ignore one at a time in DELIVER";

        /// <summary>
        /// The reconciler must decline to ask while the switch is off, not only the pass decline to run.
        /// A gate that existed only where a pass starts would let this scenario's opens queue their asks,
        /// and switching on afterwards would then fill them without anyone opening a chart again.
        /// </summary>
        // @driving_port @us-05 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task With_the_fill_switched_off_opening_a_chart_queues_nothing_and_switching_it_on_needs_no_restart()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), TodayDay);
            await GivenTheFillIsSwitchedOff();

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheDeliveryLeadOpensThePortfolioThroughputLimits(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheAdminSwitchesTheFillOn();
            await WhenTheChartHasFinishedFillingIn();

            ThenNothingWasFilledAnywhere();

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheDeliveryLeadOpensThePortfolioThroughputLimits(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheCycleTimeTrendCoversEveryDayFrom(teamId, OwnerType.Team, TodayDay.AddDays(-30), TodayDay);
                ThenTheCycleTimeTrendCoversEveryDayFrom(portfolioId, OwnerType.Portfolio, TodayDay.AddDays(-30), TodayDay);
            }
        }

        /// <summary>
        /// Which way it went, as the story asked the test to say: a pass already running when the switch
        /// goes off finishes its walk, and one still waiting is dropped before it starts. Dropping it is an
        /// ordinary thing to happen, so it is said at Information and never reaches the Task Manager's
        /// Recent Problems, which carries everything at Warning or worse.
        /// </summary>
        // @us-05 @concurrency @error @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_chart_already_filling_in_when_the_fill_is_switched_off_finishes_and_one_still_waiting_never_starts()
        {
            var fillingTeamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(fillingTeamId, TodayDay.AddDays(-120), TodayDay);
            var waitingTeamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(waitingTeamId, TodayDay.AddDays(-120), TodayDay);
            await GivenTheFillIsSwitchedOn();
            await WhenTheFlowCoachOpensTheCycleTimeTrend(fillingTeamId, TodayDay.AddDays(-30), TodayDay);

            using (AReconstructionPassHeldInFlight())
            {
                await WhenTheFlowCoachOpensTheCycleTimeTrend(waitingTeamId, TodayDay.AddDays(-30), TodayDay);
                await WhenTheAdminSwitchesTheFillOff();
            }

            using (Assert.EnterMultipleScope())
            {
                ThenTheCycleTimeTrendCoversEveryDayFrom(fillingTeamId, OwnerType.Team, TodayDay.AddDays(-30), TodayDay);
                ThenTheOwnerHoldsNoOverTimeDays(waitingTeamId, OwnerType.Team);
                ThenTheWaitingPassWasDroppedAtInformationAndTheRunningOneWasNot(waitingTeamId, fillingTeamId);
                ThenNothingAboutTheFillReachedRecentProblems();
            }

            var whatTheFinishedPassLeft = EverythingTheChartsHoldFor(fillingTeamId, OwnerType.Team);
            await WhenTheAdminSwitchesTheFillOn();
            await WhenTheFlowCoachOpensTheCycleTimeTrend(waitingTeamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheCycleTimeTrendCoversEveryDayFrom(waitingTeamId, OwnerType.Team, TodayDay.AddDays(-30), TodayDay);
                ThenTheChartsHoldExactly(fillingTeamId, OwnerType.Team, whatTheFinishedPassLeft);
            }
        }

        /// <summary>
        /// A filled day cannot be told from a recorded one, so there is nothing switching off could
        /// single out to take back - and it takes nothing back. Switching on again carries on from what is
        /// still missing and rewrites nothing it already wrote.
        /// </summary>
        // @driving_port @us-05 @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Switching_the_fill_off_keeps_every_day_it_filled_and_switching_it_back_on_fills_only_what_is_still_missing()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            await GivenTheFillIsSwitchedOn();
            await GivenTheChartWasFilledInFrom(teamId, TodayDay.AddDays(-30), TodayDay);
            var whatTheFillWrote = EverythingTheChartsHoldFor(teamId, OwnerType.Team);

            await WhenTheAdminSwitchesTheFillOff();
            var cycleTimeTrend = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            var throughputLimits = await WhenTheFlowCoachOpensTheThroughputLimits(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-120), TodayDay.AddDays(-91));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheCycleTimeTrendAnswersWithEveryDayItHolds(cycleTimeTrend, teamId, TodayDay.AddDays(-30), TodayDay);
                ThenTheThroughputLimitsAnswerWithEveryDayTheyHold(throughputLimits, teamId, TodayDay.AddDays(-30), TodayDay);
                ThenTheChartsHoldExactly(teamId, OwnerType.Team, whatTheFillWrote);
            }

            await WhenTheAdminSwitchesTheFillOn();
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-120), TodayDay.AddDays(-91));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheCycleTimeTrendAlsoCoversEveryDayFrom(teamId, TodayDay.AddDays(-120), TodayDay.AddDays(-91));
                ThenEveryDayTheFillWroteBeforeReadsAsItDid(teamId, OwnerType.Team, whatTheFillWrote);
            }
        }

        /// <summary>
        /// An instance that stores no switch at all - upgraded, seeders not yet run - must behave as one
        /// switched off, or there is a window after every upgrade in which it fills unasked. The upgrade
        /// then adds the switch off, and it takes an administrator to turn it on.
        /// </summary>
        // @us-05 @error @upgrade @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task An_instance_that_stores_no_fill_switch_fills_nothing_until_an_admin_switches_it_on_after_the_upgrade()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            GivenTheInstanceStoresNoFillSwitch();

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenNothingWasFilledAnywhere();

            WhenTheInstanceIsUpgraded();

            ThenTheFillIsStoredSwitched(on: false);

            await WhenTheAdminSwitchesTheFillOn();
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheCycleTimeTrendCoversEveryDayFrom(teamId, OwnerType.Team, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// A flow coach who administers her own team is not a System Admin. Her write is refused, the
        /// switch stays where it was, and her charts go on doing what they did - here, filling in, which
        /// is only possible because the refusal really did leave the switch on.
        /// </summary>
        // @us-05 @rbac @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Someone_who_is_not_a_system_admin_cannot_switch_the_fill_and_their_charts_carry_on_as_before()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            await GivenTheFillIsSwitchedOn();

            var answer = await WhenSomeoneWhoAdministersOnlyTheirTeamTriesToSwitchTheFillOff(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTheWriteIsRefused(answer);
                ThenTheFillIsStoredSwitched(on: true);
            }

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheCycleTimeTrendCoversEveryDayFrom(teamId, OwnerType.Team, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// The daily recording is not the fill and the switch does not govern it. Today is written on a
        /// refresh exactly as before, and a refresh writes nothing but today.
        /// </summary>
        // @us-05 @not-gated @regression @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_daily_recording_still_writes_today_with_the_fill_switched_off()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            await GivenTheFillIsSwitchedOff();

            await WhenTheTeamsRefreshRuns(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTodayWasRecordedOverEveryCycleTimeLookBack(teamId);
                ThenNothingButTodayWasWritten(teamId);
            }
        }

        /// <summary>
        /// The recorder's refusal to write four zeros for a quiet day shipped for everyone, and nobody
        /// should gate it by accident. The same refresh writes the team's age reading for today, which is
        /// what shows the recording genuinely ran for this team and chose to leave cycle time blank.
        /// </summary>
        // @us-05 @not-gated @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_quiet_day_is_still_left_blank_by_the_daily_recording_with_the_fill_switched_off()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay.AddDays(-150));
            GivenTheTeamHasOneItemStillInProgressSince(teamId, TodayDay.AddDays(-10));
            await GivenTheFillIsSwitchedOff();

            await WhenTheTeamsRefreshRuns(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTheAgeReadingWasRecordedForToday(teamId);
                ThenNoCycleTimeReadingWasRecordedForToday(teamId);
            }
        }

        /// <summary>
        /// Filled days are permanent and indistinguishable from recorded ones, so the first question an
        /// unrecognised past raises is when this instance switched the fill on. The instance answers it in
        /// its own log, for every behaviour setting alike, a write that changes nothing included - and at
        /// Information, so it never reads as a problem.
        /// </summary>
        // @us-05 @observability @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task Switching_the_fill_leaves_a_line_in_the_log_saying_which_way_it_went()
        {
            await GivenTheFillIsSwitchedOn();

            await WhenTheAdminSwitchesTheFillOff();
            await WhenTheAdminSwitchesTheFillOff();

            using (Assert.EnterMultipleScope())
            {
                ThenTheLogSaysTheFillWasSwitched(wasOn: true, isOn: false);
                ThenTheLogSaysTheFillWasSwitched(wasOn: false, isOn: false);
                ThenNothingAboutTheSwitchWasLoggedAsAProblem();
            }
        }

        /// <summary>
        /// Reading the switch costs a lookup, so it is read only once a chart has found days it is
        /// missing: a chart that is missing nothing - the settled, everyday case - pays nothing for it,
        /// and one that is missing days pays exactly one lookup whichever way the switch is set. Counted
        /// against the same chart for the same team, so the only thing that differs is whether anything is
        /// missing.
        /// </summary>
        // @us-05 @read-cost @real-io @contract-shape:pure-function
        [Test]
        public async Task Opening_a_chart_that_is_missing_nothing_pays_nothing_for_the_switch_and_one_missing_days_pays_one_lookup()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenEveryDayWasAlreadyRecordedFrom(teamId, TodayDay.AddDays(-5), TodayDay);
            await GivenTheFillIsSwitchedOff();
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-2), TodayDay);

            var missingNothing = await WhatOpeningTheCycleTimeTrendAsksOfTheStore(teamId, TodayDay.AddDays(-5), TodayDay);
            var missingDaysWhileOff = await WhatOpeningTheCycleTimeTrendAsksOfTheStore(teamId, TodayDay.AddDays(-10), TodayDay);
            await WhenTheAdminSwitchesTheFillOn();
            var missingDaysWhileOn = await WhatOpeningTheCycleTimeTrendAsksOfTheStore(teamId, TodayDay.AddDays(-9), TodayDay);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSwitchWasNotLookedAt(missingNothing);
                ThenTheSwitchWasLookedAtExactlyOnce(missingDaysWhileOff, comparedWith: missingNothing);
                ThenTheSwitchWasLookedAtExactlyOnce(missingDaysWhileOn, comparedWith: missingNothing);
            }
        }
    }
}
