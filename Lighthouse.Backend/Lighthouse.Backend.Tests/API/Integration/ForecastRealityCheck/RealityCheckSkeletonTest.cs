namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// A Team's reality check runs and its answer comes back, through the whole wiring: the route, sign-in,
    /// the controller, the shipped forecast engine and the real database. Every other scenario either scripts
    /// the forecast or asks the check's service directly; this one proves the pieces are joined.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class RealityCheckSkeletonTest : ForecastRealityCheckAcceptanceTest
    {
        private const int LevelsPerCheck = 4;

        /// <summary>
        /// The one scenario that runs the shipped forecast engine end to end. Everything else scripts the
        /// forecast so it can say which levels held; this one proves the real engine's answer is read the
        /// same way.
        /// </summary>
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

        private int GivenOceanExplorerFinishingWorkEveryDay()
        {
            var team = GivenATeam("Ocean Explorer", 30);
            GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private void GivenTheForecastIsWorkedOutAsShipped() => Forecasts.RunAsShipped();

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

        private void ThenNothingAboutTheTeamChanged(int teamId, RealityCheckSeedSteps.TeamAsStored before)
        {
            Assert.That(WhatIsStoredFor(teamId), Is.EqualTo(before),
                "the check reads; it never writes a Team setting, a Work Item or anything else");
        }
    }
}
