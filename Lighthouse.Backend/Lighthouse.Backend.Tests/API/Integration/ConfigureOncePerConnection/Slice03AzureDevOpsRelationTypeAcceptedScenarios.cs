using System.Net;
using System.Reflection;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Tomás Ortega's PMO records waits with the custom relation type <c>Contoso.DependsOn</c> on Azure DevOps
    /// Server 2022. Today an Additional Field referencing <c>Contoso.DependsOn-Forward</c> fails Validate as a
    /// missing field, so there is no way to name it. From this slice it validates; a typo still fails and lists
    /// the relation types the organisation defines. Until the relation is actually read for dependencies, a Dependency Field that names a
    /// relation type reads nothing and Refresh History says it is not read yet, so the interim is never
    /// silent. Only the Dependency Field ever reads a relation type: naming one in any other setting reads as if
    /// nothing were named, and says so.
    ///
    /// Validate is posted to the validate route the connection page's Validate button posts to, and the answer
    /// is read as that button reads it; the production Azure DevOps connector answers it over a recorded client.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6312")]
    [Category("slice-03")]
    public class Slice03AzureDevOpsRelationTypeAcceptedScenarios : NamedDependencyLinkAcceptanceTest
    {
        private const int Item4711 = 4711;

        private const int Item4702 = 4702;

        private const string StoreOperations = "Store Operations";

        private const string AcceptedAsTheButtonReadsIt = "The Validate button is answered with OK.";

        private const string RefusedAsTheButtonReadsIt = "A refused connection is answered as a bad request, as the Validate button reads it.";

        private const string TheNativeParentStands = "The native parent stands.";

        private static readonly string[] Nothing = [];

        private static readonly string[] TheNotYetReadFinding = ["RelationTypeNotYetReadForDependencies"];

        [SetUp]
        public void ContosoListsItsCustomRelationType()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.RelationTypesItLists.AddRange([ContosoDependsOnForward, ContosoDependsOnReverse]);
            contoso.FieldsItDefines.Add("Custom.RemoteParent");
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.1")]
        [Category("driving_port")]
        [Category("contract-shape:pure-function")]
        public async Task A_relation_type_reference_with_its_end_validates_where_a_field_name_goes()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), AcceptedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.True, $"Validate refused it: {answer.Message}");
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.1")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task A_relation_type_reference_matches_in_any_case()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, "contoso.dependson-FORWARD");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), AcceptedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.True, $"Validate refused it: {answer.Message}");
            }
        }

        // Field lookup runs first and wins, so no reference that validated before changes meaning.
        [Test]
        [Category("us-03")]
        [Category("ac-3.1")]
        [Category("edge")]
        [Category("active-guard")]
        [Category("contract-shape:pure-function")]
        public async Task A_field_that_exists_validates_without_asking_for_the_relation_types()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);

            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, "Custom.RemoteParent");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), AcceptedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.True, $"Validate refused it: {answer.Message}");
                Assert.That(contoso.RelationTypeListings, Is.Zero,
                    "The relation types are asked for only when a reference missed the field list.");
            }
        }

        // The factory hands one connector to every connection of the tracker, so a listing it kept would answer
        // the next Validate for another organisation.
        [Test]
        [Category("us-03")]
        [Category("ac-3.7")]
        [Category("structural")]
        public void The_shared_connector_keeps_no_relation_type_listing_between_calls()
        {
            var listing = typeof(AzureDevOpsWorkTrackingConnector).GetNestedType("RelationTypeListing", BindingFlags.NonPublic);
            var resolution = typeof(AzureDevOpsWorkTrackingConnector).GetNestedType("AdditionalFieldResolution", BindingFlags.NonPublic);
            var fields = typeof(AzureDevOpsWorkTrackingConnector).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Type[] whatAnswersForOneOrganisation = [.. new[] { listing, resolution }.OfType<Type>()];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listing, Is.Not.Null, "The connector no longer names its relation-type listing as this check expects.");
                Assert.That(resolution, Is.Not.Null, "The connector no longer names its additional-field resolution as this check expects.");
                Assert.That(fields.Where(field => whatAnswersForOneOrganisation.Any(kept => Mentions(field.FieldType, kept))).Select(field => field.Name), Is.Empty);
            }
        }

        private static bool Mentions(Type type, Type kept)
            => type == kept
                || (type.GetElementType() is { } element && Mentions(element, kept))
                || type.GetGenericArguments().Any(argument => Mentions(argument, kept));

        [Test]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_typo_still_fails_and_lists_the_relation_types_the_organisation_defines()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, "Contoso.DependsOnn-Forward");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), RefusedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.False);
                Assert.That(answer.Message, Does.Contain("Contoso.DependsOnn-Forward"), "The refusal names what was not found.");
                Assert.That(answer.Message, Does.Contain(ContosoDependsOnForward).And.Contain(Successor).And.Contain(Related),
                    "The refusal lists what the organisation does define, so the admin can copy the right one.");
            }
        }

        // Attachments, hyperlinks and artifact links are listed beside the relation types but never link one
        // work item to another, so naming one is a typo like any other and the list offered never shows them.
        [Test]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_link_to_something_other_than_a_work_item_is_not_accepted_as_a_relation_type()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, "Hyperlink");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), RefusedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.False);
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task The_relation_types_offered_on_a_typo_leave_out_links_to_anything_but_a_work_item()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, "Contoso.DependsOnn-Forward");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Message, Does.Contain(Successor), "The refusal still lists the relation types it does define.");
                Assert.That(answer.Message, Does.Not.Contain("AttachedFile").And.Not.Contain("Hyperlink").And.Not.Contain("ArtifactLink"));
            }
        }

        [TestCase(false, TestName = "An organisation that lists no relation types at all fails validation saying it listed none")]
        [TestCase(true, TestName = "An organisation that answers the listing without a list fails validation saying it listed none")]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task An_organisation_that_listed_no_relation_types_fails_validation_saying_it_listed_none(bool withoutAList)
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.RelationTypesItLists.Clear();
            contoso.AnswersTheListingWithoutAList = withoutAList;

            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), RefusedAsTheButtonReadsIt);
                Assert.That(answer.Message, Does.Contain("It listed no relation types at all to check them against."));
            }
        }

        // An organisation that refuses the listing, or answers with none, must not resolve every reference to
        // nothing: the admin is told what happened instead.
        [Test]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task An_organisation_that_will_not_list_its_relation_types_fails_validation_saying_so()
        {
            TheOrganisationAt(ContosoOrganisation).RefusesToListItsRelationTypes = true;

            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), RefusedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.False);
                Assert.That(answer.Message, Does.Contain("relation types"),
                    "The admin learns that the relation types could not be read, not that the field is missing.");
            }
        }

        // A request that timed out or never reached Azure DevOps says no more about the reference than a
        // refusal does, so it is answered the same way and not as an unexpected error.
        [TestCase(typeof(TaskCanceledException), TestName = "A relation-type listing that timed out without anybody cancelling fails validation saying the relation types could not be read")]
        [TestCase(typeof(TimeoutException), TestName = "A relation-type listing that ran out of time fails validation saying the relation types could not be read")]
        [TestCase(typeof(HttpRequestException), TestName = "A relation-type listing that never reached Azure DevOps fails validation saying the relation types could not be read")]
        [Category("us-03")]
        [Category("ac-3.2")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_relation_type_listing_that_failed_on_the_way_fails_validation_saying_so(Type failure)
        {
            TheOrganisationAt(ContosoOrganisation).TheListingFailsWith = Activator.CreateInstance(failure, "The request to list the relation types did not complete.") as Exception
                ?? throw new InvalidOperationException($"{failure} is not an exception this scenario can throw.");

            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), RefusedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.False);
                Assert.That(answer.Message, Does.Contain("would not list its relation types"),
                    "The admin learns that the relation types could not be read, not that something unexpected went wrong.");
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.7")]
        [Category("kpi")]
        [Category("contract-shape:pure-function")]
        public async Task Validate_asks_the_organisation_for_its_relation_types_at_most_once()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);

            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward, ContosoDependsOnReverse, Successor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), AcceptedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.True, $"Validate refused it: {answer.Message}");
                Assert.That(contoso.RelationTypeListings, Is.EqualTo(1), "Three relation-type references, one listing.");
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_refresh_does_not_report_a_relation_type_reference_as_a_missing_field()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheLastRefreshWarned().Where(line => line.Contains("No field in this Azure DevOps organisation is called", StringComparison.Ordinal)), Is.Empty,
                    "A relation type is not a field nobody could find, and saying so on every refresh teaches the admin to ignore the log.");
                Assert.That(shown.Findings?.Select(finding => finding.Kind), Is.EqualTo(TheNotYetReadFinding),
                    "The one thing Refresh History says about it is that it is not read yet.");
            }
        }

        // Retired once the relation is read for dependencies; reading it replaces this interim warning.
        [Test]
        [Category("us-03")]
        [Category("ac-3.9")]
        [Category("error")]
        [Category("retired-by-epic-6303-slice-04")]
        [Category("contract-shape:bounded-change")]
        public async Task Until_relations_are_read_a_relation_type_Dependency_Field_reads_nothing_and_says_not_yet()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Nothing));
                Assert.That(shown.Findings?.Select(finding => finding.Kind), Is.EqualTo(TheNotYetReadFinding));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_as_Parent_Override_Field_keeps_the_native_parent_and_says_why()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Hierarchy, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsParentOverrideField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheParentShownFor(Item4711), Is.EqualTo($"{Item4702}"), TheNativeParentStands);
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeCannotBeReadHere"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("ParentOverrideField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
            }
        }

        // The field list is asked first and wins at refresh just as it does in Validate, so a field that happens
        // to share its name with a relation-type end keeps reading the field.
        [Test]
        [Category("us-03")]
        [Category("ac-3.1")]
        [Category("edge")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_field_named_exactly_like_a_relation_type_end_is_read_as_the_field()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.FieldsItDefines.Add(ContosoDependsOnForward);
            contoso.Holds(Item4711, Item4702);
            contoso.Fills(Item4711, ContosoDependsOnForward, $"{Item4702}");
            AnAzureDevOpsTeam(StoreOperations, ContosoOrganisation);
            TheTeamNamesAsItsParentOverrideField(StoreOperations, ContosoDependsOnForward);

            await TheTeamRefreshes(StoreOperations);
            var parent = await TheParentTheTeamShowsFor(StoreOperations, Item4711);
            var shown = await WhatRefreshHistoryShowsForTheTeam(StoreOperations);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent, Is.EqualTo($"{Item4702}"), "The parent is read from the field.");
                Assert.That(shown.Findings, Is.Not.Null.And.Empty, "Nothing about a field that reads is worth a warning.");
                Assert.That(contoso.RelationTypeListings, Is.Zero, "A reference the field list answered needs no relation types.");
            }
        }

        // A Team reads no relations for dependencies, so its relations are read only for the parent: the
        // relation type it named reads nothing, and the parent link is what keeps its work in place.
        [Test]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_as_a_Teams_Parent_Override_Field_keeps_the_native_parent_and_says_why()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Hierarchy, Item4702);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsTeam(StoreOperations, ContosoOrganisation);
            TheTeamNamesAsItsParentOverrideField(StoreOperations, ContosoDependsOnForward);

            var (theLastFullDownloadAt, afterACheapRefresh) = await TheTeamRefreshesUntilARefreshDownloadsOnlyWhatMoved(StoreOperations);
            var parent = await TheParentTheTeamShowsFor(StoreOperations, Item4711);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent, Is.EqualTo($"{Item4702}"), TheNativeParentStands);
                Assert.That(afterACheapRefresh.NewestRunMode, Is.EqualTo("Delta"));
                Assert.That(afterACheapRefresh.Findings, Is.Not.Null.And.Count.EqualTo(1),
                    "A cheap refresh cannot tell the setting is fine now, so the full download's finding still shows.");
                Assert.That(afterACheapRefresh.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeCannotBeReadHere"));
                Assert.That(afterACheapRefresh.TheFirstFinding?.Setting, Is.EqualTo("ParentOverrideField"));
                Assert.That(afterACheapRefresh.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
                Assert.That(afterACheapRefresh.FindingsAsOf, Is.EqualTo(theLastFullDownloadAt));
            }
        }

        // A listing that was refused says nothing about whether the reference names a relation type, so the
        // parent the tracker reports for itself has to stand rather than every item losing its parent.
        [Test]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_as_a_Teams_Parent_Override_Field_keeps_the_native_parent_when_the_relation_types_cannot_be_listed()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Hierarchy, Item4702);
            contoso.RefusesToListItsRelationTypes = true;
            AnAzureDevOpsTeam(StoreOperations, ContosoOrganisation);
            TheTeamNamesAsItsParentOverrideField(StoreOperations, ContosoDependsOnForward);

            await TheTeamRefreshes(StoreOperations);

            Assert.That(await TheParentTheTeamShowsFor(StoreOperations, Item4711), Is.EqualTo($"{Item4702}"), TheNativeParentStands);
        }

        // A Parent Override Field the listing shows to name neither a field nor a relation type is a missing
        // field: it is warned about, and it reads no parent, as it always has.
        [Test]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("edge")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Teams_Parent_Override_Field_naming_nothing_the_organisation_defines_reads_no_parent()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Hierarchy, Item4702);
            AnAzureDevOpsTeam(StoreOperations, ContosoOrganisation);
            TheTeamNamesAsItsParentOverrideField(StoreOperations, "Custom.NoSuchParent");

            await TheTeamRefreshes(StoreOperations);
            var parent = await TheParentTheTeamShowsFor(StoreOperations, Item4711);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent, Is.Empty);
                Assert.That(WhatTheLastRefreshWarned(), Has.Some.Contains("No field in this Azure DevOps organisation is called Custom.NoSuchParent"));
            }
        }

        [TestCase(ASettingThatReadsAField.SizeEstimateField, TestName = "A relation type named as Size Estimate Field reads as nothing named and says why")]
        [TestCase(ASettingThatReadsAField.FeatureOwnerField, TestName = "A relation type named as Feature Owner Field reads as nothing named and says why")]
        [TestCase(ASettingThatReadsAField.EstimationField, TestName = "A relation type named as Estimation field reads as nothing named and says why")]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_in_another_setting_reads_as_nothing_named_and_says_why(ASettingThatReadsAField setting)
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesIn(RetailRollout, setting, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Nothing), "The refresh still stores the Feature, and reads nothing from the relation.");
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeCannotBeReadHere"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo($"{setting}"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
            }
        }
    }
}
