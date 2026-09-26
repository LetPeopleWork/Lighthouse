namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Step definitions for slice 04 - every period's dates and actual, read off the wire.
    ///
    /// Ocean Explorer finishes one Work Item every day, so a period of H days holds exactly H. Coastal Survey
    /// finishes work one day in five, which leaves its two-week window unable to run. The newly formed Team has
    /// finished three Work Items in five months, so no check can run at all. Harbour Pilots finished work
    /// every day until a week ago and nothing since - the whole Team was away.
    /// </summary>
    public partial class Slice04TheAnswerOpensInADialogTest : ForecastRealityCheckAcceptanceTest
    {
        private const string Pending = "Pending: the answer does not list the periods it scored yet (epic 4172, slice 04, story 6094).";

        private const int LongestHistoryAnyCheckReaches = 160;

        /// <summary>Finished yesterday, 40 days ago and 100 days ago: one in each of the three shorter periods, two in the eight-week one.</summary>
        private static readonly Dictionary<int, int> TheNewlyFormedTeamsActualsByHorizon = new() { [7] = 1, [14] = 1, [28] = 1, [56] = 2 };

        /// <summary>Nothing in the last seven days; one a day on every day before that.</summary>
        private static readonly Dictionary<int, int> TheHarbourPilotsActualsByHorizon = new() { [7] = 0, [14] = 7, [28] = 21, [56] = 49 };

        // --- Given ---

        private int GivenOceanExplorerFinishingAWorkItemEveryDay()
        {
            var team = GivenATeam("Ocean Explorer", 30);
            GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private int GivenATeamFinishingAWorkItemEveryDaySetTo(int samplingWindowDays)
        {
            var team = GivenATeam($"Team at {samplingWindowDays} days", samplingWindowDays);
            GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private int GivenCoastalSurveyWhichFinishesWorkInBursts()
        {
            var team = GivenATeam("Coastal Survey", 30);
            GivenTheTeamFinishedAWorkItemEvery(team, 5);
            return team;
        }

        private int GivenATeamThatHasFinishedAlmostNothing()
        {
            var team = GivenATeam("Newly Formed", 30);
            GivenTheTeamFinishedWorkItemsOn(team, [TodayDay.AddDays(-1), TodayDay.AddDays(-40), TodayDay.AddDays(-100)]);
            return team;
        }

        private int GivenHarbourPilotsWhoFinishedNothingLastWeek()
        {
            var team = GivenATeam("Harbour Pilots", 30);
            var closedOn = Enumerable.Range(7, LongestHistoryAnyCheckReaches - 7)
                .Select(daysAgo => TodayDay.AddDays(-daysAgo))
                .ToList();
            GivenTheTeamFinishedWorkItemsOn(team, closedOn);
            return team;
        }

        // --- Then ---

        private static void ThenThereIsOnePeriodPerHorizonInHorizonOrder(RealityCheckAnswer answer)
        {
            var horizons = answer.ScoredPeriods.Select(period => period.HorizonDays).ToList();

            Assert.That(horizons, Is.EqualTo(answer.SampledHorizonDays),
                "one period per horizon, in the order the horizons were swept - never one per window");
        }

        private static void ThenEachPeriodEndsTodayAndHoldsExactlyItsHorizon(RealityCheckAnswer answer)
        {
            var misdated = answer.ScoredPeriods
                .Where(period => period.ScoredPeriodEnd != TodayDay
                    || period.ScoredPeriodStart != TodayDay.AddDays(1 - period.HorizonDays))
                .Select(period => $"{period.HorizonDays}d: {period.ScoredPeriodStart}..{period.ScoredPeriodEnd}")
                .ToList();

            Assert.That(misdated, Is.Empty,
                "every period ends today and holds exactly its horizon, first and last day included");
        }

        private static void ThenEachPeriodSaysTheTeamDeliveredOneWorkItemForEachOfItsDays(RealityCheckAnswer answer)
        {
            var miscounted = answer.ScoredPeriods
                .Where(period => period.ActualCompleted != period.HorizonDays)
                .Select(period => $"{period.HorizonDays}d: actual {period.ActualCompleted?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing"}")
                .ToList();

            Assert.That(miscounted, Is.Empty, "one Work Item was finished on each day, so a period of H days holds exactly H");
        }

        private static void ThenThePeriodsSayTheTeamDelivered(RealityCheckAnswer answer, Dictionary<int, int> expectedByHorizon)
        {
            var delivered = answer.ScoredPeriods.ToDictionary(period => period.HorizonDays, period => period.ActualCompleted);

            Assert.That(delivered, Is.EquivalentTo(expectedByHorizon.ToDictionary(pair => pair.Key, pair => (int?)pair.Value)),
                "every period says what the Team delivered in it, zero included, whether or not any check of it could run");
        }

        private static void ThenEveryCheckWasScoredOnTheDaysOfItsPeriod(RealityCheckAnswer answer)
        {
            var periods = answer.ScoredPeriods.ToDictionary(period => period.HorizonDays);

            var elsewhere = answer.Cells
                .Where(cell => !periods.TryGetValue(cell.HorizonDays, out var period)
                    || cell.ScoredPeriodStart != period.ScoredPeriodStart
                    || cell.ScoredPeriodEnd != period.ScoredPeriodEnd)
                .Select(cell => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d")
                .ToList();

            Assert.That(elsewhere, Is.Empty, "every check of a horizon is scored on that horizon's period and no other");
        }

        private static void ThenEveryEvaluatedCheckCarriesItsPeriodsActual(RealityCheckAnswer answer)
        {
            var periods = answer.ScoredPeriods.ToDictionary(period => period.HorizonDays, period => period.ActualCompleted);

            var disagreeing = answer.Cells
                .Where(cell => cell.IsSufficient)
                .Where(cell => !periods.TryGetValue(cell.HorizonDays, out var actual) || actual != cell.ActualCompleted)
                .Select(cell => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d: cell {cell.ActualCompleted}")
                .ToList();

            Assert.That(disagreeing, Is.Empty, "a check that ran carries exactly the actual of the period it was scored on");
        }

        private static void ThenEveryCheckThatCouldNotRunStillCarriesNoActualOfItsOwn(RealityCheckAnswer answer)
        {
            var carryingOne = answer.Cells
                .Where(cell => !cell.IsSufficient && cell.ActualCompleted is not null)
                .Select(cell => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d")
                .ToList();

            Assert.That(carryingOne, Is.Empty,
                "a check that could not run keeps carrying no actual; the period's actual lives on the period");
        }

        private static void ThenNoCheckCarriesAnActual(RealityCheckAnswer answer)
        {
            Assert.That(answer.Cells.Where(cell => cell.ActualCompleted is not null), Is.Empty,
                "no check could run, so no check carries an actual - only the periods do");
        }

        private static void ThenTwentyChecksWereRun(RealityCheckAnswer answer)
        {
            Assert.That(answer.Cells, Has.Count.EqualTo(20), "four standard windows and the Team's own, at four horizons each");
        }
    }
}
