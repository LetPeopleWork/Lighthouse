using Lighthouse.Backend.Models.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Acceptance scenarios - Slice 02: the count from slice 01 opened up. Which Features exactly is this
    /// one waiting on, what state are they in, and where can the reader go to look at them. Driving port:
    /// the route the dialog behind the Depends On cell calls.
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

        private static readonly NotHonouredReason[] TheThreeReasonsThisEpicCanProduce =
        [
            NotHonouredReason.OutsideThisPortfolio,
            NotHonouredReason.InALoop,
            NotHonouredReason.BlockerCannotBeForecast,
        ];

        // @driving_adapter @us-02 - "Opening the list of Features one is waiting on"
        [Test]
        public async Task Opening_what_a_feature_waits_on_names_it_with_its_state_its_portfolios_and_a_way_to_open_it()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheListIs(list, "F-1");
            ThenTheEntryFor(list, "F-1").Names("Rebuild the search index");
            ThenTheEntryFor(list, "F-1").SaysItIsInState("New");
            ThenTheEntryFor(list, "F-1").SaysItBelongsTo("Platform");
            ThenTheEntryFor(list, "F-1").OffersAWayToOpenIt("https://tracker.example/F-1");
        }

        // The list and the count on the row are read by the same person seconds apart, so an entry the
        // list leaves out is a number the reader cannot account for. The count leaves out a link naming
        // nothing Lighthouse holds, and so must this.
        [Test]
        public async Task A_link_naming_nothing_lighthouse_holds_is_no_entry_here_either()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform,
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndOneNobodyHolds));

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheListIs(list, "F-1");
            await ThenTheListIsAsLongAsTheCountOnTheRow("F-3");
        }

        // Clients pin themselves to a version and every other Features route answers on both. A route
        // that only answered on one would be reachable from the screen and unreachable from the client
        // that was told to ask for a version.
        [Test]
        public async Task Both_versions_of_the_route_answer_the_same_thing()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var (versionOne, latest) = await WhenTheReaderOpensItOnBothVersions("F-3");

            ThenBothVersionsSaidTheSameThing(versionOne, latest);
        }

        // Absent and empty are different answers: absent means nobody worked it out, empty means it is
        // waiting on nothing. A reader who cannot tell them apart has to guess which one they are looking
        // at, and this feature exists to stop that guess.
        [Test]
        public async Task A_feature_waiting_on_nothing_is_handed_an_empty_list_rather_than_no_list()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(platform, AFeatureTheTrackerHolds("F-3", "Publish the partner catalogue"));

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheListIs(list);
        }

        // @error - the Feature being asked about, not the ones it waits on. Same answer the work items
        // route gives, for the same reason: a different answer would confirm the Feature exists.
        [Test]
        public async Task A_feature_the_reader_may_not_read_is_not_found()
        {
            var platform = GivenAPortfolio("Platform");
            var somewhereElse = GivenAPortfolio("Somewhere else");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var answer = await WhenAReaderOfAnotherPortfolioOpensWhatItWaitsOn("F-3", somewhereElse);

            ThenTheAnswerIsNotFound(answer);
        }

        // @driving_adapter @us-02 - "Each entry says where Lighthouse read it from"
        [Test]
        public async Task Every_entry_says_it_was_read_from_the_work_tracking_systems_own_link()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheEntryFor(list, "F-1").SaysItCameFromTheTrackersOwnLink();
            ThenNoEntryClaimsASourceOutsideTheWorkTrackingSystem(list);
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

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheEntryFor(list, "F-9").CannotBeActedOnBecause("OutsideThisPortfolio");
            ThenTheEntryFor(list, "F-1").CarriesNoReasonAtAll();
        }

        // A caller meeting a reason it has never heard of has to guess, and the guess this feature exists
        // to prevent is "probably fine". Asserted against the set itself rather than against a list of
        // strings a scenario carries, which would drift the moment somebody adds a value.
        [Test]
        public void The_reasons_this_epic_can_produce_are_a_closed_set()
        {
            ThenTheReasonsAreExactly(TheThreeReasonsThisEpicCanProduce);
        }

        // @error @us-02 - "A Feature the reader may not see is named as withheld, never quietly dropped".
        // The number on the row counts every Feature waited on, readable or not, so an entry left out
        // here would make the list disagree with the number above it and give the reader nothing on
        // screen to explain the difference.
        [Test]
        public async Task A_feature_the_reader_may_not_see_is_withheld_rather_than_left_out()
        {
            var platform = GivenAPortfolio("Platform");
            var warehouse = GivenAPortfolio("Warehouse");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndTheWarehouse));
            await GivenAnotherPortfolioRefreshed(warehouse, AFeatureTheTrackerHolds("F-9", "Warehouse sync"));

            var list = await WhenAReaderOfOnlyOnePortfolioOpensWhatItWaitsOn("F-3", platform);

            ThenOneEntryIsWithheld(list);
            ThenTheWithheldEntryDisclosesNothingButThatItExists(list);
            ThenTheListIsAsLongAsWhatItWaitsOn(list, expectedEntries: 2);
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

            var asSomeoneWhoMayChangeIt = await WhenTheReaderOpensWhatItWaitsOn("F-3");
            var asSomeoneWhoMayOnlyRead = await WhenAReaderOfOnlyOnePortfolioOpensWhatItWaitsOn("F-3", platform);

            ThenBothReadersWereShownTheSame(asSomeoneWhoMayChangeIt, asSomeoneWhoMayOnlyRead);
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
        // The reader finds broken links by scanning the list they already have open, which is why the
        // warning rides on the payload that list is built from rather than behind a second request.
        [Test]
        public async Task Waiting_on_a_feature_outside_the_portfolio_warns_on_the_row_and_names_it()
        {
            var platform = GivenAPortfolio("Platform");
            var warehouse = GivenAPortfolio("Warehouse");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndexAndTheWarehouse));
            await GivenAnotherPortfolioRefreshed(warehouse, AFeatureTheTrackerHolds("F-9", "Warehouse sync"));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").WarnsAbout("F-9", "Warehouse sync", "OutsideThisPortfolio");
            ThenNoWarningCarriesASentenceNobodyCanRename(rows);
        }

        // Having a dependency is not a problem, so a Feature whose dependencies are all fine has nothing
        // to say here. An empty list rather than an absent one: the row was looked at and found sound.
        [Test]
        public async Task A_feature_whose_dependencies_are_all_sound_carries_no_warning_entries()
        {
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var rows = await WhenTheDeliveryLeadOpensTheFeaturesView();

            ThenTheRowFor(rows, "F-3").CarriesNoDependencyWarningAtAll();
            ThenTheRowFor(rows, "F-1").CarriesNoDependencyWarningAtAll();
        }

        // Everything in this epic is free. A licence check on the way in would make the list of what a
        // Feature waits on a paid answer, which is the opposite of what was decided.
        [Test]
        public async Task The_list_is_answered_on_an_instance_with_no_premium_licence()
        {
            GivenNoPremiumLicence();
            var platform = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheSearchIndex));

            var list = await WhenTheReaderOpensWhatItWaitsOn("F-3");

            ThenTheListIs(list, "F-1");
        }
    }
}
