using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Step definitions for slice 01 - the facts the verdict sentence is composed from, read off the wire.
    ///
    /// The Teams are the ones the stories name. Ocean Explorer finishes work every day and sits at 30
    /// days; Deep Current sits at 14; Coastal Survey finishes work in bursts, one day in five, which
    /// leaves every two-week window holding fewer than five days with finished work.
    /// </summary>
    public partial class Slice01OneSentenceAboutYourSamplingWindowTest : ForecastRealityCheckAcceptanceTest
    {
        /// <summary>The shipped data-sufficiency bar, written out so the answer is checked against the number and not against itself.</summary>
        private const int TheShippedBarInDays = 5;

        private const int LevelsPerCheck = 4;

        private const int ANumberNoTeamWasEverGiven = 987_654;

        // The closed sets the answer speaks in, spelled as they travel on the wire.
        private const string AllWindowsAlike = "AllWindowsAlike";
        private const string SomeWindowsSound = "SomeWindowsSound";
        private const string NoWindowSound = "NoWindowSound";
        private const string NotEnoughEvidence = "NotEnoughEvidence";
        private const string Inside = "Inside";
        private const string Outside = "Outside";
        private const string NotDetermined = "NotDetermined";
        private const string SometimesHeld = "SometimesHeld";
        private const string NeverHeld = "NeverHeld";
        private const string AlwaysHeld = "AlwaysHeld";
        private const string OverForecast = "OverForecast";
        private const string WithinBand = "WithinBand";
        private const string UnderForecast = "UnderForecast";
        private const string Sufficient = "Sufficient";
        private const string TooFewActiveDays = "TooFewActiveDays";
        private const string DegenerateForecast = "DegenerateForecast";

        private const string ARequestCarryingDatesFromLastYear =
            "{\"startDate\":\"2025-06-01\",\"endDate\":\"2025-07-01\",\"historicalStartDate\":\"2025-03-01\",\"historicalEndDate\":\"2025-06-01\"}";

        private static readonly int[] TheThreeLongerStandardWindows = [30, 60, 90];

        private static readonly int[] TheOwnWindowWithTheTwoOuterStandardOnes = [14, 45, 90];

        private static readonly int[] TheThreeLevelsThatExpectedAMiss = [50, 70, 85];

        private static readonly int[] TheShortestAndTheTwoLongestStandardWindows = [14, 60, 90];

        private static readonly string[] EveryReasonACheckCanGive = [Sufficient, TooFewActiveDays, DegenerateForecast];

        private static readonly string[] EveryWayACheckCanLand = [OverForecast, WithinBand, UnderForecast];

        /// <summary>
        /// Words whose presence in a property name would mean the answer names, orders or bounds a
        /// winner - or ships a rendered clause in one instance's vocabulary.
        /// </summary>
        private static readonly string[] WordsThatWouldNameAWinner =
            ["recommend", "best", "rank", "winner", "bound", "windowScore", "sentence", "clause"];

        // --- Given ---

        private int GivenOceanExplorerFinishingWorkEveryDay() => GivenATeamFinishingWorkEveryDay("Ocean Explorer", 30);

        private int GivenDeepCurrentSetToFourteenDays() => GivenATeamFinishingWorkEveryDay("Deep Current", 14);

        private int GivenATeamFinishingWorkEveryDaySetTo(int samplingWindowDays)
            => GivenATeamFinishingWorkEveryDay($"Team at {samplingWindowDays} days", samplingWindowDays);

        private int GivenCoastalSurveyWhichFinishesWorkInBursts() => GivenATeamFinishingWorkEveryFifthDay("Coastal Survey", 30);

        private int GivenATeamFinishingWorkEveryFifthDaySetTo(int samplingWindowDays)
            => GivenATeamFinishingWorkEveryFifthDay($"Bursty team at {samplingWindowDays} days", samplingWindowDays);

        private int GivenATeamFinishingWorkEveryDay(string name, int samplingWindowDays)
        {
            var team = GivenATeam(name, samplingWindowDays);
            GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private int GivenATeamFinishingWorkEveryFifthDay(string name, int samplingWindowDays)
        {
            var team = GivenATeam(name, samplingWindowDays);
            GivenTheTeamFinishedAWorkItemEvery(team, 5);
            return team;
        }

        /// <summary>Three finished Work Items in five months: no check's history holds five days with finished work.</summary>
        private int GivenATeamThatHasFinishedAlmostNothing()
        {
            var team = GivenATeam("Newly Formed", 30);
            GivenTheTeamFinishedWorkItemsOn(team, [TodayDay.AddDays(-1), TodayDay.AddDays(-40), TodayDay.AddDays(-100)]);
            return team;
        }

        /// <summary>
        /// Finished work on <paramref name="days"/> consecutive days eight to twelve days ago - inside the
        /// history of the one-week check of the two-week window, away from both its edges - and once more
        /// yesterday, inside the week that check scores.
        /// </summary>
        private int GivenATeamThatFinishedWorkOnlyOnThisManyDaysBeforeLastWeek(int days)
        {
            var team = GivenATeam($"Team with {days} busy days", 30);
            var closedOn = Enumerable.Range(8, days).Select(daysAgo => TodayDay.AddDays(-daysAgo)).ToList();
            closedOn.Add(TodayDay.AddDays(-1));
            GivenTheTeamFinishedWorkItemsOn(team, closedOn);
            return team;
        }

        private void GivenTheForecastIsWorkedOutAsShipped() => Forecasts.RunAsShipped();

        private void GivenEveryCheckHeldAtTheCautiousLevelsOnly() => Forecasts.Otherwise = HeldUpTo.EightyFive;

        /// <summary>The worked example from the stories: three of the fourteen-day window's four checks came in below the whole band.</summary>
        private void GivenTheFourteenDayWindowOverForecastInThreeOfItsFourChecks()
        {
            Forecasts.Check(14, 7, HeldUpTo.NoLevel);
            Forecasts.Check(14, 14, HeldUpTo.NoLevel);
            Forecasts.Check(14, 28, HeldUpTo.NoLevel);
            Forecasts.Check(14, 56, HeldUpTo.EightyFive);
        }

        /// <summary>
        /// One window over-forecasts every time, so the region is a proper subset and there is something
        /// an answer that ranked windows would have ranked.
        /// </summary>
        private void GivenTheWindowsBehaveDifferently(int samplingWindowDays)
        {
            var theOddOneOut = samplingWindowDays == 60 ? 90 : 60;
            Forecasts.EveryCheckOf(theOddOneOut, HeldUpTo.NoLevel);
        }

        /// <summary>
        /// Sixteen checks laid out so each level holds a different number of times: the 50% level in 8,
        /// the 70% in 11, the 85% in 14 and the 95% in 15.
        /// </summary>
        private void GivenTheSixteenChecksHeldUpToEightFiftiesThreeSeventiesThreeEightyFivesOneNinetyFiveAndOneNothing()
        {
            HeldUpTo[] howFarEachHeld =
            [
                .. Enumerable.Repeat(HeldUpTo.EveryLevel, 8),
                .. Enumerable.Repeat(HeldUpTo.Seventy, 3),
                .. Enumerable.Repeat(HeldUpTo.EightyFive, 3),
                HeldUpTo.NinetyFive,
                HeldUpTo.NoLevel,
            ];

            var checks = StandardWindowDays.SelectMany(window => HorizonDays.Select(horizon => (window, horizon))).ToList();
            for (var index = 0; index < checks.Count; index++)
            {
                Forecasts.Check(checks[index].window, checks[index].horizon, howFarEachHeld[index]);
            }
        }

        private void GivenTheThirtyDayWindowFellShortOfItsMostCautiousForecastInTwoOfFourChecks()
        {
            Forecasts.Check(30, 7, HeldUpTo.NoLevel);
            Forecasts.Check(30, 14, HeldUpTo.NoLevel);
            Forecasts.Check(30, 28, HeldUpTo.EightyFive);
            Forecasts.Check(30, 56, HeldUpTo.EightyFive);
        }

        private void GivenTheSixtyDayWindowFellShortOnceAndDeliveredMoreThanForecastThreeTimes()
        {
            Forecasts.Check(60, 7, HeldUpTo.NoLevel);
            Forecasts.Check(60, 14, HeldUpTo.EveryLevel);
            Forecasts.Check(60, 28, HeldUpTo.EveryLevel);
            Forecasts.Check(60, 56, HeldUpTo.EveryLevel);
        }

        /// <summary>
        /// The engine comes back with nothing it can read a level from for all but <paramref name="checksThatRan"/>
        /// of the window's checks; of those that ran, the first <paramref name="ofThemFellShort"/> fell short
        /// of even the 95% forecast and the rest held at it.
        /// </summary>
        private void GivenOnlySomeOfTheNinetyDayWindowsChecksCouldBeWorkedOut(int checksThatRan, int ofThemFellShort)
        {
            var cannotBeWorkedOut = HorizonDays.Take(HorizonDays.Length - checksThatRan);
            var ran = HorizonDays.Skip(HorizonDays.Length - checksThatRan).ToList();

            foreach (var horizon in cannotBeWorkedOut)
            {
                Forecasts.CannotBeWorkedOutFor(90, horizon);
            }

            for (var index = 0; index < ran.Count; index++)
            {
                Forecasts.Check(90, ran[index], index < ofThemFellShort ? HeldUpTo.NoLevel : HeldUpTo.EightyFive);
            }
        }

        private void GivenNoneOfTheThirtyDayWindowsChecksCouldBeWorkedOut()
        {
            foreach (var horizon in HorizonDays)
            {
                Forecasts.CannotBeWorkedOutFor(30, horizon);
            }
        }

        private async Task GivenTheCheckAnswersForATeamThatExists(int teamId) => await TheAnswerTo(teamId);

        // --- When ---

        private Task<HttpResponseMessage> WhenTomWhoCanOnlyReadTheTeamRunsTheCheck(int teamId)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(teamId, "tom-becker"), teamId);

        private Task<HttpResponseMessage> WhenSomebodyWhoCanReadOnlyAnotherTeamRunsTheCheck(int teamId, int theTeamTheyCanRead)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(theTeamTheyCanRead, "harbour-pilot"), teamId);

        private Task<HttpResponseMessage> WhenTheCheckIsRunForATeamNobodyCreated()
            => WhenTheCheckIsRunBy(client => client.AsSystemAdmin(), ANumberNoTeamWasEverGiven);

        private async Task<HttpResponseMessage> WhenMariaBackTestsTheLastFourWeeksByHand(int teamId)
        {
            Client.AsTeamViewer(teamId, "maria-santos");
            var body =
                $"{{\"startDate\":\"{TodayDay.AddDays(-28):yyyy-MM-dd}\",\"endDate\":\"{TodayDay:yyyy-MM-dd}\"," +
                $"\"historicalStartDate\":\"{TodayDay.AddDays(-58):yyyy-MM-dd}\",\"historicalEndDate\":\"{TodayDay.AddDays(-28):yyyy-MM-dd}\"}}";

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"/api/latest/forecast/backtest/{teamId}", content);
        }

        // --- Then: what was checked ---

        private static void ThenEveryCheckEndsToday(RealityCheckAnswer answer)
        {
            var endingElsewhere = answer.Cells.Where(cell => cell.ScoredPeriodEnd != TodayDay).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.AnchorDate, Is.EqualTo(TodayDay),
                    "every check ends today; an answer anchored on any other day scores a period nobody asked about");
                Assert.That(endingElsewhere, Is.Empty,
                    "a check that does not end today is measured against a different stretch of time than the one the answer claims");
                Assert.That(answer.Cells, Is.Not.Empty, "an answer with no checks satisfies 'every check ends today' by having none");
            }
        }

        private static void ThenEachChecksHistorySitsImmediatelyBeforeWhatItScores(RealityCheckAnswer answer)
        {
            var misplaced = answer.Cells
                .Where(cell => cell.ScoredPeriodStart != TodayDay.AddDays(-cell.HorizonDays)
                    || cell.HistoryWindowEnd != cell.ScoredPeriodStart
                    || cell.HistoryWindowStart != cell.ScoredPeriodStart.AddDays(-cell.SamplingWindowDays))
                .Select(cell => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d: history {cell.HistoryWindowStart}..{cell.HistoryWindowEnd}, scored {cell.ScoredPeriodStart}..{cell.ScoredPeriodEnd}")
                .ToList();

            Assert.That(misplaced, Is.Empty,
                "each check scores the stretch reaching back from today by its horizon, and learns from the window immediately before it");
        }

        private static void ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells, Has.Count.EqualTo(16));
                Assert.That(answer.Cells.Where(cell => !cell.IsSufficient), Is.Empty,
                    "this Team finishes work every day, so every check has plenty of history");
                Assert.That(answer.RunsEvaluated, Is.EqualTo(16));
            }
        }

        private static void ThenTheAnswerStatesTheLadderTheHorizonsTheLevelsAndTheBar(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StandardWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(answer.SampledHorizonDays, Is.EqualTo(HorizonDays),
                    "one, two, four and eight weeks - the horizons are printed, not assumed");
                Assert.That(answer.ConfidenceLevels, Is.EqualTo(ConfidenceLevels));
                Assert.That(answer.MinimumActiveDays, Is.EqualTo(TheShippedBarInDays),
                    "the bar a check's history had to clear is the shipped one, echoed rather than restated");
                Assert.That(answer.LevelsPerRun, Is.EqualTo(LevelsPerCheck));
                Assert.That(answer.RunsAttempted, Is.EqualTo(16));
            }
        }

        private static void ThenTheTeamWasCheckedAtTheseWindows(RealityCheckAnswer answer, int[] windows)
        {
            var expected = windows.Where(days => days > 0).Distinct().Order().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StandardWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(answer.SampledWindowDays, Is.EqualTo(expected),
                    "the windows swept are the standard ladder plus the Team's own rolling window, in ascending order, once each");
                Assert.That(answer.Cells, Has.Count.EqualTo(expected.Count * HorizonDays.Length));
                Assert.That(answer.RunsAttempted, Is.EqualTo(expected.Count * HorizonDays.Length),
                    "the denominator says how many checks this Team got - sixteen or twenty - not how many another Team got");
            }
        }

        private static void ThenTheTeamsOwnSettingIsReportedAs(RealityCheckAnswer answer, int samplingWindowDays)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.CurrentSettingDays, Is.EqualTo(samplingWindowDays));
                Assert.That(answer.CurrentSettingWasTested, Is.True,
                    "the Team's own window is always one of the windows swept, so its setting was tested");
            }
        }

        private static void ThenEveryWindowAndHorizonPairWasCheckedExactlyOnce(RealityCheckAnswer answer)
        {
            var expected = answer.SampledWindowDays
                .SelectMany(window => answer.SampledHorizonDays.Select(horizon => $"{window}d/{horizon}d"))
                .Order(StringComparer.Ordinal)
                .ToList();
            var present = answer.Cells
                .Select(cell => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d")
                .Order(StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(present, Is.EqualTo(expected),
                    "a check that could not run is present with its reason, never left out - a missing check reads as one that was fine");
                Assert.That(expected, Is.Not.Empty);
            }
        }

        private static void ThenTheDenominatorCountsWhatWasEvaluated(RealityCheckAnswer answer)
        {
            var evaluable = answer.Cells.Count(cell => cell.IsSufficient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.RunsAttempted, Is.EqualTo(answer.Cells.Count));
                Assert.That(answer.RunsEvaluated, Is.EqualTo(evaluable));
                Assert.That(answer.LevelsPerRun, Is.EqualTo(LevelsPerCheck));
                Assert.That(answer.ScoresEvaluated, Is.EqualTo(answer.RunsEvaluated * answer.LevelsPerRun),
                    "the scores the answer claims are the ones actually evaluated, not the ones attempted");
            }
        }

        // --- Then: the region ---

        private static void ThenEveryWindowIsInTheRegionAndTheSettingIsInsideIt(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.SoundWindowDays, Is.EqualTo(answer.SampledWindowDays),
                    "when every window behaved alike, the region is all of them");
                Assert.That(answer.Determination, Is.EqualTo(AllWindowsAlike));
                Assert.That(answer.CurrentSettingStanding, Is.EqualTo(Inside),
                    "the null result is an answer: the setting is fine");
            }
        }

        private static void ThenTheRegionIs(RealityCheckAnswer answer, int[] soundWindows, string determination)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.SoundWindowDays, Is.EqualTo(soundWindows));
                Assert.That(answer.Determination, Is.EqualTo(determination));
            }
        }

        private static void ThenTheTeamsSettingIs(RealityCheckAnswer answer, int samplingWindowDays, string standing)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.CurrentSettingDays, Is.EqualTo(samplingWindowDays));
                Assert.That(answer.CurrentSettingWasTested, Is.True);
                Assert.That(answer.CurrentSettingStanding, Is.EqualTo(standing));
            }
        }

        private static void ThenThisManyOfTheWindowsChecksOverForecast(RealityCheckAnswer answer, int samplingWindowDays, int howMany)
        {
            Assert.That(answer.CellsFor(samplingWindowDays).Count(cell => cell.Outcome == OverForecast), Is.EqualTo(howMany),
                $"the {samplingWindowDays}-day window came in below the whole band in {howMany} of its checks");
        }

        private static void ThenThisManyOfTheWindowsChecksLandedAboveTheBand(RealityCheckAnswer answer, int samplingWindowDays, int howMany)
        {
            Assert.That(answer.CellsFor(samplingWindowDays).Count(cell => cell.Outcome == UnderForecast), Is.EqualTo(howMany),
                $"the {samplingWindowDays}-day window's checks that landed above the whole band are still reported as such, check by check");
        }

        private static void ThenTheWindowWasEvaluatedAndHoldsUp(RealityCheckAnswer answer, int samplingWindowDays, bool holdsUp)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.UnevaluatedWindowDays, Does.Not.Contain(samplingWindowDays),
                    "a window at least one of whose checks ran was evaluated");
                Assert.That(answer.SoundWindowDays.Contains(samplingWindowDays), Is.EqualTo(holdsUp),
                    $"the {samplingWindowDays}-day window holds up exactly when its 95% forecast held in more than half of the checks that ran");
            }
        }

        private static void ThenTheWindowsThatBehavedAlikeComeBackInTheLaddersOwnOrder(RealityCheckAnswer answer)
        {
            var sampled = answer.SampledWindowDays;
            var sound = answer.SoundWindowDays;
            var ownWindow = answer.CurrentSettingDays;
            var ladderWithTheOwnWindow = answer.StandardWindowDays.Append(ownWindow).Where(days => days > 0).Distinct().Order().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sampled, Is.EqualTo(ladderWithTheOwnWindow),
                    "the windows swept are a fixed ascending ladder - there is no order in them but length");
                Assert.That(sound, Is.EqualTo(sampled.Where(sound.Contains).ToList()),
                    "the windows that behaved alike keep the ladder's own order; any other order would be a ranking");
                Assert.That(answer.UnevaluatedWindowDays, Is.EqualTo(sampled.Where(answer.UnevaluatedWindowDays.Contains).ToList()),
                    "the windows that could not be checked keep the ladder's own order too");
                Assert.That(answer.UnevaluatedWindowDays.Intersect(sound), Is.Empty,
                    "a window is never both holding up and not evaluated");
                Assert.That(sound, Has.Count.InRange(1, sampled.Count - 1),
                    "the arrangement makes one window behave differently, so an order other than the ladder's would have something to rank");
            }
        }

        private static void ThenNothingInTheAnswerCarriesANumberPerWindow(RealityCheckAnswer answer)
        {
            var findings = new List<string>();
            InspectForAnythingThatCouldRankAWindow(answer.Root, "$", findings);

            Assert.That(findings, Is.Empty,
                "no part of the answer may carry a number per sampling window: with nothing to sort by, no ranking is representable");
        }

        private static void ThenTheAnswerCarriesFactsAndNoSentence(RealityCheckAnswer answer)
        {
            var credited = new List<string>();
            CollectTextMentioning(answer.Root, "Brown", credited);

            Assert.That(credited, Is.Empty,
                "the answer carries facts; any mention of the source method belongs beside a statement of where this check departs from it, which is copy the browser composes");
        }

        // --- Then: which levels held ---

        private static void ThenEachLevelHeldExactlyWhenTheTeamDeliveredAtLeastItsForecast(RealityCheckAnswer answer)
        {
            var disagreements = new List<string>();

            foreach (var cell in answer.Cells.Where(cell => cell.IsSufficient))
            {
                if (cell.ForecastByLevel.Count != LevelsPerCheck || cell.ActualCompleted is null)
                {
                    disagreements.Add($"{cell.SamplingWindowDays}d/{cell.HorizonDays}d carries no complete forecast or no actual");
                    continue;
                }

                var byLevel = cell.ForecastByLevel;
                if (!(byLevel[95] <= byLevel[85] && byLevel[85] <= byLevel[70] && byLevel[70] <= byLevel[50]))
                {
                    disagreements.Add($"{cell.SamplingWindowDays}d/{cell.HorizonDays}d reads its levels out of order");
                }

                disagreements.AddRange(ConfidenceLevels
                    .Where(level => cell.HeldByLevel.GetValueOrDefault(level) != (cell.ActualCompleted >= byLevel[level]))
                    .Select(level => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d at {level}%: forecast {byLevel[level]}, actual {cell.ActualCompleted}"));
            }

            Assert.That(disagreements, Is.Empty,
                "a level held exactly when the Team delivered at least what that level forecast");
        }

        private static void ThenTheLevelHeldAgainstItsNominalRate(
            RealityCheckAnswer answer, int confidenceLevel, int timesItHeld, double timesItWasExpectedToHold, string reading)
        {
            var coverage = answer.Coverage(confidenceLevel);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coverage.HeldCount, Is.EqualTo(timesItHeld));
                Assert.That(coverage.ExpectedHeldCount, Is.EqualTo(timesItWasExpectedToHold).Within(0.001),
                    $"a {confidenceLevel}% level is expected to hold in {confidenceLevel}% of the checks that could run - not in the rest");
                Assert.That(coverage.Reading, Is.EqualTo(reading));
            }
        }

        private static void ThenEveryLevelReads(RealityCheckAnswer answer, string reading)
            => ThenTheseLevelsRead(answer, ConfidenceLevels, reading);

        private static void ThenTheseLevelsRead(RealityCheckAnswer answer, int[] levels, string reading)
        {
            var readings = levels.Select(level => answer.Coverage(level).Reading).ToList();

            Assert.That(readings, Is.All.EqualTo(reading));
        }

        private static void ThenEveryCheckLandedThisWay(RealityCheckAnswer answer, string outcome)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells.Select(cell => cell.Outcome), Is.All.EqualTo(outcome));
                Assert.That(answer.Cells, Is.Not.Empty);
            }
        }

        private static void ThenTheCheckLandedAndItsLevelsHeld(CellReading cell, string outcome, HeldUpTo held)
        {
            var expectedHeld = new Dictionary<int, bool>
            {
                [95] = held >= HeldUpTo.NinetyFive,
                [85] = held >= HeldUpTo.EightyFive,
                [70] = held >= HeldUpTo.Seventy,
                [50] = held >= HeldUpTo.EveryLevel,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cell.Outcome, Is.EqualTo(outcome));
                Assert.That(cell.HeldByLevel, Is.EquivalentTo(expectedHeld),
                    "the actual's position in the band is what says which levels held: every level at or below it held");
            }
        }

        private static void ThenThisManyChecksCouldBeEvaluated(RealityCheckAnswer answer, int howMany)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells.Count(cell => cell.IsSufficient), Is.EqualTo(howMany));
                Assert.That(answer.RunsEvaluated, Is.EqualTo(howMany));
                Assert.That(answer.ScoresEvaluated, Is.EqualTo(howMany * LevelsPerCheck));
            }
        }

        private static void ThenNoCheckCouldBeEvaluated(RealityCheckAnswer answer)
        {
            var lines = ConfidenceLevels.Select(answer.Coverage).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells.Where(cell => cell.IsSufficient), Is.Empty);
                Assert.That(answer.RunsEvaluated, Is.Zero);
                Assert.That(answer.ScoresEvaluated, Is.Zero);
                Assert.That(lines.Select(line => line.HeldCount), Is.All.Zero);
                Assert.That(lines.Select(line => line.ExpectedHeldCount), Is.All.Zero,
                    "with nothing evaluated, no level was expected to hold anywhere");
                Assert.That(lines.Select(line => line.Reading), Is.All.EqualTo(NotEvaluated),
                    "a level no check could test reads as not evaluated, never as never held");
            }
        }

        // --- Then: checks that could not run ---

        private static void ThenEveryCheckOfTheWindowCouldNotRunForTooFewDays(RealityCheckAnswer answer, int samplingWindowDays)
        {
            var checks = answer.CellsFor(samplingWindowDays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checks, Has.Count.EqualTo(HorizonDays.Length));
                Assert.That(checks.Select(cell => cell.Reason), Is.All.EqualTo(TooFewActiveDays));
                Assert.That(checks.Select(cell => cell.DaysWithCompletedWork), Is.All.InRange(1, TheShippedBarInDays - 1),
                    "the reason carries the number the forecaster needs: how many days with finished work there were, against five");
                Assert.That(checks.Where(cell => cell.IsSufficient || cell.Outcome is not null || cell.ForecastByLevel.Count > 0), Is.Empty,
                    "a check that could not run reports no forecast and no outcome, so nothing can read it as a result");
            }
        }

        private static void ThenTheWindowIsNotClaimedAsHoldingUp(RealityCheckAnswer answer, int samplingWindowDays)
        {
            Assert.That(answer.SoundWindowDays, Does.Not.Contain(samplingWindowDays),
                "a window none of whose checks could run cannot be claimed to have held up - the answer would be asserting something it never examined");
        }

        private static void ThenTheOneWeekCheckOfTheTwoWeekWindow(CellReading cell, bool canBeChecked, int daysWithFinishedWork)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cell.IsSufficient, Is.EqualTo(canBeChecked));
                Assert.That(cell.DaysWithCompletedWork, Is.EqualTo(daysWithFinishedWork));
                Assert.That(cell.Reason, Is.EqualTo(canBeChecked ? Sufficient : TooFewActiveDays));
            }
        }

        private static void ThenTheCheckCouldNotBeEvaluatedBecause(CellReading cell, string reason)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cell.IsSufficient, Is.False);
                Assert.That(cell.Reason, Is.EqualTo(reason));
                Assert.That(cell.Outcome, Is.Null);
                Assert.That(cell.ForecastByLevel, Is.Empty);
            }
        }

        private static void ThenNoNegativeNumberReachesTheAnswer(RealityCheckAnswer answer)
        {
            var negatives = new List<string>();
            CollectNegativeNumbers(answer.Root, "$", negatives);

            Assert.That(negatives, Is.Empty,
                "the engine's 'no reading' marker is minus one, and it must never reach a count, a band or a cell");
        }

        // --- Then: Teams without a rolling window ---

        private static void ThenTheOwnSettingIsReportedAsNotTested(RealityCheckAnswer answer, string reason)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.CurrentSettingWasTested, Is.False);
                Assert.That(answer.CurrentSettingStanding, Is.EqualTo(NotTested),
                    "a setting that was not tested cannot be reported inside or outside anything, nor as checked and inconclusive");
                Assert.That(answer.CurrentSettingNotTestedReason, Is.EqualTo(reason));
            }
        }

        /// <summary>
        /// Three fields say one thing, so they must never disagree: not tested, the standing that says so,
        /// and a reason for it all come together or not at all.
        /// </summary>
        private static void ThenTheSettingIsNotTestedExactlyWhenAReasonIsGiven(RealityCheckAnswer answer)
        {
            var notTested = !answer.CurrentSettingWasTested;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.CurrentSettingStanding == NotTested, Is.EqualTo(notTested),
                    "the standing reads NotTested exactly when the setting was not tested");
                Assert.That(answer.CurrentSettingNotTestedReason is not null, Is.EqualTo(notTested),
                    "a reason travels exactly when the setting was not tested");
            }
        }

        /// <summary>
        /// A window none of whose checks could run is named as not evaluated, never as holding up, and it
        /// keeps the answer from claiming every window behaved alike.
        /// </summary>
        private static void ThenTheWindowCouldNotBeEvaluatedSoOnlySomeWindowsHoldUp(RealityCheckAnswer answer, int samplingWindowDays)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.UnevaluatedWindowDays, Does.Contain(samplingWindowDays));
                Assert.That(answer.SoundWindowDays, Does.Not.Contain(samplingWindowDays));
                Assert.That(answer.Determination, Is.EqualTo(SomeWindowsSound),
                    "'all windows alike' is a claim about every window swept, and this one could not be checked");
            }
        }

        private static void ThenEveryWindowIsListedAsNotEvaluated(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.UnevaluatedWindowDays, Is.EqualTo(answer.SampledWindowDays));
                Assert.That(answer.SampledWindowDays, Is.Not.Empty);
            }
        }

        private static void ThenTheOwnWindowWasStillTested(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.SampledWindowDays, Does.Contain(answer.CurrentSettingDays));
                Assert.That(answer.CellsFor(answer.CurrentSettingDays).Where(cell => cell.IsSufficient), Is.Empty,
                    "every check of the Team's own window lacked the history to run");
            }
        }

        // --- Then: read-only ---

        private void ThenNothingAboutTheTeamChanged(int teamId, TeamAsStored before)
        {
            Assert.That(WhatIsStoredFor(teamId), Is.EqualTo(before),
                "the check reads; it never writes a Team setting, a Work Item or anything else");
        }

        private static void ThenTheTwoAnswersAreTheSame(RealityCheckAnswer first, RealityCheckAnswer second)
        {
            Assert.That(second.Root.GetRawText(), Is.EqualTo(first.Root.GetRawText()),
                "nothing is kept between runs, so the same history answers the same way");
        }

        private static async Task ThenTheyAreRefusedWithoutASingleCheck(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(body, Does.Not.Contain("cells"));
            }
        }

        private static async Task ThenTheRequestIsRefusedWithoutASingleCheck(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Not.Contain("cells"));
            }
        }

        private static void ThenEveryReasonAndOutcomeComesFromItsClosedSet(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells.Select(cell => cell.Reason), Is.All.AnyOf(EveryReasonACheckCanGive),
                    "a check is evaluable, or unevaluable for one of two named reasons - there is no third that could reach the screen without its own words");
                Assert.That(answer.Cells.Where(cell => cell.Outcome is not null).Select(cell => cell.Outcome), Is.All.AnyOf(EveryWayACheckCanLand));
                Assert.That(answer.Cells.Where(cell => !cell.IsSufficient), Is.Not.Empty,
                    "this Team's two-week windows are too thin, so the closed set is exercised on both sides");
            }
        }

        private static void ThenThereIsNoSuchTeam(HttpResponseMessage response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private static async Task ThenTheBackTestAnswersWithFourLevelsAndWhatTheTeamDelivered(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var result = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Field(result, "percentiles").GetArrayLength(), Is.EqualTo(LevelsPerCheck));
                Assert.That(Field(result, "actualThroughput").GetInt32(), Is.GreaterThan(0),
                    "the Team finished work every day of the four weeks it was back-tested over");
            }
        }

        // --- Walking the answer ---

        private static void InspectForAnythingThatCouldRankAWindow(JsonElement element, string path, List<string> findings)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectForAnythingThatCouldRankAWindow(item, $"{path}[{index++}]", findings);
                }

                return;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var names = element.EnumerateObject().Select(property => property.Name).ToList();

            if (names.Contains("samplingWindowDays") && !names.Contains("horizonDays"))
            {
                findings.Add($"{path} is about one sampling window rather than one check");
            }

            findings.AddRange(names
                .Where(name => int.TryParse(name, out _))
                .Select(name => $"{path}.{name} keys something by a number"));

            findings.AddRange(names
                .Where(name => WordsThatWouldNameAWinner.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Select(name => $"{path}.{name}"));

            foreach (var property in element.EnumerateObject())
            {
                InspectForAnythingThatCouldRankAWindow(property.Value, $"{path}.{property.Name}", findings);
            }
        }

        private static void CollectTextMentioning(JsonElement element, string word, List<string> found)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String when (element.GetString() ?? string.Empty).Contains(word, StringComparison.OrdinalIgnoreCase):
                    found.Add(element.GetString()!);
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectTextMentioning(item, word, found);
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        CollectTextMentioning(property.Value, word, found);
                    }

                    break;
            }
        }

        private static void CollectNegativeNumbers(JsonElement element, string path, List<string> found)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number when element.GetDouble() < 0:
                    found.Add($"{path} = {element.GetRawText()}");
                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectNegativeNumbers(item, $"{path}[{index++}]", found);
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        CollectNegativeNumbers(property.Value, $"{path}.{property.Name}", found);
                    }

                    break;
            }
        }
    }
}
