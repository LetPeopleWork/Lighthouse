using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OrderedParentSources
{
    /// <summary>
    /// What a refresh of an Azure DevOps Team does with its ordered parent sources. Azure DevOps has no links, so
    /// every entry is a field or the native parent, and a record never needs the Features Lighthouse holds: the
    /// parent is decided in one pass. They run through the running application with the real Azure DevOps
    /// connector over a faked work item tracking client, on the harness of the named-dependency scenarios, which
    /// also records every request the connector made, so "no more requests than before" is a count.
    ///
    /// The scenarios are written against the planned shape of the owner. What is missing until the slice builds
    /// it: <c>OwnParentSources</c> on the owner (a stand-in that throws is there) and the connector reading every
    /// position, the native parent only when it is listed, and relations only when the list or the dependencies
    /// ask for them. Changing a list refetches and a field in use cannot be removed from its connection: those two
    /// rules are not tracker-specific and are pinned once, in the fingerprint and connection-save scenarios.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsParentSourcesScenarios : NamedDependencyLinkAcceptanceTest
    {
        private const string EpicLink = "Custom.EpicLink";

        private const string ProgramRef = "Custom.ProgramRef";

        private const string NorthwindOrganisation = "https://dev.azure.com/northwind";

        private const string PaymentsMobile = "Payments Mobile";

        private const string PaymentsWeb = "Payments Web";

        private const string PaymentsApi = "Payments Api";

        private static readonly string[] TheFeatureWaitsOnTheTargetOfItsPredecessor = ["4702"];

        private const int Item4710 = 4710;

        private const int Item4712 = 4712;

        private const int Item4713 = 4713;

        private const int TheNativeParent = 9050;

        private const int TheEpicLinkParent = 9101;

        private const int TheProgramRefParent = 9100;

        /// <summary>
        /// Given Payments Mobile lists "Custom.EpicLink" then "Custom.ProgramRef", Payments Web lists them the
        /// other way round, item 4712 has only "Custom.ProgramRef", and item 4713 has both, then 4712 shows under
        /// the Program Ref parent for both Teams, and 4713 under the Epic Link parent for Payments Mobile and the
        /// Program Ref parent for Payments Web: the order decides when both have one.
        /// </summary>
        [Test]
        public async Task The_second_custom_field_gives_the_parent_when_the_first_is_empty_and_the_order_decides_when_both_have_one()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.FieldsItDefines.AddRange([EpicLink, ProgramRef]);
            contoso.Holds(Item4712, Item4713);
            contoso.Fills(Item4712, ProgramRef, $"{TheProgramRefParent}");
            contoso.Fills(Item4713, EpicLink, $"{TheEpicLinkParent}");
            contoso.Fills(Item4713, ProgramRef, $"{TheProgramRefParent}");
            AnAzureDevOpsTeam(PaymentsMobile, ContosoOrganisation);
            TheTeamListsTheParentSources(PaymentsMobile, TheTeamsSource(PaymentsMobile, EpicLink), TheTeamsSource(PaymentsMobile, ProgramRef));
            AnAzureDevOpsTeam(PaymentsWeb, ContosoOrganisation);
            TheTeamListsTheParentSources(PaymentsWeb, TheTeamsSource(PaymentsWeb, ProgramRef), TheTeamsSource(PaymentsWeb, EpicLink));

            await TheTeamRefreshes(PaymentsMobile);
            await TheTeamRefreshes(PaymentsWeb);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4712), Is.EqualTo($"{TheProgramRefParent}"));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsWeb, Item4712), Is.EqualTo($"{TheProgramRefParent}"));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4713), Is.EqualTo($"{TheEpicLinkParent}"));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsWeb, Item4713), Is.EqualTo($"{TheProgramRefParent}"));
            }
        }

        /// <summary>
        /// Given item 4713 has no custom value and the native parent 9050, when a Team lists "Custom.EpicLink" then
        /// the native parent and refreshes, 4713 shows under 9050 and the relations were read; when another Team
        /// lists "Custom.EpicLink" only, 4713 has no parent and no relations were read at all, so an install with
        /// one source makes no more requests than before; and a Team whose only entry the connector cannot
        /// classify reads it as a field, raises one finding, and still reads no relations.
        /// </summary>
        [Test]
        public async Task The_native_parent_and_its_relations_are_read_only_when_the_list_asks_for_them()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            var fabrikam = TheOrganisationAt(FabrikamOrganisation);
            var northwind = TheOrganisationAt(NorthwindOrganisation);
            foreach (var organisation in new[] { contoso, fabrikam })
            {
                organisation.FieldsItDefines.Add(EpicLink);
                organisation.Holds(Item4713, TheNativeParent);
                organisation.Relates(Item4713, Hierarchy, TheNativeParent);
            }

            northwind.Holds(Item4713, TheNativeParent);
            northwind.Relates(Item4713, Hierarchy, TheNativeParent);
            AnAzureDevOpsTeam(PaymentsMobile, ContosoOrganisation);
            TheTeamListsTheParentSources(PaymentsMobile, TheTeamsSource(PaymentsMobile, EpicLink), ParentSourceEntry.TrackerParent);
            AnAzureDevOpsTeam(PaymentsWeb, FabrikamOrganisation);
            TheTeamListsTheParentSources(PaymentsWeb, TheTeamsSource(PaymentsWeb, EpicLink));
            AnAzureDevOpsTeam(PaymentsApi, NorthwindOrganisation);
            TheTeamListsTheParentSources(PaymentsApi, TheTeamsSource(PaymentsApi, "Custom.NobodyDefined"));

            await TheTeamRefreshes(PaymentsMobile);
            await TheTeamRefreshes(PaymentsWeb);
            await TheTeamRefreshes(PaymentsApi);
            var apiFindings = (await WhatRefreshHistoryShowsForTheTeam(PaymentsApi)).Findings;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4713), Is.EqualTo($"{TheNativeParent}"));
                Assert.That(contoso.Requests, Has.Some.StartWith("relations"));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsWeb, Item4713), Is.Empty, "The native parent is read only when it is listed.");
                Assert.That(fabrikam.Requests, Has.None.StartWith("relations"), "A list of fields alone asks for nothing it never asked for.");
                Assert.That(northwind.Requests, Has.None.StartWith("relations"));
                Assert.That(apiFindings, Is.Not.Null.And.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Given a Team listed a relation type, then "Custom.ProgramRef", and item 4712 has Program Ref 9100, when
        /// the Team refreshes, then Refresh History says the relation type cannot be read as a parent, and the next
        /// entry still gives the parent.
        /// </summary>
        [Test]
        public async Task A_relation_type_in_the_list_is_named_in_Refresh_History_and_the_other_entries_still_give_parents()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.FieldsItDefines.Add(ProgramRef);
            contoso.RelationTypesItLists.Add(ContosoDependsOnForward);
            contoso.Holds(Item4712);
            contoso.Fills(Item4712, ProgramRef, $"{TheProgramRefParent}");
            AnAzureDevOpsTeam(PaymentsMobile, ContosoOrganisation);
            TheTeamListsTheParentSources(PaymentsMobile, TheTeamsSource(PaymentsMobile, ContosoDependsOnForward), TheTeamsSource(PaymentsMobile, ProgramRef));

            await TheTeamRefreshes(PaymentsMobile);
            var shown = await WhatRefreshHistoryShowsForTheTeam(PaymentsMobile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeCannotBeReadHere"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("ParentOverrideField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4712), Is.EqualTo($"{TheProgramRefParent}"));
            }
        }

        /// <summary>
        /// Given a Team that named "Custom.EpicLink" as its only Parent Override Field before the upgrade, so it now
        /// lists "Custom.EpicLink" then the native parent, and item 4710 has an Epic Link value while item 4713 has
        /// none and the native parent 9050, when the Team refreshes after the upgrade, then 4710 keeps the parent it
        /// had, 4713 now shows under 9050, and the relations are read on every refresh from then on.
        /// </summary>
        [Test]
        public async Task After_the_upgrade_an_item_with_no_field_value_and_a_native_parent_hangs_under_it_and_relations_are_read_each_refresh_that_fetches_it()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.FieldsItDefines.Add(EpicLink);
            contoso.Holds(Item4710, Item4713, TheNativeParent);
            contoso.Fills(Item4710, EpicLink, $"{TheEpicLinkParent}");
            contoso.Relates(Item4713, Hierarchy, TheNativeParent);
            AnAzureDevOpsTeam(PaymentsMobile, ContosoOrganisation);
            TheTeamListsTheParentSources(PaymentsMobile, TheTeamsSource(PaymentsMobile, EpicLink), ParentSourceEntry.TrackerParent);

            await TheTeamRefreshes(PaymentsMobile);
            contoso.Requests.Clear();
            contoso.Changes(Item4710);
            contoso.Changes(Item4713);
            await TheTeamRefreshes(PaymentsMobile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4710), Is.EqualTo($"{TheEpicLinkParent}"));
                Assert.That(await TheParentTheTeamShowsFor(PaymentsMobile, Item4713), Is.EqualTo($"{TheNativeParent}"));
                Assert.That(contoso.Requests, Has.Some.StartWith("relations"), "The native parent is listed, so every refresh reads the relations.");
            }
        }

        /// <summary>
        /// Given a Portfolio whose own parent sources list holds only "Custom.EpicLink", and a Feature that waits
        /// on the work item at the other end of its Predecessor relation, when the Portfolio refreshes, then the
        /// Feature waits on that work item: the relations are read for the dependencies although no parent source
        /// asks for them.
        /// </summary>
        [Test]
        public async Task A_Portfolio_that_lists_only_fields_still_reads_its_dependencies_from_the_relations()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.FieldsItDefines.Add(EpicLink);
            contoso.Holds(4711, 4702);
            contoso.Relates(4711, Predecessor, 4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            ThePortfolioListsTheParentSources(RetailRollout, ThePortfoliosSource(RetailRollout, EpicLink));

            await RefreshesNow(RetailRollout);

            Assert.That(await WhatTheFeatureWaitsOn("4711"), Is.EqualTo(TheFeatureWaitsOnTheTargetOfItsPredecessor).AsCollection);
        }
    }
}
