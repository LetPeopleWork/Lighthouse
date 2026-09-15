using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 06 / ADO #5843: the warnings,
    /// without reading the log. Driving ports: the System-Administrator-guarded recent-problems read on the
    /// existing logs controller, the level control beside it, and the scheduled refresh that produces the
    /// problems in the first place. US-06, AC-06.1 … AC-06.7.
    ///
    /// Every scenario here is driven by a real refresh against a connector that will not answer, which is
    /// AC-06.7's whole point: a test that logs a line and then reads it back proves the buffer works and
    /// proves nothing about the question this slice exists to answer — whether filtering by severity leaves
    /// an operator with something they can act on.
    ///
    /// Two acceptance criteria are answered elsewhere, neither silently:
    ///
    /// AC-06.5 (the section says plainly that this is what has happened since the instance started, and is
    /// not a complete history) and AC-06.6 (with nothing captured, the section says so rather than
    /// rendering empty) are promises about words on a screen and live in the popover's own specs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-06")]
    public partial class Slice06TheWarningsWithoutTheLogTest
    {
        // @walking_skeleton @driving_port @real-io @error @AC-06.1 @AC-06.7
        // The slice in one line. Today the only record that this team stopped refreshing is a line inside a
        // rolling text file, which an operator sees only if they decide to go and open it — and the whole
        // point of the popover is that noticing should not depend on that decision.
        [Test]
        public async Task A_refresh_that_broke_is_waiting_in_the_popover_instead_of_in_the_log_file()
        {
            var team = GivenATeamCalled("Lagunitas");
            GivenTheTrackerTurnsEveryRefreshAway();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenRecentProblemsSaysWhatBrokeFor(team);
        }

        // @driving_port @real-io @error @AC-06.1 @AC-06.7
        // The one above says the failure is listed; this one says the row is worth reading. A section that
        // lists a failure without saying when, how seriously, from where or what broke is a wall of
        // sentences, which is the outcome this slice's hypothesis says would disprove it.
        [Test]
        public async Task A_recent_problem_says_when_it_happened_how_serious_it_is_where_it_came_from_and_what_broke()
        {
            var team = GivenATeamCalled("Sierra Nevada");
            GivenTheTrackerTurnsEveryRefreshAway();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatProblemSaysWhenHowSeriousWhereFromAndWhatBroke(team);
        }

        // @driving_port @real-io @AC-06.1
        // The negative control, and the one that keeps the section honest. Every refresh writes its own
        // summary line, and a section that showed those would be a wall of text within the hour — which is
        // precisely the outcome the level filter is being tested for. A refresh that worked contributes
        // nothing, and the positive control is that the refresh really ran.
        [Test]
        public async Task A_refresh_that_worked_leaves_nothing_behind_for_an_operator_to_worry_about()
        {
            var team = GivenATeamCalled("Anchor Steam");

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenTheRefreshOfThatTeamReallyRan(team);
            await ThenRecentProblemsSaysNothingAbout(team);
        }

        // @driving_port @real-io @error @AC-06.1
        // A short list is read from the top. Two failures that arrived in the wrong order would have an
        // operator chasing the one that has already been fixed.
        [Test]
        public async Task The_problem_that_just_happened_is_the_one_an_operator_reads_first()
        {
            var first = GivenATeamCalled("Ballast Point");
            var second = GivenATeamCalled("Stone");
            GivenTheTrackerTurnsEveryRefreshAway();

            await WhenTheScheduledRefreshOfThatTeamRuns(first);
            await WhenTheScheduledRefreshOfThatTeamRuns(second);

            await ThenTheProblemAboutIsReadBeforeTheProblemAbout(newer: second, older: first);
        }

        // @driving_port @real-io @error @AC-06.2
        // An instance that has been up for a month must not be holding a month of failures in memory. What
        // makes room is the oldest going first: dropping the newest instead would leave the section
        // permanently showing whatever went wrong first and never what is going wrong now.
        [Test]
        public async Task The_oldest_problem_makes_way_for_the_newest_once_there_is_no_more_room()
        {
            var teams = GivenMoreTeamsThanTheInstanceHasRoomToRememberProblemsFor();
            GivenTheTrackerTurnsEveryRefreshAway();

            await WhenTheScheduledRefreshOfEachOfThoseTeamsRuns(teams);

            await ThenRecentProblemsSaysNothingAbout(teams[0]);
            await ThenRecentProblemsSaysWhatBrokeFor(teams[^1]);
        }

        // @driving_port @real-io @error @AC-06.3
        // The level control the log viewer already offers governs this section too, and it takes effect on
        // what is being captured right now. An operator who turns the noise down and has to restart the
        // instance for it to mean anything has not been given a control.
        [Test]
        public async Task Telling_the_instance_to_report_only_the_very_worst_takes_effect_on_the_spot()
        {
            var team = GivenATeamCalled("Firestone Walker");
            GivenTheTrackerTurnsEveryRefreshAway();

            await WhenTheOperatorTellsTheInstanceToReportOnlyTheVeryWorst();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenRecentProblemsSaysNothingAbout(team);
        }

        // @driving_port @real-io @error @AC-06.3
        // The other direction, and the control on the one above: without it, a section that has quietly
        // stopped recording anything at all satisfies that scenario perfectly.
        [Test]
        public async Task Telling_it_to_report_problems_again_takes_effect_on_the_spot_too()
        {
            var silenced = GivenATeamCalled("Russian River");
            var afterwards = GivenATeamCalled("Deschutes");
            GivenTheTrackerTurnsEveryRefreshAway();
            await WhenTheOperatorTellsTheInstanceToReportOnlyTheVeryWorst();
            await WhenTheScheduledRefreshOfThatTeamRuns(silenced);

            await WhenTheOperatorTellsTheInstanceToReportProblemsAgain();
            await WhenTheScheduledRefreshOfThatTeamRuns(afterwards);

            await ThenRecentProblemsSaysWhatBrokeFor(afterwards);
            await ThenRecentProblemsSaysNothingAbout(silenced);
        }

        // @driving_port @real-io @error @AC-06.4
        // The same team names, work-tracking URLs and connector errors the log file carries, which is why
        // the log file has been administrator-only since 2026-08-06. Serving them from a second route
        // would hand out exactly what that guard was put there to withhold.
        // Unlike the rest of this fixture, this one needed nothing built to pass: it asserts a guard the
        // controller already carries, so a failure here would mean the attribute had gone missing.
        [Test]
        public async Task Recent_problems_are_not_handed_to_somebody_who_may_not_read_the_log_either()
        {
            await ThenRecentProblemsIsRefusedToWhoeverTheLogFileIsRefusedTo();
        }
    }
}
