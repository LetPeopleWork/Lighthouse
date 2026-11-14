using System.Net;
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
    public class Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios : NamedDependencyLinkAcceptanceTest
    {
        private const int Item4711 = 4711;

        private const int Item4702 = 4702;

        private const int Item4690 = 4690;

        private const int Item5101 = 5101;

        private const int Item5102 = 5102;

        private const string FabrikamNeedsForward = "Fabrikam.Needs-Forward";

        private const string HierarchyForward = "System.LinkTypes.Hierarchy-Forward";

        private static readonly string[] Nothing = [];

        private static readonly string[] Waits4702 = ["4702"];

        private static readonly string[] Waits4711 = ["4711"];

        private static readonly string[] Waits5102 = ["5102"];

        private static readonly string[] TheRelationTypesSeen = [Predecessor, Related];

        private static readonly string[] OnlyRelated = [Related];

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

        // Every Feature handed back overwrites what is stored for it, so a refresh that could not tell what the
        // Dependency Field names would empty every wait the last good refresh read.
        [Test]
        [Category("us-03")]
        [Category("ac-3.7")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_refresh_that_cannot_list_the_relation_types_stops_and_keeps_what_the_last_one_read()
        {
            RetailRolloutHolds4711DependingOn4702();
            await RefreshesNow(RetailRollout);

            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.RefusesToListItsRelationTypes = true;
            var requestsBefore = contoso.Requests.Count;
            await SomethingMakesTheNextRefreshDownloadEverything(RetailRollout);
            AMinutePasses();
            await RefreshesNow(RetailRollout);

            var sentWhileStopped = contoso.Requests.Skip(requestsBefore).ToList();
            var shown = await WhatRefreshHistoryShowsFor(RetailRollout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentWhileStopped, Has.Some.EqualTo("relation types"), "The refresh this scenario describes never asked for the relation types.");
                Assert.That(await WhatTheWorkItemWaitsOn(Item4711), Is.EqualTo(Waits4702));
                Assert.That(shown.NewestRunSucceeded, Is.False, "A stopped refresh is not one that worked.");
                Assert.That(shown.Findings ?? [], Is.Empty, "A stopped refresh records no findings of its own.");
                Assert.That(shown.FindingsAsOf, Is.Not.EqualTo(shown.NewestRunAt), "What is shown comes from the last refresh that finished.");
                Assert.That(sentWhileStopped, Has.None.StartsWith("payload:").And.None.StartsWith("relations:"),
                    $"Nothing it downloads can be kept, so it downloads nothing. Requests: {string.Join(", ", sentWhileStopped)}");
            }
        }

        private void RetailRolloutHolds4711DependingOn4702()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.Holds(Item4711, Item4702, Item4690);
            contoso.Relates(Item4711, ContosoDependsOnForward, Item4702);

            // Azure DevOps stores one link and serves it from both ends, so the other work item carries its mirror.
            contoso.Relates(Item4702, ContosoDependsOnReverse, Item4711);
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
