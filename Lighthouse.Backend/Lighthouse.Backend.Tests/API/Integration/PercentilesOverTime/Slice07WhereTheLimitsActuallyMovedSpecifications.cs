using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 07 - the natural process limits a delivery lead needs in order to tell
    /// a genuine shift from one odd week.
    ///
    /// The family sets are stated here as the behaviour they stand for rather than as a reference to
    /// where the product happens to keep them: a team reports five behaviours, a portfolio six, and the
    /// sixth is about the size of deliveries, which a team does not have. Naming the sets here means
    /// dropping one is a failing scenario rather than a capability that quietly goes missing.
    /// </summary>
    public partial class Slice07WhereTheLimitsActuallyMovedTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private static readonly ProcessBehaviorMetricType[] EveryBehaviourATeamReports =
        [
            ProcessBehaviorMetricType.Throughput,
            ProcessBehaviorMetricType.WorkItemAge,
            ProcessBehaviorMetricType.Wip,
            ProcessBehaviorMetricType.CycleTime,
            ProcessBehaviorMetricType.Arrivals,
        ];

        private static readonly ProcessBehaviorMetricType[] EveryBehaviourAPortfolioReports =
        [
            ProcessBehaviorMetricType.Throughput,
            ProcessBehaviorMetricType.WorkItemAge,
            ProcessBehaviorMetricType.Wip,
            ProcessBehaviorMetricType.CycleTime,
            ProcessBehaviorMetricType.Arrivals,
            ProcessBehaviorMetricType.FeatureSize,
        ];

        /// <summary>
        /// How long the lead in the two pinned-stretch scenarios keeps finished work for. Not the
        /// product default of a year, and the gap is the entire scenario rather than colour.
        ///
        /// Those scenarios pin a stretch starting 240 days back and ask about a period 60 to 30 days
        /// back. Keeping finished work for 220 days means that, measured from today, the owner no
        /// longer reaches the stretch it pinned - so limits drawn from it are judged unusable and the
        /// chart comes back empty. Measured from any day in the period under review, the same 220 days
        /// reaches between 280 and 250 days back, and the stretch sits comfortably inside.
        ///
        /// That disagreement between the two possible anchors is the only thing that can tell a chart
        /// judged as of today from one judged as of the day being rebuilt. Widen this back to the
        /// default year "for realism" and both anchors agree the stretch is in reach, both give the
        /// same answer, and the scenarios pass whichever anchor the product uses - which is exactly
        /// how the first version of these two came to assert nothing at all. The relation is not left
        /// to this number on its own either: the guard below reads both settings back off the owner
        /// and fails the scenario the moment they stop disagreeing.
        ///
        /// A lead who narrows how long finished work is kept to below the reach of a stretch they
        /// pinned is also the person this defect bites. The settings screen offers the number, and
        /// shorter than a year is an ordinary choice.
        ///
        /// The owner was last refreshed <see cref="DaysAgoThePinnedStretchOwnerWasLastRefreshed"/> days
        /// back, not today. A refresh deletes whatever finished before the reach, so an owner refreshed
        /// today would no longer hold any of the stretch, and steady limits drawn from it would be a
        /// reading of a store no owner could have. Refreshed 25 days back, what it still holds reaches 245
        /// days back and the stretch at 240 is still there - while every day under review, 60 to 30 days
        /// back, lies on or before that refresh, since days after it are never filled in.
        /// </summary>
        private const int DaysFinishedWorkIsKeptForWhenTheStretchIsOutOfReach = 220;

        private const int DaysAgoThePinnedStretchOwnerWasLastRefreshed = 25;

        /// <summary>
        /// How long the team in the fidelity scenario keeps finished work for, bounded on both sides by
        /// what it has to catch. The watched day's limits are drawn from the thirty days ending on it, 43
        /// days back, so that stretch starts 72 days back: keeping work for fewer than 72 days puts it out of
        /// reach as of today, which is the only way judging the day as of today can give a different answer
        /// from judging it as of itself. And a stretch twice as long starts 58 days before the watched day,
        /// so keeping work for at least 58 days leaves that one in reach as of the day, and working the day
        /// out over the wrong span shows up as the wrong limits rather than as none. The guard in the
        /// scenario checks both through the product's own calculation rather than trusting this arithmetic.
        ///
        /// The team was last refreshed <see cref="DaysAgoTheWatchedTeamWasLastRefreshed"/> days back, and
        /// that bound is just as tight. A refresh deletes whatever finished more than 65 days before it, so
        /// the watched day's stretch, starting 72 days back, is only still held if the refresh was at least
        /// 7 days back - and the twice-as-long stretch, starting 101 days back, only if it was at least 36
        /// days back, which is what keeps that wrong way a wrong reading rather than a missing one. A
        /// refresh any earlier than the watched day, 43 days back, would leave that day unfilled because
        /// nothing after a refresh is. Refreshed 40 days back, the team still holds work to 105 days back.
        ///
        /// Items keep finishing up to today even so. One of the wrong ways the guard compares against is
        /// the stretch ending today, and without them that stretch reads nothing at all instead of a
        /// wrong answer. Nothing the chart fills in reads past the last refresh, so they never reach it.
        /// </summary>
        private const int DaysFinishedWorkIsKeptForWhenTheWatchedDayIsOutOfReachToday = 65;

        private const int DaysAgoTheWatchedTeamWasLastRefreshed = 40;

        /// <summary>
        /// How far back from a day its limits reach when no stretch is pinned: the team's thirty days of
        /// throughput history, the day itself included. Not taken on trust either - the guard first checks
        /// that this stretch gives back exactly what the recorder wrote.
        /// </summary>
        private const int DaysTheLimitsReachBack = 29;

        /// <summary>A reading of all zeros is the chart saying it could draw nothing from that stretch.</summary>
        private static readonly (int Unpl, int Average, int Lnpl) NoReading = (0, 0, 0);

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenAPortfolioStillBeingRefreshed() => SeedPortfolioObservedUntil(TodayDay);

        /// <summary>
        /// An owner whose last refresh was on <paramref name="lastRefreshedOn"/> rather than today. That
        /// refresh deleted every finished item older than the owner keeps, so what the store still holds
        /// reaches back to <paramref name="lastRefreshedOn"/> less <paramref name="days"/> - and a scenario
        /// that reads a stretch older than that is reading work the owner could not still have.
        /// </summary>
        private int GivenATeamLastRefreshedOnThatKeepsFinishedWorkFor(DateOnly lastRefreshedOn, int days)
            => SeedTeamObservedUntil(lastRefreshedOn, doneItemsCutoffDays: days);

        private int GivenAPortfolioLastRefreshedOnThatKeepsFinishedWorkFor(DateOnly lastRefreshedOn, int days)
            => SeedPortfolioObservedUntil(lastRefreshedOn, doneItemsCutoffDays: days);

        private int GivenATeamLastObservedOn(DateOnly lastObservedOn) => SeedTeamObservedUntil(lastObservedOn);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        /// <summary>
        /// Every delivery carries a size, and no two consecutive ones carry the same size. Both halves
        /// are load-bearing for this slice: a portfolio whose deliveries have no size has no size
        /// process to report and the size chart correctly says so, and a run of identically sized
        /// deliveries has a centre but no spread, which is not the chart a lead reads "did the limits
        /// move" off.
        /// </summary>
        private void GivenThePortfolioFinishedOneDeliveryADayFrom(int portfolioId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedSizedDeliveryFinishedOn(portfolioId, $"{portfolioId}-{day:yyyyMMdd}", day.AddDays(-4), day, HowBigTheDeliveryFinishedOnWas(day));
            }
        }

        /// <summary>
        /// A team that has been getting faster: quiet at the weekend, one more item a day than it was
        /// managing a month before. Kept apart from the steady seeder above rather than replacing it,
        /// because several scenarios turn on the flat series - one of them specifically on the
        /// collapsed band a stretch with nothing in it produces.
        /// </summary>
        private void GivenTheTeamFinishedMoreEachMonthFrom(int teamId, DateOnly from, DateOnly to)
            => SeedItemsFinishedOn(teamId, from, to, HowManyItemsTheTeamFinishedOn);

        private void GivenThePortfolioFinishedMoreDeliveriesEachMonthFrom(int portfolioId, DateOnly from, DateOnly to)
            => SeedSizedDeliveriesFinishedOn(portfolioId, from, to, HowManyDeliveriesThePortfolioFinishedOn, HowBigTheDeliveryFinishedOnWas);

        /// <summary>
        /// How much work the delivery that finished on <paramref name="day"/> was broken down into.
        /// Deliveries vary in size; the arithmetic only has to make them vary in a way that does not
        /// depend on when the suite runs.
        /// </summary>
        private static int HowBigTheDeliveryFinishedOnWas(DateOnly day) => 3 + (day.DayNumber % 5);

        /// <summary>
        /// An owner that is getting faster: nothing finished at the weekend, and on a weekday one more
        /// than it was managing a month earlier.
        ///
        /// Both halves are load-bearing, and for different reasons. The quiet weekend gives the band a
        /// width - limits are drawn from how much the count moves day to day, so an owner that finishes
        /// the same amount every day has a centre and no spread at all, and every day's limits come out
        /// identical no matter which stretch they were drawn from. The speeding up is what makes a
        /// rolling stretch and a fixed one give different answers: a stretch that follows the window
        /// climbs with the owner, a stretch that was pinned does not.
        ///
        /// Without both, the two scenarios that read "did the limits move" agree with each other
        /// instead of telling the reader apart, which is what one item a day did here before.
        /// </summary>
        private static int HowMuchAnOwnerThatIsSpeedingUpFinishedOn(DateOnly day, int atTheStart)
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                return 0;
            }

            var daysAgo = TodayDay.DayNumber - day.DayNumber;

            return atTheStart + (Math.Max(0, DaysAgoTheOwnerStartedSpeedingUp - daysAgo) / DaysBetweenFinishingOneMore);
        }

        /// <summary>
        /// How far back the speeding up began, and how long the owner takes to finish one more a day.
        /// A month between steps is what carries the average of a thirty-day stretch past a whole item
        /// within the period these scenarios review - a gentler climb rounds to the same limits on
        /// every day of it, and the assertion that they moved would be vacuous again in a new costume.
        /// Checked before it was relied on, not assumed.
        /// </summary>
        private const int DaysAgoTheOwnerStartedSpeedingUp = 300;

        private const int DaysBetweenFinishingOneMore = 30;

        /// <summary>A team finishes more items than a portfolio finishes deliveries, so it starts higher.</summary>
        private static int HowManyItemsTheTeamFinishedOn(DateOnly day) => HowMuchAnOwnerThatIsSpeedingUpFinishedOn(day, atTheStart: 2);

        private static int HowManyDeliveriesThePortfolioFinishedOn(DateOnly day) => HowMuchAnOwnerThatIsSpeedingUpFinishedOn(day, atTheStart: 1);

        /// <summary>
        /// The lead has fixed the stretch the limits are drawn from, rather than letting it follow the
        /// window. A fixed reference period does not move, so the limits drawn from it should not move
        /// either - and that is a reading, not an absence.
        /// </summary>
        private void GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(int teamId, DateOnly from, DateOnly to)
            => PinTheTeamsBaselineTo(teamId, from, to);

        private void GivenThePortfolioPinnedTheStretchItsLimitsAreDrawnFrom(int portfolioId, DateOnly from, DateOnly to)
            => PinThePortfoliosBaselineTo(portfolioId, from, to);

        /// <summary>
        /// The arrangement the two pinned-stretch scenarios turn on, stated as something the scenario
        /// checks rather than left to the numbers it happened to seed: the pinned stretch has fallen
        /// outside what the owner still keeps when that is measured from today, and is still inside it
        /// when measured from any day in the period under review.
        ///
        /// Only while both hold do the two candidate anchors give different answers, and only then can
        /// the scenario tell which one the product used. Seed a wider retention window and the scenario
        /// silently stops testing anything; this fails instead, and says which half went.
        ///
        /// It also checks that the arrangement could exist at all: that the owner's last refresh, which
        /// deleted everything older than it keeps, left the stretch in the store, and that the period
        /// under review ends no later than that refresh.
        /// </summary>
        private void GivenTheTeamsStretchIsOutOfReachTodayAndInReachOverThePeriod(int teamId, DateOnly lastDayUnderReview)
        {
            var (stretchStartsOn, daysFinishedWorkIsKeptFor, lastRefreshedOn) = HowTheTeamsStretchAndRetentionStand(teamId);

            TheStretchIsOutOfReachTodayAndInReachAsOf(stretchStartsOn, daysFinishedWorkIsKeptFor, lastDayUnderReview, lastRefreshedOn);
        }

        private void GivenThePortfoliosStretchIsOutOfReachTodayAndInReachOverThePeriod(int portfolioId, DateOnly lastDayUnderReview)
        {
            var (stretchStartsOn, daysFinishedWorkIsKeptFor, lastRefreshedOn) = HowThePortfoliosStretchAndRetentionStand(portfolioId);

            TheStretchIsOutOfReachTodayAndInReachAsOf(stretchStartsOn, daysFinishedWorkIsKeptFor, lastDayUnderReview, lastRefreshedOn);
        }

        private static void TheStretchIsOutOfReachTodayAndInReachAsOf(
            DateOnly stretchStartsOn, int daysFinishedWorkIsKeptFor, DateOnly lastDayUnderReview, DateOnly lastRefreshedOn)
        {
            var reachFromToday = TodayDay.AddDays(-daysFinishedWorkIsKeptFor);
            var reachFromTheLastDayUnderReview = lastDayUnderReview.AddDays(-daysFinishedWorkIsKeptFor);
            var reachFromTheLastRefresh = lastRefreshedOn.AddDays(-daysFinishedWorkIsKeptFor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stretchStartsOn, Is.LessThan(reachFromToday),
                    $"Measured from today the owner still keeps finished work back to {reachFromToday:yyyy-MM-dd}, which reaches " +
                    $"the stretch pinned at {stretchStartsOn:yyyy-MM-dd}. Judged from today or judged from the day being rebuilt, " +
                    "the answer is then the same one, and this scenario passes whichever the product asks - which is the failure " +
                    "mode it exists to catch, not a detail of the seed.");

                Assert.That(stretchStartsOn, Is.GreaterThanOrEqualTo(reachFromTheLastDayUnderReview),
                    $"Measured from {lastDayUnderReview:yyyy-MM-dd}, the last day under review, the owner keeps finished work only " +
                    $"back to {reachFromTheLastDayUnderReview:yyyy-MM-dd}, which does not reach the stretch pinned at " +
                    $"{stretchStartsOn:yyyy-MM-dd}. The stretch is then out of reach from every angle, reporting nothing is the " +
                    "honest answer, and steady limits are not what should come back.");

                Assert.That(stretchStartsOn, Is.GreaterThanOrEqualTo(reachFromTheLastRefresh),
                    $"The owner was last refreshed on {lastRefreshedOn:yyyy-MM-dd}, and that refresh deleted every finished item " +
                    $"older than {reachFromTheLastRefresh:yyyy-MM-dd} - which includes the start of the stretch pinned at " +
                    $"{stretchStartsOn:yyyy-MM-dd}. No owner could still hold the work that stretch is drawn from, so steady " +
                    "limits read from it describe a store nobody has.");

                Assert.That(lastDayUnderReview, Is.LessThanOrEqualTo(lastRefreshedOn),
                    $"Nothing was observed after {lastRefreshedOn:yyyy-MM-dd}, so days after it are never filled in, and the " +
                    $"period under review, which runs to {lastDayUnderReview:yyyy-MM-dd}, would be partly empty for that reason alone.");
            }
        }

        /// <summary>
        /// A reference stretch that reaches back further than the owner keeps finished work. Nothing can
        /// be drawn from a period the owner no longer holds, so those days have no limits to report.
        /// </summary>
        private void GivenTheTeamPinnedAStretchItNoLongerKeepsAnyWorkFrom(int teamId)
            => PinTheTeamsBaselineTo(teamId, TodayDay.AddDays(-900), TodayDay.AddDays(-800));

        private async Task<RecordedLimitDay> GivenALimitDayTheRecorderWroteAndThenLost(int teamId, ProcessBehaviorMetricType behaviour, DateOnly day)
        {
            TheInstanceMovesOnTo(day);
            await TheTeamsRefreshCompletes(teamId);

            var asRecorded = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .SingleOrDefault(held => held.RecordedAt == day);

            Assert.That(asRecorded, Is.Not.Default,
                $"The recorder wrote no {behaviour} limits on {day:yyyy-MM-dd}, so there is nothing to hold reconstruction to.");

            TheRecordOfThatLimitDayIsLost(teamId, OwnerType.Team, behaviour, day);
            TheInstanceMovesOnTo(TodayDay);

            return asRecorded;
        }

        /// <summary>
        /// What makes the fidelity scenario able to fail, checked rather than assumed. Worked out over its
        /// own stretch and judged as of itself, the watched day must give back exactly what the recorder
        /// wrote - otherwise this guard has the wrong idea of the stretch, and nothing it says about the
        /// others counts. Worked out any of the likely wrong ways, it must not.
        ///
        /// The limits are whole numbers, so moving a stretch by a day often moves them by less than one and
        /// they round to the same thing. Most days in the period are like that even with this team, which
        /// is why the watched day sits where it does and why this is checked and not left to the seed.
        ///
        /// Judging the day as of today while keeping its own stretch is listed apart from the rest: the day
        /// being judged only decides whether the stretch is still in reach, never what it reads, so the one
        /// way that mistake can show is by the day coming back with no limits at all.
        /// </summary>
        private void GivenThatDayReadsDifferentlyWhenWorkedOutAnyOtherWay(int teamId, RecordedLimitDay asWatched)
        {
            var day = asWatched.RecordedAt;
            var watched = (asWatched.Unpl, asWatched.Average, asWatched.Lnpl);

            var overItsOwnStretch = ThroughputLimitsWorkedOutOver(teamId, day.AddDays(-DaysTheLimitsReachBack), day, day);
            var judgedAsOfToday = ThroughputLimitsWorkedOutOver(teamId, day.AddDays(-DaysTheLimitsReachBack), day, TodayDay);

            var overAWrongStretch = new Dictionary<string, (int Unpl, int Average, int Lnpl)>
            {
                ["the stretch ending today, judged as of today"] =
                    ThroughputLimitsWorkedOutOver(teamId, TodayDay.AddDays(-DaysTheLimitsReachBack), TodayDay, TodayDay),
                ["the stretch ending the day before"] =
                    ThroughputLimitsWorkedOutOver(teamId, day.AddDays(-DaysTheLimitsReachBack - 1), day.AddDays(-1), day.AddDays(-1)),
                ["the stretch ending the day after"] =
                    ThroughputLimitsWorkedOutOver(teamId, day.AddDays(1 - DaysTheLimitsReachBack), day.AddDays(1), day.AddDays(1)),
                ["a stretch reaching twice as far back"] =
                    ThroughputLimitsWorkedOutOver(teamId, day.AddDays(-2 * DaysTheLimitsReachBack), day, day),
            };

            var thatWouldGoUnnoticed = overAWrongStretch
                .Where(reading => reading.Value == watched || reading.Value == NoReading)
                .Select(reading => $"{reading.Key}: {reading.Value}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(overItsOwnStretch, Is.EqualTo(watched),
                    $"Worked out over the {DaysTheLimitsReachBack + 1} days ending on {day:yyyy-MM-dd} and judged as of that day, " +
                    "the day does not give back what the recorder wrote, so the stretch this guard compares against is not the " +
                    "one the recorder used and the readings below prove nothing.");

                Assert.That(judgedAsOfToday, Is.Not.EqualTo(watched),
                    $"Judged as of today, the stretch ending on {day:yyyy-MM-dd} is still in reach of what the team keeps, so it " +
                    "reads the same as when it was judged as of itself, and the scenario passes whichever day the product judges by.");

                Assert.That(thatWouldGoUnnoticed, Is.Empty,
                    $"Worked out these ways, {day:yyyy-MM-dd} reads {watched} again or reads nothing at all. The first passes the " +
                    "scenario whatever the product does; the second fails it on a missing day rather than a wrong one.");
            }
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensTheTeamLimits(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, behaviour, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensThePortfolioLimits(int portfolioId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
            => ReadPortfolioLimitTrend(portfolioId, behaviour, from, to);

        private async Task WhenTheDeliveryLeadTogglesEveryTeamBehaviour(int teamId, DateOnly from, DateOnly to)
        {
            foreach (var behaviour in EveryBehaviourATeamReports)
            {
                await ReadTeamLimitTrend(teamId, behaviour, from, to);
            }
        }

        private async Task WhenTheDeliveryLeadTogglesEveryPortfolioBehaviour(int portfolioId, DateOnly from, DateOnly to)
        {
            foreach (var behaviour in EveryBehaviourAPortfolioReports)
            {
                await ReadPortfolioLimitTrend(portfolioId, behaviour, from, to);
            }
        }

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenEveryBehaviourTheTeamReportsGotItsLimits(int teamId)
        {
            Assert.That(LimitFamiliesHeldFor(teamId, OwnerType.Team), Is.EquivalentTo(EveryBehaviourATeamReports),
                "A team reports five behaviours when it records today, so it must fill in all five for past days too. " +
                "Filling in four of them is a capability that goes missing without anyone noticing.");
        }

        private void ThenEveryBehaviourThePortfolioReportsGotItsLimits(int portfolioId)
        {
            Assert.That(LimitFamiliesHeldFor(portfolioId, OwnerType.Portfolio), Is.EquivalentTo(EveryBehaviourAPortfolioReports),
                "A portfolio reports six behaviours when it records today, so it must fill in all six for past days too.");
        }

        /// <summary>
        /// Only the first half turns on what was seeded: seed deliveries with no size and it fails, seed
        /// them with one and it passes. The second half cannot be made to fail by any arrangement - a
        /// team's family set is built without a delivery-size reader because there is no team-side
        /// method to read one from, so no amount of seeded data produces the row it looks for. It is
        /// kept because it still fails on the one edit that would break the promise - a delivery-size
        /// reader added to the team's family set - and it says in the scenario's own words what that
        /// set is for. Read it as the statement, not as the proof; the proof is the recorder's
        /// family-set assertion, which names the whole set rather than one absent member.
        /// </summary>
        private void ThenDeliverySizeIsReportedForThePortfolioAndNotForTheTeam(int teamId, int portfolioId)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(LimitDaysHeldFor(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.FeatureSize), Is.Not.Empty,
                    "How big deliveries are getting is a portfolio question, and the portfolio must answer it for past days.");
                Assert.That(LimitDaysHeldFor(teamId, OwnerType.Team, ProcessBehaviorMetricType.FeatureSize), Is.Empty,
                    "A team has no deliveries to size, so filling in a size history for one would invent a behaviour it does not have.");
            }
        }

        private void ThenNoLimitsAreReportedFor(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var present = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Select(day => day.RecordedAt)
                .Where(day => day >= from && day <= to)
                .ToList();

            Assert.That(present, Is.Empty,
                $"There is nothing to draw {behaviour} limits from over {from:yyyy-MM-dd}..{to:yyyy-MM-dd}, and three flat lines " +
                $"pinned at zero would describe a process nobody had. Written anyway: {string.Join(", ", present)}.");
        }

        private void ThenNoDayReportsACollapsedBand(int teamId, ProcessBehaviorMetricType behaviour)
        {
            // The lower limit is deliberately not part of this: a busy process routinely reports a lower
            // limit of zero, because the calculation will not go below it for counts that cannot go
            // negative. It is the average and the upper limit both being zero that means "no process".
            var collapsed = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day is { Average: 0, Unpl: 0 })
                .Select(day => day.RecordedAt)
                .ToList();

            Assert.That(collapsed, Is.Empty,
                $"A band with no width and no centre is not a process anyone had. Written anyway: {string.Join(", ", collapsed)}.");
        }

        private void ThenTheLimitsHoldSteadyAcross(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var reported = LimitDaysHeldFor(ownerId, ownerType, behaviour)
                .Where(day => day.RecordedAt >= from && day.RecordedAt <= to)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reported, Is.Not.Empty,
                    "A fixed reference stretch still describes a process. Reporting nothing at all reads as \"your data did not " +
                    "support it\", which is a different and false statement.");
                Assert.That(reported.Select(day => (day.Unpl, day.Average, day.Lnpl)).Distinct().Count(), Is.EqualTo(1),
                    "The stretch the limits are drawn from was fixed, so the limits drawn from it cannot move day to day.");
            }
        }

        /// <summary>
        /// The second assertion is the one that earns the name. Until it was added this checked only
        /// that days were present, so it agreed with its sibling - the fixed-stretch reading - instead
        /// of telling the two apart, and would have passed against an implementation that drew every
        /// day from one fixed window. That it can fail is shown by pinning a stretch for this same
        /// team, which collapses the period to a single triple.
        /// </summary>
        private void ThenTheLimitsAreFreeToMoveAcross(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var reported = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day.RecordedAt >= from && day.RecordedAt <= to)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reported, Is.Not.Empty,
                    "Without a fixed reference stretch each day draws its limits from its own recent history, so the days must be " +
                    "there to be compared at all.");

                Assert.That(reported.Select(day => (day.Unpl, day.Average, day.Lnpl)).Distinct().Count(), Is.GreaterThan(1),
                    "The team never fixed the stretch its limits are drawn from, so each day draws them from its own recent " +
                    "history - and this team was finishing more every month, so the lines have to have moved over the period. " +
                    "One triple repeated across every day of it is the reading a fixed stretch gives, which is a different " +
                    "scenario and a different answer. Reported: " +
                    string.Join(", ", reported.Select(day => $"{day.RecordedAt:yyyy-MM-dd} {day.Lnpl}/{day.Average}/{day.Unpl}")));
            }
        }

        private void ThenTheLimitsCameBackTheSameAsWhenTheyWereWatched(int teamId, ProcessBehaviorMetricType behaviour, RecordedLimitDay asWatched)
        {
            var held = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day.RecordedAt == asWatched.RecordedAt)
                .ToList();

            Assert.That(held, Is.EqualTo(new[] { asWatched }),
                "Limits worked out afterwards must equal the limits that were watched, or the three lines mix two different " +
                "readings while claiming to be one history.");
        }

        private void ThenTheLimitsStopOn(int teamId, ProcessBehaviorMetricType behaviour, DateOnly lastObservedOn)
        {
            var beyond = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Select(day => day.RecordedAt)
                .Where(day => day > lastObservedOn)
                .ToList();

            Assert.That(beyond, Is.Empty,
                $"Nothing has been observed since {lastObservedOn:yyyy-MM-dd}, so limits past it would describe a period nobody " +
                $"watched. Written anyway: {string.Join(", ", beyond)}.");
        }

        private void ThenTheLimitsAreUnchangedSince(int teamId, ProcessBehaviorMetricType behaviour, IReadOnlyList<RecordedLimitDay> before)
        {
            Assert.That(LimitDaysHeldFor(teamId, OwnerType.Team, behaviour), Is.EqualTo(before),
                "Everything in the period already had limits, so a second look must cost nothing and change nothing.");
        }

        private IReadOnlyList<RecordedLimitDay> LimitsReportedFor(int teamId, ProcessBehaviorMetricType behaviour)
            => LimitDaysHeldFor(teamId, OwnerType.Team, behaviour);
    }
}
