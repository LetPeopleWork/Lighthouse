using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Tomás names <c>Contoso.DependsOn-Forward</c> as Retail Rollout's Dependency Field. After its update, each
    /// Feature waits on the work items at the other end of its relations of exactly that end, and the
    /// Predecessor relation no longer counts for that Portfolio. A reference without its end, or a relation type
    /// with no direction such as Related, reads nothing and says why; nothing matching anywhere says so once,
    /// listing the relation types the Features did carry.
    ///
    /// The relations of a Portfolio's work items arrive in a second request batch that is skipped today when a
    /// Parent Override Field is set. A Dependency Field naming a relation type needs that batch, so it is asserted
    /// with a Parent Override set too.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6312")]
    [Category("slice-04")]
    public class Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios : NamedDependencyLinkAcceptanceTest
    {
        private const int Item4711 = 4711;

        private const int Item4702 = 4702;

        private const int Item4690 = 4690;

        private const int Item5101 = 5101;

        private const int Item5102 = 5102;

        private const string FabrikamNeedsForward = "Fabrikam.Needs-Forward";

        private static readonly string[] Nothing = [];

        private static readonly string[] Waits4702 = ["4702"];

        private static readonly string[] Waits4711 = ["4711"];

        private static readonly string[] Waits5102 = ["5102"];

        private static readonly string[] TheRelationTypesSeen = [Predecessor, Related];

        private static readonly string[] BothEndsOfContosoDependsOn = [ContosoDependsOnForward, ContosoDependsOnReverse];

        private static readonly string[] TheNoDirectionFinding = ["RelationTypeHasNoDirection"];

        private static readonly DependencySource[] FromTheTrackersLink = [DependencySource.TrackerLink];

        [SetUp]
        public void ContosoAndFabrikamListTheirCustomRelationTypes()
        {
            TheOrganisationAt(ContosoOrganisation).RelationTypesItLists.AddRange([ContosoDependsOnForward, ContosoDependsOnReverse]);
            TheOrganisationAt(FabrikamOrganisation).RelationTypesItLists.AddRange([FabrikamNeedsForward, "Fabrikam.Needs-Reverse"]);
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.4")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_waits_on_the_work_item_at_the_other_end_of_the_named_relation()
        {
            RetailRolloutHolds4711DependingOn4702();

            await RefreshesNow(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702));
                Assert.That(await WhereTheFeaturesDependenciesWereReadFrom($"{Item4711}"), Is.EqualTo(FromTheTrackersLink));
                Assert.That(await WhatTheWorkItemWaitsOn(Item4702), Is.EqualTo(Nothing), "The other end does not wait on anything.");
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.4")]
        [Category("contract-shape:bounded-change")]
        public async Task Naming_the_reverse_end_reads_the_other_direction()
        {
            RetailRolloutHolds4711DependingOn4702();
            TheOrganisationAt(ContosoOrganisation).Relates(Item4702, ContosoDependsOnReverse, Item4711);
            NamesAsItsDependencyField(NorthernStoresOnContoso(), ContosoDependsOnReverse);

            await RefreshesNow(NorthernStores);

            Assert.That(await WhatTheWorkItemWaitsOn(Item4702), Is.EqualTo(Waits4711));
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.4")]
        [Category("contract-shape:bounded-change")]
        public async Task Naming_a_relation_end_replaces_Predecessor_for_that_Portfolio()
        {
            RetailRolloutHolds4711DependingOn4702();
            TheOrganisationAt(ContosoOrganisation).Relates(Item4711, Predecessor, Item4690);

            await RefreshesNow(RetailRollout);

            Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702), "Predecessor counts only while the Portfolio names nothing.");
        }

        // A Parent Override set today skips the download of every relation, and a Dependency Field that names a
        // relation type would then read nothing on exactly the Portfolios most likely to use both.
        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.5")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Parent_Override_Field_on_the_same_Portfolio_does_not_stop_the_relation_being_read()
        {
            RetailRolloutHolds4711DependingOn4702();
            TheOrganisationAt(ContosoOrganisation).FieldsItDefines.Add("Custom.RemoteParent");
            TheOrganisationAt(ContosoOrganisation).Fills(Item4711, "Custom.RemoteParent", $"{Item4690}");
            NamesAsItsParentOverrideField(RetailRollout, "Custom.RemoteParent");

            await RefreshesNow(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702));
                Assert.That(await TheParentShownFor(Item4711), Is.EqualTo($"{Item4690}"), "The Parent Override still places the Feature.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.6")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_relation_type_named_without_its_end_reads_nothing_and_names_both_ends()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            contoso.RelationTypesItLists.Add(ContosoDependsOn);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOn);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Nothing), "Guessing a direction would invert every wait it guessed wrong.");
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("RelationTypeNamedWithoutItsEnd"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOn));
                Assert.That(shown.TheFirstFinding?.Seen ?? Array.Empty<string>(), Is.EquivalentTo(BothEndsOfContosoDependsOn));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.6")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Related_has_no_direction_so_it_reads_nothing_and_says_so()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Related, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, Related);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Nothing));
                Assert.That(shown.Findings?.Select(finding => finding.Kind), Is.EqualTo(TheNoDirectionFinding));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(Related));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.6")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Nothing_carrying_the_named_end_says_so_once_and_lists_the_relation_types_seen()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702, Item4690);
            contoso.Relates(Item4711, Predecessor, Item4702);
            contoso.Relates(Item4702, Related, Item4690);
            contoso.Relates(Item4690, Related, Item4711);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(RetailRollout);
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1), "One finding per refresh, never one per work item.");
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("NamedLinkMatchedNothing"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("DependencyField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo(ContosoDependsOnForward));
                Assert.That(shown.TheFirstFinding?.Seen ?? Array.Empty<string>(), Is.EquivalentTo(TheRelationTypesSeen));
                Assert.That(WhatTheLastRefreshWarned().Count(line => line.Contains(ContosoDependsOnForward, StringComparison.Ordinal)), Is.EqualTo(1));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.7")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task Naming_a_relation_end_costs_one_relation_type_listing_and_nothing_per_work_item()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702, Item4690);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsPortfolio(NorthernStores, ContosoOrganisation);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOnForward);

            await RefreshesNow(NorthernStores);
            var namingNothing = contoso.Requests.Count;
            await RefreshesNow(RetailRollout);
            var namingTheEnd = contoso.Requests.Count - namingNothing;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(namingTheEnd, Is.EqualTo(namingNothing + 1), $"Requests: {string.Join(", ", contoso.Requests)}");
                Assert.That(contoso.RelationTypeListings, Is.EqualTo(1));
            }
        }

        // Connector instances are shared by every connection of a tracker, so a listing remembered on the
        // connector would answer Fabrikam's question with Contoso's relation types.
        [Test]
        [Ignore(PendingEpic6303.Slice04)]
        [Category("us-03")]
        [Category("ac-3.7")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Two_organisations_on_one_shared_connector_each_answer_with_their_own_relation_types()
        {
            RetailRolloutHolds4711DependingOn4702();
            var fabrikam = TheOrganisationAt(FabrikamOrganisation);
            fabrikam.Holds(Item5101, Item5102);
            fabrikam.Relates(Item5101, FabrikamNeedsForward, Item5102);
            AnAzureDevOpsPortfolio(NorthernStores, FabrikamOrganisation);
            NamesAsItsDependencyField(NorthernStores, FabrikamNeedsForward);

            await RefreshesNow(RetailRollout);
            await RefreshesNow(NorthernStores);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702));
                Assert.That(await WhatTheWorkItemWaitsOn(Item5101), Is.EqualTo(Waits5102));
                Assert.That(TheOrganisationAt(ContosoOrganisation).RelationTypeListings, Is.EqualTo(1));
                Assert.That(fabrikam.RelationTypeListings, Is.EqualTo(1));
            }
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.4")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_naming_nothing_still_reads_Predecessor_without_listing_relation_types()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702);
            contoso.Relates(Item4711, Predecessor, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);

            await RefreshesNow(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702));
                Assert.That(contoso.RelationTypeListings, Is.Zero);
            }
        }

        private void RetailRolloutHolds4711DependingOn4702()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702, Item4690);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);
            AnAzureDevOpsPortfolio(RetailRollout, ContosoOrganisation);
            NamesAsItsDependencyField(RetailRollout, ContosoDependsOnForward);
        }

        private string NorthernStoresOnContoso()
        {
            AnAzureDevOpsPortfolio(NorthernStores, ContosoOrganisation);
            return NorthernStores;
        }
    }
}
