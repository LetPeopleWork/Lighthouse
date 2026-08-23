using Lighthouse.Backend.Models.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Acceptance scenarios - Slice 02: the row says which Features this one is waiting on, names each of
    /// them, and says what stands against any Lighthouse cannot act on. Driving port: the Features list
    /// every screen reads.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4365-dependencies")]
    [Category("slice-02")]
    public partial class Slice02DependencyDetailTest
    {
        private static readonly string[] TheSearchIndex = ["F-1"];
        private static readonly string[] TheSearchIndexAndOneNobodyHolds = ["F-1", "F-404"];
        private static readonly string[] TheSearchIndexAndTheWarehouse = ["F-1", "F-9"];
        private static readonly string[] TheCatalogue = ["F-3"];
        private static readonly string[] ItsOwnSelf = ["F-3"];

        private static readonly NotHonouredReason[] TheReasonsThisEpicCanProduce =
        [
            NotHonouredReason.OutsideThisPortfolio,
            NotHonouredReason.InALoop,
            NotHonouredReason.BlockerCannotBeForecast,
            NotHonouredReason.IgnoredByPortfolio,

            // Added by the Epic that lets a dependency change a date, for a wait nothing but a premium
            // licence stands against. Listed here because this is the set a reader meets.
            NotHonouredReason.NotLicensed,
        ];

        // @driving_adapter @us-02 - "Opening the list of Features one is waiting on". The reader finds what
        // a Feature waits on on the row itself, named and linked, rather than by going somewhere for it.
        [Test]
        public async Task A_row_names_the_features_it_waits_on_and_leads_to_each_of_them()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").WaitsOn("F-1");
            ThenTheRowFor(rows, "F-3").Entry("F-1").Names("Rebuild the search index");
            ThenTheRowFor(rows, "F-3").Entry("F-1").LeadsTo("https://tracker.example/F-1");
            ThenTheRowFor(rows, "F-3").Entry("F-1").SaysItCameFromTheTrackersOwnLink();
        }

        // A link naming something this instance does not hold cannot be named to a reader, so it is not
        // there. Storing it and showing nothing is the honest pair: the day that Feature is imported, the
        // link starts naming it on its own.
        [Test]
        public async Task A_link_naming_nothing_lighthouse_holds_is_not_on_the_row()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndOneNobodyHolds));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").WaitsOn("F-1");
        }

        [Test]
        public async Task A_feature_waiting_on_nothing_says_so_with_an_empty_list()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(platform, AFeatureTheTrackerHolds("F-3", "Publish the partner catalogue"));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").WaitsOn();
        }

        // @driving_adapter @us-02 - "An entry Lighthouse cannot act on says so, in words the reader
        // already uses". Both halves are one scenario on purpose: a reason on every entry would be as
        // useless as a reason on none, so what carries one and what does not have to be read together.
        [Test]
        public async Task An_entry_lighthouse_cannot_act_on_carries_the_reason_and_one_it_can_act_on_carries_none()
        {
            var platform = GivenAPortfolio("Platform");
            var warehouse = GivenAPortfolio("Warehouse");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndTheWarehouse));
            await GivenAnotherPortfolioRefreshed(warehouse, AFeatureTheTrackerHolds("F-9", "Warehouse sync"));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-9").CannotBeActedOnBecause("OutsideThisPortfolio");
            ThenTheRowFor(rows, "F-3").Entry("F-1").CarriesNoReasonAtAll();
        }

        // A caller meeting a reason it has never heard of has to guess, and the guess this feature exists
        // to prevent is "probably fine". Asserted against the set itself rather than against a list of
        // strings a scenario carries, which would drift the moment somebody adds a value.
        [Test]
        public void The_reasons_this_epic_can_produce_are_a_closed_set()
        {
            ThenTheReasonsAreExactly(TheReasonsThisEpicCanProduce);
        }

        // @error @us-02 - "A Feature the reader may not see is named as withheld, never quietly dropped".
        // A shorter list is one the reader has no way of telling is short.
        [Test]
        public async Task A_feature_the_reader_may_not_see_is_withheld_rather_than_left_out()
        {
            var platform = GivenAPortfolio("Platform");
            var warehouse = GivenAPortfolio("Warehouse");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndTheWarehouse));
            await GivenAnotherPortfolioRefreshed(warehouse, AFeatureTheTrackerHolds("F-9", "Warehouse sync"));

            var rows = await WhenAReaderOfOnlyOnePortfolioOpensTheFeaturesView(platform);

            ThenExactlyOneEntryIsWithheld(rows, "F-3");
            ThenTheWithheldEntryDisclosesNothingButThatItExists(rows, "F-3");
            ThenTheRowFor(rows, "F-3").WaitsOnThisMany(2);
        }

        // @rbac @us-02 - "A reader who may not change anything sees the same list and is offered no
        // action". Nothing here is offered to anybody, so the readable half is the whole difference a
        // permission could make, and it must make none.
        [Test]
        public async Task A_reader_who_may_not_change_anything_is_shown_the_same_list()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var asSomeoneWhoMayChangeIt = await WhenTheDeliveryLeadOpensTheFeaturesView();
            var asSomeoneWhoMayOnlyRead = await WhenAReaderOfOnlyOnePortfolioOpensTheFeaturesView(platform);

            ThenBothReadersWereShownTheSame(asSomeoneWhoMayChangeIt, asSomeoneWhoMayOnlyRead, "F-3");
        }

        // Lighthouse never records a dependency of its own, so "may this reader change one?" is not a
        // permission question here - there is nothing to permit. Asserted over what the API actually
        // exposes, because a route that appeared later would answer the question by existing.
        [Test]
        public void No_route_anywhere_adds_removes_or_suppresses_a_dependency()
        {
            ThenNothingInTheApiWritesADependency();
        }

        // @error @us-03 - "Waiting on a Feature outside the Portfolio raises a warning that names it".
        [Test]
        public async Task Waiting_on_a_feature_outside_the_portfolio_says_so_on_the_row_and_names_it()
        {
            var platform = GivenAPortfolio("Platform");
            var warehouse = GivenAPortfolio("Warehouse");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndTheWarehouse));
            await GivenAnotherPortfolioRefreshed(warehouse, AFeatureTheTrackerHolds("F-9", "Warehouse sync"));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-9").CannotBeActedOnBecause("OutsideThisPortfolio");
            ThenTheRowFor(rows, "F-3").Entry("F-9").Names("Warehouse sync");
            ThenNoEntryCarriesASentenceNobodyCanRename(rows);
        }

        // @us-03 - a Portfolio's page and a Team's page ask for the Features they hold rather than for the
        // whole list, and they carry the same warnings column. One dependency reading as a problem on one
        // screen and as fine on another is the disagreement this whole feature exists to prevent, so what
        // is said about it cannot depend on which screen asked.
        [Test]
        public async Task Asking_for_only_some_features_says_the_same_about_them_as_asking_for_all()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));
            GivenTheTeamBehindItHasNoMeasuredDelivery("F-1");

            var everything = await WhenTheDeliveryLeadOpensTheFeaturesView();
            var onlySomeOfThem = await WhenOnlySomeOfTheFeaturesAreAskedFor("F-3");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheEntriesAsRead(onlySomeOfThem, "F-3"),
                    Is.EqualTo(TheEntriesAsRead(everything, "F-3")),
                    "Asking for one Feature has to say what asking for all of them says about it - including "
                    + "what is wrong with a dependency, which the narrower request could not work out and "
                    + "therefore said nothing about.");
                Assert.That(TheEntriesAsRead(everything, "F-3"), Has.Some.Contains("BlockerCannotBeForecast"),
                    "A row with nothing wrong with it would satisfy the comparison above for free.");
            }
        }

        // Having a dependency is not a problem. A Feature whose dependencies are all sound names them and
        // reports nothing against them.
        [Test]
        public async Task A_feature_whose_dependencies_are_all_sound_reports_nothing_against_them()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-1").HasNothingWrongWithIt();
        }

        // @error @us-03 - "Waiting on a Feature positioned below raises a different warning, and nothing
        // is moved". The order stays the reader's, so the whole rendered order is compared before and
        // after the read.
        [Test]
        public async Task Waiting_on_a_feature_positioned_below_says_so_and_moves_nothing()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenAPortfolioWhereTheOneItWaitsOnComesLast(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex),
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"));
            var theOrderBefore = await GivenTheOrderEveryFeatureIsIn();

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-1").SitsBelowTheFeatureWaitingOnIt();
            ThenTheOrderEveryFeatureIsInIsUnchanged(theOrderBefore, rows);
        }

        // @error @us-03 - "A loop warns on every Feature in it and names the others". Every member is
        // waiting on every other member, so there is no first one to start with.
        [Test]
        public async Task Two_features_waiting_on_each_other_both_say_so()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenAPortfolioWhereTheOneItWaitsOnComesLast(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex),
                AFeatureWaitingOn("F-1", "Rebuild the search index", TheCatalogue));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-1").CannotBeActedOnBecause("InALoop");
            ThenTheRowFor(rows, "F-1").Entry("F-3").CannotBeActedOnBecause("InALoop");
        }

        // The smallest circle there is, and the one the deduplication key was chosen to keep: a Feature
        // that names itself would be indistinguishable from one that names nothing if the key had been
        // the Feature alone.
        [Test]
        public async Task A_feature_waiting_on_itself_says_so_about_itself()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", ItsOwnSelf));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-3").CannotBeActedOnBecause("InALoop");
        }

        // A hundred Features waiting on one another in a circle. The claim is not that a walk over them
        // terminates - the detector's own tests say that - but that a real read of a real payload survives
        // it, which is the only place the whole path is under test at once.
        [Test]
        public async Task A_hundred_features_waiting_on_one_another_are_all_reported()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenAPortfolioWhereTheOneItWaitsOnComesLast(platform, AChainOfFeaturesClosingOnItself(100));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenEveryFeatureInTheChainSaysItIsInACircle(rows, 100);
        }

        // Working out a verdict is reading, and reading writes nothing. A stored verdict would also be a
        // second place the answer lives, which is the one thing this slice exists to avoid.
        [Test]
        public async Task Working_out_the_loop_stores_nothing()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenAPortfolioWhereTheOneItWaitsOnComesLast(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex),
                AFeatureWaitingOn("F-1", "Rebuild the search index", TheCatalogue));
            var everythingRecordedBefore = GivenEverythingRecordedAboutTheDependencies();

            await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenNothingAboutTheDependenciesWasRecorded(everythingRecordedBefore);
        }

        // @edge @us-03 - "A Feature waiting on one whose Team has no measured delivery is told why". The
        // wait has no end anyone can name, which is a different problem from the dependency being unusable.
        [Test]
        public async Task Waiting_on_a_feature_no_one_can_forecast_says_so()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));
            GivenTheTeamBehindItHasNoMeasuredDelivery("F-1");

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-1").CannotBeActedOnBecause("BlockerCannotBeForecast");
        }

        // The exemption the forecast already makes, read here rather than decided again: a Feature with
        // nothing left to do has no forecast because there is nothing to forecast, which is a fact and not
        // a gap. Reporting it would send the reader to chase a Team that has already finished.
        [Test]
        public async Task Waiting_on_a_feature_with_no_work_left_is_not_reported_as_unforecastable()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));
            GivenTheWorkOnItIsAllFinished("F-1");

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").Entry("F-1").HasNothingWrongWithIt();
        }

        // What an operator hears. Both of these are already on screen for the user; they are in the log so
        // a support conversation can be had from a log file rather than from a screenshot.
        [Test]
        public async Task A_refresh_that_finds_a_circle_says_so_once_and_names_who_is_in_it()
        {
            var platform = GivenAPortfolio("Platform");

            await WhenARefreshRuns(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex),
                AFeatureWaitingOn("F-1", "Rebuild the search index", TheCatalogue));

            ThenTheOperatorWasWarnedOnceAboutACircleNaming("F-3", "F-1");
        }

        // One line for the lot of them. A line per Feature per refresh would bury the update summary an
        // operator actually reads, and this is a report rather than a fault.
        [Test]
        public async Task A_refresh_that_finds_features_no_one_can_forecast_says_how_many_in_one_line()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));
            GivenTheTeamBehindItHasNoMeasuredDelivery("F-1");

            await WhenARefreshRuns(
                platform,
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"),
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            ThenTheOperatorWasToldOnceHowManyCannotBeForecast(1);
        }

        [Test]
        public async Task A_refresh_with_nothing_wrong_adds_no_line_about_dependencies_at_all()
        {
            var platform = GivenAPortfolio("Platform");

            await WhenARefreshRuns(
                platform,
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"),
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            ThenNothingWasSaidAboutDependencies();
        }

        // @kpi - "The verdict is worked out from what the page already loaded". Asserted as a count rather
        // than a stopwatch reading: a wall-clock figure on one machine says nothing about an instance ten
        // times larger, and the failure worth catching is a read whose cost grows with the list.
        [Test]
        public async Task Reading_the_features_view_costs_the_same_however_many_features_there_are()
        {
            var platform = GivenAPortfolio("Platform");
            await WhenARefreshRuns(platform, AChainOfFeaturesWaitingOnEachOther(20));
            var overTwentyFeatures = await WhenTheFeaturesViewIsReadCountingWhatItAsksTheStore();

            await WhenARefreshRuns(platform, AChainOfFeaturesWaitingOnEachOther(200));
            var overTwoHundredFeatures = await WhenTheFeaturesViewIsReadCountingWhatItAsksTheStore();

            ThenTheReadAskedTheStoreTheSameNumberOfTimes(overTwentyFeatures, overTwoHundredFeatures);
        }

        // Everything in this epic is free. A licence check would hide what a Feature waits on from most
        // instances and leave the column blank with no way to tell why.
        [Test]
        public async Task The_list_is_answered_on_an_instance_with_no_premium_licence()
        {
            GivenNoPremiumLicence();
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").WaitsOn("F-1");
        }
    }
}
