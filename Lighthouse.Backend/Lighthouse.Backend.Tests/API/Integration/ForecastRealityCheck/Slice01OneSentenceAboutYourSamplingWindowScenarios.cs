namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// DISTILL acceptance scenarios for slice 01 of the Forecast Reality Check - one press on a Team's
    /// Forecast tab answers whether the sampling window behind every forecast it publishes would have got
    /// the last few periods right, and which of the four confidence levels held up.
    ///
    /// Driving port: the check itself, over HTTP, on both the latest and the versioned route. The
    /// sentence the forecaster reads is composed in the browser from what these scenarios pin, so the
    /// words are asserted in the Team Forecast view's own tests; these pin the facts the words rest on.
    ///
    /// Step definitions live in Slice01OneSentenceAboutYourSamplingWindowSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4172-forecast-backtest-sweep")]
    [Category("slice-01")]
    public partial class Slice01OneSentenceAboutYourSamplingWindowTest
    {
        // --- The check answers ---

        /// <summary>
        /// The one scenario that runs the shipped forecast engine end to end. Everything else scripts the
        /// forecast so it can say which levels held; this one proves the real engine's answer is read the
        /// same way.
        /// </summary>
        // @walking_skeleton @driving_port @driving_adapter @real-io @us-01 @kpi-OUT-4172-never-overclaims @contract-shape:unbounded-preservation
        [Test]
        public async Task Maria_runs_the_reality_check_on_Ocean_Explorer_and_gets_an_answer_without_giving_a_date()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenTheForecastIsWorkedOutAsShipped();
            var storedBefore = WhatIsStoredFor(oceanExplorer);

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer));

            using (Assert.EnterMultipleScope())
            {
                ThenEveryCheckEndsToday(answer);
                ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
                ThenEachLevelHeldExactlyWhenTheTeamDeliveredAtLeastItsForecast(answer);
                ThenTheDenominatorCountsWhatWasEvaluated(answer);
                ThenNothingAboutTheTeamChanged(oceanExplorer, storedBefore);
            }
        }

        // @driving_port @driving_adapter @real-io @us-01 @contract-shape:pure-function
        [Test]
        public async Task The_same_check_answers_on_the_versioned_route_as_well()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, route: VersionedRoute));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        // @driving_port @us-01 @boundary @real-io @contract-shape:pure-function
        [TestCase(NoOptions)]
        [TestCase("{\"applyFilterOverride\":null}")]
        [TestCase("{\"applyFilterOverride\":false}")]
        [TestCase("{\"applyFilterOverride\":true}")]
        public async Task The_check_answers_whether_or_not_the_forecast_filter_choice_is_given(string options)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, options));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        // @driving_port @us-01 @error @real-io @contract-shape:unbounded-preservation
        [TestCase("{\"applyFilterOverride\":\"yes\"}")]
        [TestCase("{\"applyFilterOverride\":1}")]
        [TestCase("{\"applyFilterOverride\":")]
        public async Task A_request_whose_filter_choice_is_not_yes_no_or_unset_is_refused_and_nothing_is_checked(string options)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            await GivenTheCheckAnswersForATeamThatExists(oceanExplorer);

            using var refused = await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, options);

            await ThenTheRequestIsRefusedWithoutASingleCheck(refused);
        }

        /// <summary>
        /// The check takes no dates. A caller that sends some anyway - an old client, a hand-written
        /// script - must not be able to move the check off today.
        /// </summary>
        // @driving_port @us-01 @error @real-io @contract-shape:pure-function
        [Test]
        public async Task Dates_sent_with_the_request_are_ignored_and_every_check_still_ends_today()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, ARequestCarryingDatesFromLastYear));

            ThenEveryCheckEndsToday(answer);
        }

        // --- Who may ask ---

        // @driving_port @us-01 @rbac @real-io @contract-shape:pure-function
        [Test]
        public async Task Tom_who_can_read_the_Team_but_not_change_it_gets_the_whole_answer()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenTomWhoCanOnlyReadTheTeamRunsTheCheck(oceanExplorer));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        // @driving_port @us-01 @rbac @error @real-io @contract-shape:unbounded-preservation
        /// <summary>
        /// Refused as "not found", exactly as the single back-test refuses, so the answer never reveals
        /// that a Team the person may not see exists at all.
        /// </summary>
        [Test]
        public async Task Somebody_who_cannot_read_the_Team_is_refused_and_learns_nothing_about_it()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            var harbourPilots = GivenAnotherTeam("Harbour Pilots");

            using var refused = await WhenSomebodyWhoCanReadOnlyAnotherTeamRunsTheCheck(oceanExplorer, harbourPilots);

            await ThenTheyAreToldItWasNotFoundWithoutASingleCheck(refused);
        }

        // @driving_port @us-01 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_Team_that_does_not_exist_is_answered_as_not_found()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            await GivenTheCheckAnswersForATeamThatExists(oceanExplorer);

            using var answer = await WhenTheCheckIsRunForATeamNobodyCreated();

            ThenThereIsNoSuchTeam(answer);
        }

        // --- What the answer states it checked ---

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Every_check_ends_today_and_reaches_back_by_its_own_length()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryCheckEndsToday(answer);
                ThenEachChecksHistorySitsImmediatelyBeforeWhatItScores(answer);
            }
        }

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task The_answer_states_exactly_what_it_checked_and_the_bar_each_check_had_to_clear()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await TheAnswerTo(oceanExplorer);

            ThenTheAnswerStatesTheLadderTheHorizonsTheLevelsAndTheBar(answer);
        }

        // @driving_port @us-01 @real-io @contract-shape:pure-function
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(90)]
        public async Task A_Team_whose_window_is_on_the_standard_ladder_is_checked_sixteen_times(int samplingWindowDays)
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(samplingWindowDays);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamWasCheckedAtTheseWindows(answer, StandardWindowDays);
                ThenTheTeamsOwnSettingIsReportedAs(answer, samplingWindowDays);
            }
        }

        /// <summary>
        /// Every worked example in the stories sits on the standard ladder, so this is the path a suite
        /// written from them alone would never run. The Team's own setting is the most decision-relevant
        /// check in the report and is always one of the windows swept.
        /// </summary>
        // @driving_port @us-01 @boundary @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(45)]
        [TestCase(7)]
        [TestCase(120)]
        public async Task A_Team_whose_window_is_off_the_ladder_has_it_checked_as_a_fifth_window(int samplingWindowDays)
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(samplingWindowDays);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamWasCheckedAtTheseWindows(answer, [.. StandardWindowDays, samplingWindowDays]);
                ThenTheTeamsOwnSettingIsReportedAs(answer, samplingWindowDays);
            }
        }

        /// <summary>
        /// The feature's central discipline, held at the wire rather than in review: the windows that
        /// behaved alike come back as a set in the ladder's own order, and nothing in the answer carries a
        /// number per window that anything could be sorted by.
        /// </summary>
        // @driving_port @us-01 @property @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(30)]
        [TestCase(45)]
        public async Task No_part_of_the_answer_can_rank_one_sampling_window_above_another(int samplingWindowDays)
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(samplingWindowDays);
            GivenTheWindowsBehaveDifferently(samplingWindowDays);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheWindowsThatBehavedAlikeComeBackInTheLaddersOwnOrder(answer);
                ThenNothingInTheAnswerCarriesANumberPerWindow(answer);
                ThenTheAnswerCarriesFactsAndNoSentence(answer);
            }
        }

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Every_check_that_was_run_is_in_the_answer_including_the_ones_that_could_not_be_evaluated()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var answer = await TheAnswerTo(coastalSurvey);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryWindowAndHorizonPairWasCheckedExactlyOnce(answer);
                ThenTheDenominatorCountsWhatWasEvaluated(answer);
                ThenEveryReasonAndOutcomeComesFromItsClosedSet(answer);
                ThenTheWindowCouldNotBeEvaluatedSoOnlySomeWindowsHoldUp(answer, 14);
            }
        }

        // --- The region, never a winner ---

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Every_window_behaving_alike_is_an_answer_that_says_the_setting_is_fine()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenEveryCheckHeldAtTheCautiousLevelsOnly();

            var answer = await TheAnswerTo(oceanExplorer);

            ThenEveryWindowIsInTheRegionAndTheSettingIsInsideIt(answer);
        }

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Deep_Currents_fourteen_day_window_over_forecast_three_times_in_four_and_sits_outside_the_region()
        {
            var deepCurrent = GivenDeepCurrentSetToFourteenDays();
            GivenTheFourteenDayWindowOverForecastInThreeOfItsFourChecks();

            var answer = await TheAnswerTo(deepCurrent);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, TheThreeLongerStandardWindows, SomeWindowsSound);
                ThenTheTeamsSettingIs(answer, 14, Outside);
                ThenThisManyOfTheWindowsChecksOverForecast(answer, 14, 3);
            }
        }

        /// <summary>
        /// The case the fifth window exists to find, and the reason the region is a set rather than a pair
        /// of bounds: the windows either side of the Team's own behaved, and its own did not. A pair of
        /// bounds would have said 14 to 90 and called the Team inside.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task The_windows_either_side_of_an_off_ladder_setting_can_hold_up_when_the_setting_itself_does_not()
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(45);
            Forecasts.EveryCheckOf(45, HeldUpTo.NoLevel);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, StandardWindowDays, SomeWindowsSound);
                ThenTheTeamsSettingIs(answer, 45, Outside);
                ThenNothingInTheAnswerCarriesANumberPerWindow(answer);
            }
        }

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task An_off_ladder_setting_can_hold_up_when_the_windows_either_side_of_it_do_not()
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(45);
            Forecasts.EveryCheckOf(30, HeldUpTo.NoLevel);
            Forecasts.EveryCheckOf(60, HeldUpTo.NoLevel);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, TheOwnWindowWithTheTwoOuterStandardOnes, SomeWindowsSound);
                ThenTheTeamsSettingIs(answer, 45, Inside);
            }
        }

        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task When_no_window_held_up_the_answer_says_so_rather_than_naming_the_least_bad()
        {
            var deepCurrent = GivenDeepCurrentSetToFourteenDays();
            Forecasts.Otherwise = HeldUpTo.NoLevel;

            var answer = await TheAnswerTo(deepCurrent);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, [], NoWindowSound);
                ThenTheTeamsSettingIs(answer, 14, Outside);
            }
        }

        // --- Where a window stops holding up ---

        /// <summary>
        /// A window holds up when its most cautious forecast held in more than half of the checks that ran
        /// on it. Falling short of even the 95% forecast in two of four is already ten times its own rate,
        /// so it is enough to put the window outside.
        /// </summary>
        // @driving_port @us-01 @boundary @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_window_that_fell_short_of_its_most_cautious_forecast_in_two_of_four_checks_is_outside_the_region()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenTheThirtyDayWindowFellShortOfItsMostCautiousForecastInTwoOfFourChecks();

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, TheShortestAndTheTwoLongestStandardWindows, SomeWindowsSound);
                ThenTheTeamsSettingIs(answer, 30, Outside);
                ThenThisManyOfTheWindowsChecksOverForecast(answer, 30, 2);
            }
        }

        /// <summary>
        /// Delivering more than even the optimistic forecast never counts against a window: landing above
        /// the median is what half of a well-judged forecast's checks do. It shows up in the 50% level's
        /// line instead. Only falling short of the cautious forecast counts.
        /// </summary>
        // @driving_port @us-01 @boundary @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Delivering_more_than_forecast_never_counts_against_a_window_only_falling_short_does()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenTheSixtyDayWindowFellShortOnceAndDeliveredMoreThanForecastThreeTimes();

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryWindowIsInTheRegionAndTheSettingIsInsideIt(answer);
                ThenThisManyOfTheWindowsChecksOverForecast(answer, 60, 1);
                ThenThisManyOfTheWindowsChecksLandedAboveTheBand(answer, 60, 3);
            }
        }

        /// <summary>
        /// A window some of whose checks could not run is judged on the ones that did - more than half of
        /// them must have held at 95%. One check that ran decides on its own; no minimum number of checks
        /// is added, because that would be a second sufficiency bar.
        /// </summary>
        // @driving_port @us-01 @boundary @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(2, 0, true)]
        [TestCase(2, 1, false)]
        [TestCase(1, 0, true)]
        [TestCase(1, 1, false)]
        public async Task A_window_only_some_of_whose_checks_could_run_is_judged_on_the_ones_that_did(int checksThatRan, int ofThemFellShort, bool holdsUp)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenOnlySomeOfTheNinetyDayWindowsChecksCouldBeWorkedOut(checksThatRan, ofThemFellShort);

            var answer = await TheAnswerTo(oceanExplorer);

            ThenTheWindowWasEvaluatedAndHoldsUp(answer, 90, holdsUp);
        }

        /// <summary>
        /// A window that could not be checked, between two that held up, is a gap in the region: the answer
        /// lists what held up around it rather than letting a span claim the window it never examined.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_window_that_could_not_be_checked_in_the_middle_of_the_ladder_is_a_gap_in_the_region()
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(60);
            GivenNoneOfTheThirtyDayWindowsChecksCouldBeWorkedOut();

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRegionIs(answer, TheShortestAndTheTwoLongestStandardWindows, SomeWindowsSound);
                ThenTheWindowCouldNotBeEvaluatedSoOnlySomeWindowsHoldUp(answer, 30);
                ThenTheTeamsSettingIs(answer, 60, Inside);
            }
        }

        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_Team_whose_history_supports_no_check_at_all_is_told_nothing_could_be_concluded()
        {
            var newlyFormed = GivenATeamThatHasFinishedAlmostNothing();

            var answer = await TheAnswerTo(newlyFormed);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryWindowAndHorizonPairWasCheckedExactlyOnce(answer);
                ThenNoCheckCouldBeEvaluated(answer);
                ThenTheRegionIs(answer, [], NotEnoughEvidence);
                ThenEveryWindowIsListedAsNotEvaluated(answer);
                ThenTheTeamsSettingIs(answer, 30, NotDetermined);
                ThenTheSettingIsNotTestedExactlyWhenAReasonIsGiven(answer);
            }
        }

        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task When_the_Teams_own_window_could_not_be_evaluated_its_standing_is_not_determined()
        {
            var team = GivenATeamFinishingWorkEveryFifthDaySetTo(14);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamsSettingIs(answer, 14, NotDetermined);
                ThenTheOwnWindowWasStillTested(answer);
                ThenTheWindowCouldNotBeEvaluatedSoOnlySomeWindowsHoldUp(answer, 14);
                ThenTheSettingIsNotTestedExactlyWhenAReasonIsGiven(answer);
            }
        }

        // --- Which levels held, against how often they should have ---

        /// <summary>
        /// A level holds when the Team delivered at least what the level forecast, and its nominal rate is
        /// the level itself. At the 50% level the wrong formula - counting the complement - gives the same
        /// number, which is how that mistake survived review once; the other three rows tell them apart.
        /// </summary>
        // @driving_port @us-01 @property @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(50, 8, 8.0)]
        [TestCase(70, 11, 11.2)]
        [TestCase(85, 14, 13.6)]
        [TestCase(95, 15, 15.2)]
        public async Task A_level_is_expected_to_hold_as_often_as_its_own_percentage_of_the_checks(int confidenceLevel, int timesItHeld, double timesItWasExpectedToHold)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            GivenTheSixteenChecksHeldUpToEightFiftiesThreeSeventiesThreeEightyFivesOneNinetyFiveAndOneNothing();

            var answer = await TheAnswerTo(oceanExplorer);

            ThenTheLevelHeldAgainstItsNominalRate(answer, confidenceLevel, timesItHeld, timesItWasExpectedToHold, SometimesHeld);
        }

        // @driving_port @us-01 @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(50, 0, 6.0, NeverHeld)]
        [TestCase(70, 0, 8.4, NeverHeld)]
        [TestCase(85, 12, 10.2, AlwaysHeld)]
        [TestCase(95, 12, 11.4, SometimesHeld)]
        public async Task Only_the_checks_that_could_run_count_towards_how_often_a_level_should_have_held(
            int confidenceLevel, int timesItHeld, double timesItWasExpectedToHold, string reading)
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();
            GivenEveryCheckHeldAtTheCautiousLevelsOnly();

            var answer = await TheAnswerTo(coastalSurvey);

            using (Assert.EnterMultipleScope())
            {
                ThenThisManyChecksCouldBeEvaluated(answer, 12);
                ThenTheLevelHeldAgainstItsNominalRate(answer, confidenceLevel, timesItHeld, timesItWasExpectedToHold, reading);
            }
        }

        /// <summary>
        /// The teaching case: a Team that never once reached even the most cautious number it was
        /// publishing. Every level reads as never held, which is over-forecasting, and the 95% line says
        /// how many times it should have held.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_Team_that_never_reached_even_its_most_cautious_forecast_is_told_no_level_ever_held()
        {
            var deepCurrent = GivenDeepCurrentSetToFourteenDays();
            Forecasts.Otherwise = HeldUpTo.NoLevel;

            var answer = await TheAnswerTo(deepCurrent);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryLevelReads(answer, NeverHeld);
                ThenTheLevelHeldAgainstItsNominalRate(answer, 95, 0, 15.2, NeverHeld);
                ThenEveryCheckLandedThisWay(answer, OverForecast);
            }
        }

        /// <summary>
        /// Always holding is a finding only where the level's own rate expected at least one whole miss.
        /// Sixteen checks at 95% expected 15.2 holds - under one miss - so holding in all sixteen is what
        /// that level predicted, and it reads as neither extreme.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_Team_that_always_beat_its_most_optimistic_forecast_is_told_which_levels_always_held_when_a_miss_was_expected()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.Otherwise = HeldUpTo.EveryLevel;

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenTheseLevelsRead(answer, TheThreeLevelsThatExpectedAMiss, AlwaysHeld);
                ThenTheLevelHeldAgainstItsNominalRate(answer, 50, 16, 8.0, AlwaysHeld);
                ThenTheLevelHeldAgainstItsNominalRate(answer, 95, 16, 15.2, SometimesHeld);
                ThenEveryCheckLandedThisWay(answer, UnderForecast);
            }
        }

        /// <summary>
        /// Where the Team's actual count landed against one check's band, from below the whole band to
        /// above it, and which of the four levels that means held.
        /// </summary>
        // @driving_port @us-01 @property @real-io @contract-shape:pure-function
        [TestCase(HeldUpTo.NoLevel, OverForecast)]
        [TestCase(HeldUpTo.NinetyFive, WithinBand)]
        [TestCase(HeldUpTo.EightyFive, WithinBand)]
        [TestCase(HeldUpTo.Seventy, WithinBand)]
        [TestCase(HeldUpTo.EveryLevel, UnderForecast)]
        public async Task Each_check_says_where_the_Teams_actual_landed_against_its_forecast(HeldUpTo held, string outcome)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.Check(30, 56, held);

            var answer = await TheAnswerTo(oceanExplorer);

            ThenTheCheckLandedAndItsLevelsHeld(answer.Cell(30, 56), outcome, held);
        }

        // --- Checks that could not be run ---

        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task Coastal_Surveys_fourteen_day_checks_hold_too_few_days_of_finished_work_and_are_named_as_unable_to_run()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var answer = await TheAnswerTo(coastalSurvey);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryCheckOfTheWindowCouldNotRunForTooFewDays(answer, 14);
                ThenThisManyChecksCouldBeEvaluated(answer, 12);
                ThenTheWindowIsNotClaimedAsHoldingUp(answer, 14);
                ThenTheWindowCouldNotBeEvaluatedSoOnlySomeWindowsHoldUp(answer, 14);
                ThenTheSettingIsNotTestedExactlyWhenAReasonIsGiven(answer);
            }
        }

        /// <summary>
        /// The shipped bar, unchanged and not duplicated: five days with finished work is enough to check,
        /// four is not. The days are placed well inside the one-week check's two-week history, so neither
        /// edge of that window decides the outcome.
        /// </summary>
        // @driving_port @us-01 @boundary @real-io @contract-shape:pure-function
        [TestCase(5, true)]
        [TestCase(4, false)]
        public async Task Five_days_with_finished_work_is_enough_to_check_and_four_is_not(int daysWithFinishedWork, bool canBeChecked)
        {
            var team = GivenATeamThatFinishedWorkOnlyOnThisManyDaysBeforeLastWeek(daysWithFinishedWork);

            var answer = await TheAnswerTo(team);

            ThenTheOneWeekCheckOfTheTwoWeekWindow(answer.Cell(14, 7), canBeChecked, daysWithFinishedWork);
        }

        /// <summary>
        /// A check can clear the bar and still come back from the engine with nothing a level can be read
        /// off. That is a second reason a check cannot be evaluated, not a second bar; and the engine's
        /// "no reading" marker must never reach a count, a band or a cell.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [Test]
        public async Task A_check_whose_forecast_could_not_be_worked_out_is_named_for_that_reason_and_counts_for_nothing()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.CannotBeWorkedOutFor(30, 28);

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenTheCheckCouldNotBeEvaluatedBecause(answer.Cell(30, 28), DegenerateForecast);
                ThenThisManyChecksCouldBeEvaluated(answer, 15);
                ThenNoNegativeNumberReachesTheAnswer(answer);
            }
        }

        // --- Teams without a rolling window of their own ---

        /// <summary>
        /// A Team that forecasts from fixed dates has no rolling window behind its forecasts, so there is
        /// nothing of its own to add. It still gets the standard sixteen checks - they are a true statement
        /// about how its history would have forecast - and is told its own setting was not what was tested.
        /// A stored window of zero as well is the case where both reasons apply; fixed dates wins, because
        /// it is why the stored window does not drive the Team's forecasts at all.
        /// </summary>
        // @driving_port @us-01 @error @real-io @kpi-OUT-4172-never-overclaims @contract-shape:pure-function
        [TestCase(45)]
        [TestCase(0)]
        public async Task A_Team_forecasting_from_fixed_dates_is_checked_at_the_standard_windows_and_told_its_own_setting_was_not_tested(int storedSamplingWindowDays)
        {
            var fixedDatesTeam = GivenATeamForecastingFromFixedDates("Lighthouse Keepers", storedSamplingWindowDays);
            GivenTheTeamFinishedAWorkItemEveryDay(fixedDatesTeam);

            var answer = await TheAnswerTo(fixedDatesTeam);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamWasCheckedAtTheseWindows(answer, StandardWindowDays);
                ThenTheOwnSettingIsReportedAsNotTested(answer, UsesFixedDates);
            }
        }

        // @driving_port @us-01 @error @real-io @contract-shape:pure-function
        [TestCase(0)]
        [TestCase(-7)]
        public async Task A_stored_window_that_is_not_a_length_of_time_adds_nothing_to_the_check(int storedSamplingWindowDays)
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(storedSamplingWindowDays);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamWasCheckedAtTheseWindows(answer, StandardWindowDays);
                ThenTheOwnSettingIsReportedAsNotTested(answer, NotAPositiveLength);
            }
        }

        // --- Read-only, end to end ---

        // @driving_port @us-01 @real-io @kpi-OUT-4172-read-only @contract-shape:unbounded-preservation
        [Test]
        public async Task Running_the_check_changes_nothing_about_the_Team_or_its_Work_Items()
        {
            var deepCurrent = GivenDeepCurrentSetToFourteenDays();
            GivenTheFourteenDayWindowOverForecastInThreeOfItsFourChecks();
            var storedBefore = WhatIsStoredFor(deepCurrent);

            var answer = await TheAnswerTo(deepCurrent);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamsSettingIs(answer, 14, Outside);
                ThenNothingAboutTheTeamChanged(deepCurrent, storedBefore);
            }
        }

        /// <summary>Nothing is stored between runs, so a second press over the same history answers identically.</summary>
        // @driving_port @us-01 @real-io @kpi-OUT-4172-read-only @contract-shape:unbounded-preservation
        [Test]
        public async Task Running_the_check_twice_gives_the_same_answer_twice()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var first = await TheAnswerTo(coastalSurvey);
            var second = await TheAnswerTo(coastalSurvey);

            ThenTheTwoAnswersAreTheSame(first, second);
        }

        // --- What must keep working beside it ---

        /// <summary>
        /// The single back-test sits in the same group on the same tab and its contract is not widened.
        /// Green before the check exists, and it stays the harness's own proof that seeding, the pinned
        /// clock and the scripted forecast reach the real forecast endpoints.
        /// </summary>
        // @driving_port @regression @coexistence @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task The_single_back_test_beside_the_check_still_answers_as_it_did()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            using var response = await WhenMariaBackTestsTheLastFourWeeksByHand(oceanExplorer);

            await ThenTheBackTestAnswersWithFourLevelsAndWhatTheTeamDelivered(response);
        }
    }
}
