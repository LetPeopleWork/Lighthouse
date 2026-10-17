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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), "The Validate button is answered with OK.");
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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), "The Validate button is answered with OK.");
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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), "The Validate button is answered with OK.");
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
            var fields = typeof(AzureDevOpsWorkTrackingConnector).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listing, Is.Not.Null, "The connector no longer names its relation-type listing as this check expects.");
                Assert.That(fields.Where(field => Mentions(field.FieldType, listing!)).Select(field => field.Name), Is.Empty);
            }
        }

        private static bool Mentions(Type type, Type listing)
            => type == listing
                || (type.HasElementType && Mentions(type.GetElementType()!, listing))
                || type.GetGenericArguments().Any(argument => Mentions(argument, listing));

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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), "A refused connection is answered as a bad request, as the Validate button reads it.");
                Assert.That(answer.IsValid, Is.False);
                Assert.That(answer.Message, Does.Contain("Contoso.DependsOnn-Forward"), "The refusal names what was not found.");
                Assert.That(answer.Message, Does.Contain(ContosoDependsOnForward).And.Contain(Successor).And.Contain(Related),
                    "The refusal lists what the organisation does define, so the admin can copy the right one.");
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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.BadRequest), "A refused connection is answered as a bad request, as the Validate button reads it.");
                Assert.That(answer.IsValid, Is.False);
                Assert.That(answer.Message, Does.Contain("relation types"),
                    "The admin learns that the relation types could not be read, not that the field is missing.");
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
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), "The Validate button is answered with OK.");
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
                Assert.That(shown.Findings?.Select(finding => finding.Named), Has.None.Contains("no field"));
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
        [Ignore(PendingEpic6303.Slice03)]
        [Category("us-03")]
        [Category("ac-3.10")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_as_Parent_Override_Field_keeps_the_native_parent_and_says_why()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, "System.LinkTypes.Hierarchy-Reverse", Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsParentOverrideField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheParentShownFor(Item4711), Is.EqualTo($"{Item4702}"), "The native parent stands.");
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeCannotBeReadHere"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("ParentOverrideField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
            }
        }

        [TestCase(ASettingThatReadsAField.SizeEstimateField, TestName = "A relation type named as Size Estimate Field reads as nothing named and says why")]
        [TestCase(ASettingThatReadsAField.FeatureOwnerField, TestName = "A relation type named as Feature Owner Field reads as nothing named and says why")]
        [TestCase(ASettingThatReadsAField.EstimationField, TestName = "A relation type named as Estimation field reads as nothing named and says why")]
        [Ignore(PendingEpic6303.Slice03)]
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
