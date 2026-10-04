using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A Team admin says when the Team refines: one or more weekdays, every so many weeks, and - when that is
    /// more than one - the week it starts from. The tab then names the next Refinement: the first cadence day
    /// strictly after today, today being the instance's calendar day in its own time zone. On a Refinement day
    /// the tab already looks to the following one, because today's session is the moment to top up. A
    /// blackout day does not move a Refinement; holding it is the Team's call. Without a cadence the tab still
    /// lists its Work Items and takes votes, it just names no date.
    ///
    /// Driving ports: the Team settings write and read, and the Refinement tab's read. Step definitions live
    /// in Slice04RefinementCadenceSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-04")]
    public partial class Slice04RefinementCadenceTest : RefinementNeedAcceptanceTest
    {
        // --- The cadence setting ---

        // @driving_port @real-io @us-04 @slice-04 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_sets_the_Refinement_cadence_and_it_reads_back()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();

            await TheAdminHasSetTheCadence(gravity, [Tuesday, Thursday], 2, "2026-10-05");

            ThenTheCadenceIs(await ReadTheTeamSettings(gravity), new CadenceReading("Tuesday,Thursday", 2, "2026-10-05"));
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:bounded-change
        // A starting week can be named by any of its days; it is kept as the week, from its Monday.
        [Test]
        public async Task A_starting_week_named_by_any_of_its_days_is_kept_as_that_week()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();

            await TheAdminHasSetTheCadence(gravity, [Tuesday], 2, "2026-10-07");

            ThenTheCadenceIs(await ReadTheTeamSettings(gravity), new CadenceReading(Tuesday, 2, "2026-10-05"));
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:bounded-change
        [Test]
        public async Task The_same_weekday_named_twice_is_one_Refinement_day()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();

            await TheAdminHasSetTheCadence(gravity, [Thursday, Thursday], 1, null);

            ThenTheCadenceIs(await ReadTheTeamSettings(gravity), new CadenceReading(Thursday, 1, null));
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:unbounded-preservation
        // Without a starting week "every second Tuesday" names no Tuesday in particular.
        [Test]
        public async Task Every_few_weeks_without_a_starting_week_is_refused_and_nothing_is_saved()
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();

            using var refused = await WhenTheAdminSavesTheCadence(gravity, ACadence([Tuesday], 2, null));

            await ThenTheSaveIsRefusedAndTheCadenceIsStillThursdaysEveryWeek(refused, gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:unbounded-preservation
        [TestCase(0)]
        [TestCase(-1)]
        public async Task Fewer_than_one_week_between_Refinements_is_refused_and_nothing_is_saved(int intervalWeeks)
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();

            using var refused = await WhenTheAdminSavesTheCadence(gravity, ACadence([Thursday], intervalWeeks, "2026-10-05"));

            await ThenTheSaveIsRefusedAndTheCadenceIsStillThursdaysEveryWeek(refused, gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:unbounded-preservation
        [TestCase("Someday")]
        [TestCase("Thu 8 Oct")]
        public async Task A_Refinement_day_that_is_not_a_weekday_is_refused_and_nothing_is_saved(string notAWeekday)
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();

            using var refused = await WhenTheAdminSavesTheCadence(gravity, ACadence([notAWeekday], 1, null));

            await ThenTheSaveIsRefusedAndTheCadenceIsStillThursdaysEveryWeek(refused, gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about the cadence; its saves must not remove it.
        [TestCase(SaveShape.WithoutTheRefinementSection)]
        [TestCase(SaveShape.RefinementSectionWithoutTheMember)]
        public async Task A_save_that_says_nothing_about_the_cadence_keeps_it(SaveShape shape)
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();

            await WhenTheSettingsAreSaved(gravity, shape);

            ThenTheCadenceIs(await ReadTheTeamSettings(gravity), new CadenceReading(Thursday, 1, null));
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice04)]
        public async Task Clearing_every_Refinement_day_leaves_the_Team_without_a_cadence()
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();

            await TheAdminHasSetTheCadence(gravity, [], 1, null);

            await ThenTheTeamHasNoCadenceAndTheTabNamesNoDate(gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:bounded-change
        // The cadence is a refinement setting; it never makes the Team fetch its Work Items afresh.
        [Test]
        public async Task Setting_the_cadence_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();
            var before = WorkItemsStoredFor(gravity);

            await TheAdminHasSetTheCadence(gravity, [Thursday], 1, null);

            await ThenTheTeamStillHoldsItsWorkItemsAndRefinesOnThursdays(gravity, before);
        }

        // --- The next Refinement ---

        // @driving_port @real-io @us-04 @slice-04 @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:pure-function
        // Thursdays every week. The Thursday itself and the Friday after are the boundaries.
        [TestCase(2026, 10, 2, "2026-10-08", false)]
        [TestCase(2026, 10, 7, "2026-10-08", false)]
        [TestCase(2026, 10, 8, "2026-10-15", true)]
        [TestCase(2026, 10, 9, "2026-10-15", false)]
        public async Task The_next_Refinement_is_the_first_cadence_day_after_today(int year, int month, int day, string nextRefinement, bool isRefinementDay)
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();
            TodayIs(year, month, day);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, nextRefinement, isRefinementDay);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:pure-function
        // Tuesdays every second week, from the week of Monday 5 October 2026.
        [TestCase(2026, 10, 2, "2026-10-06", false)]
        [TestCase(2026, 10, 6, "2026-10-20", true)]
        [TestCase(2026, 10, 14, "2026-10-20", false)]
        public async Task Every_second_week_counts_from_the_starting_week(int year, int month, int day, string nextRefinement, bool isRefinementDay)
        {
            var gravity = await GivenGravityRefinesOnTuesdaysEveryOtherWeekFromTheFifth();
            TodayIs(year, month, day);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, nextRefinement, isRefinementDay);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice04)]
        public async Task No_Refinement_falls_before_the_starting_week()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();
            await TheAdminHasSetTheCadence(gravity, [Tuesday], 2, "2026-10-19");
            TodayIs(2026, 10, 2);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, "2026-10-20", false);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_Team_refining_on_two_weekdays_looks_to_whichever_comes_first()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();
            await TheAdminHasSetTheCadence(gravity, [Tuesday, Thursday], 1, null);
            TodayIs(2026, 10, 8);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, "2026-10-13", true);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:pure-function
        // At 23:30 UTC on Wednesday 7 October it is already Thursday in Zurich - a Refinement day there.
        [TestCase("Europe/Zurich", "2026-10-15", true)]
        [TestCase("UTC", "2026-10-08", false)]
        [Ignore(PendingSlice04)]
        public async Task The_instances_time_zone_decides_which_day_today_is(string timeZone, string nextRefinement, bool isRefinementDay)
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();
            TheInstantIs(new DateTimeOffset(2026, 10, 7, 23, 30, 0, TimeSpan.Zero), timeZone);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, nextRefinement, isRefinementDay);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_Refinement_on_a_blackout_day_keeps_its_date()
        {
            var gravity = await GivenGravityRefinesOnThursdaysEveryWeek();
            await ABlackoutDayOn(new DateOnly(2026, 10, 8));
            TodayIs(2026, 10, 2);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNextRefinementIs(tab, "2026-10-08", false);
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:pure-function
        // Without a cadence the tab is still the place to see and vote; it just names no date.
        [Test]
        [Ignore(PendingSlice04)]
        public async Task Without_a_cadence_the_tab_lists_its_Work_Items_and_names_no_date()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheTabListsAllSixAndNamesNoDate(tab);
        }

        // @driving_port @real-io @us-04 @us-11 @slice-04 @error @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice04)]
        public async Task Without_a_cadence_votes_are_still_taken()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();

            await HasVoted(ABrowserOf(JonasWeber), gravity, ConfigurationManagement, Answer.Yes);

            await ThenTheVoteCountsAndTheTabNamesNoDate(gravity, ConfigurationManagement);
        }
    }
}
