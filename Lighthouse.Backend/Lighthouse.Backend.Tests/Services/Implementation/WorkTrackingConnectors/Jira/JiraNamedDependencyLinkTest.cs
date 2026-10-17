using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A Portfolio's Dependency Field naming a Jira link type instead of a field, read through the
    /// connector against an instance that answers its field list, its link type list and its search.
    /// </summary>
    public class JiraNamedDependencyLinkTest
    {
        private const int TheDependencyFieldId = 1;

        private const string TheFeature = "PAY-112";

        private const string WhatItBlocks = "PAY-98";

        private const string WhatBlocksIt = "PAY-50";

        private const string WhatGatesIt = "PAY-77";

        private static readonly JiraLinkType StockBlocks = new("Blocks", "is blocked by", "blocks");

        private static readonly JiraLinkType Gate = new("Gate", "is gated by", "blocks");

        private static readonly JiraLinkType Relates = new("Relates", "relates to", "relates to");

        private static readonly string[] TheStockBlocksPhrasesOutwardFirst = ["blocks", "is blocked by"];

        /// <summary>
        /// Jira's stock Blocks type is named the word its outward end reads. Typed exactly as one end reads,
        /// the phrase says which end waits, so the advice to type "blocks" that the both-ways finding gives
        /// can actually be followed.
        /// </summary>
        [TestCase("blocks", WhatItBlocks)]
        [TestCase("is blocked by", WhatBlocksIt)]
        public async Task A_phrase_typed_exactly_as_one_end_reads_picks_that_end_even_where_the_type_shares_its_name(
            string typed, string expected)
        {
            var subject = AnInstanceDefining([StockBlocks], AFeatureThatBlocksOneAndIsBlockedByAnother());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField(typed), CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { expected }));
                Assert.That(feature.RefreshFindings, Is.Empty);
            }
        }

        /// <summary>
        /// The type's own name, in its own capitals or any others, still says nothing about which end waits.
        /// </summary>
        [TestCase("Blocks")]
        [TestCase("BLOCKS")]
        public async Task The_types_name_reads_nothing_and_says_it_points_both_ways(string typed)
        {
            var subject = AnInstanceDefining([StockBlocks], AFeatureThatBlocksOneAndIsBlockedByAnother());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField(typed), CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatItWaitsOn(feature), Is.Empty);
                Assert.That(feature.RefreshFindings.Select(finding => finding.Kind), Is.EqualTo(new List<RefreshFindingKind> { RefreshFindingKind.NamedLinkPointsBothWays }));
            }
        }

        /// <summary>
        /// The two phrases are always offered outward end first, so the advice reads "blocks" or "is blocked
        /// by" on every instance rather than in whichever order the phrases happened to be read.
        /// </summary>
        [Test]
        public async Task A_name_pointing_both_ways_offers_the_outward_phrase_before_the_inward_one()
        {
            var subject = AnInstanceDefining([StockBlocks], AFeatureThatBlocksOneAndIsBlockedByAnother());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Blocks"), CancellationToken.None)).Single();

            Assert.That(feature.RefreshFindings.Single().Seen, Is.EqualTo(TheStockBlocksPhrasesOutwardFirst));
        }

        /// <summary>
        /// What was typed settles on one link type, and only that type's links are read. Another type whose
        /// end happens to read the same word as that type's name is a different relationship.
        /// </summary>
        [Test]
        public async Task Only_links_of_the_type_the_reference_settled_on_are_read()
        {
            var subject = AnInstanceDefining(
                [StockBlocks, Gate],
                JiraWireFormat.AnEpic(TheFeature, StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt), Gate.LinkWhoseInwardIssueIs(WhatGatesIt)));

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("is blocked by"), CancellationToken.None)).Single();

            Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { WhatBlocksIt }));
        }

        /// <summary>
        /// A Portfolio naming no Dependency Field has no setting to point at, so a refresh whose links carry no
        /// "is blocked by" is told so in the log, as it always was, and records no finding.
        /// </summary>
        [Test]
        public async Task A_Portfolio_naming_nothing_whose_links_carry_no_is_blocked_by_is_told_so_in_the_log_only()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AnInstanceDefining([StockBlocks, Relates], JiraWireFormat.AnEpic(TheFeature, Relates.LinkWhoseInwardIssueIs(WhatGatesIt)), log);
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            var feature = (await subject.GetFeaturesForProject(portfolio, CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.RefreshFindings, Is.Empty);
                Assert.That(log.Warnings, Has.One.EqualTo(
                    $"Portfolio {portfolio.Name} has Features linked to others, but none of those links is called 'is blocked by', "
                    + "which is the one Lighthouse reads as waiting. The link names it did see are: relates to"));
            }
        }

        private static string AFeatureThatBlocksOneAndIsBlockedByAnother()
            => JiraWireFormat.AnEpic(TheFeature, StockBlocks.LinkWhoseOutwardIssueIs(WhatItBlocks), StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt));

        private static List<string> WhatItWaitsOn(Feature feature)
            => [.. feature.DependsOnReferences.Select(reference => reference.ReferenceId)];

        private static Portfolio APortfolioNamingAsItsDependencyField(string reference)
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = TheDependencyFieldId,
                DisplayName = reference,
                Reference = reference,
            });

            portfolio.DependencyOverrideAdditionalFieldDefinitionId = TheDependencyFieldId;

            return portfolio;
        }

        private static JiraWorkTrackingConnector AnInstanceDefining(JiraLinkType[] linkTypes, string issue)
            => AnInstanceDefining(linkTypes, issue, new RecordingLogger<JiraWorkTrackingConnector>());

        private static JiraWorkTrackingConnector AnInstanceDefining(JiraLinkType[] linkTypes, string issue, RecordingLogger<JiraWorkTrackingConnector> log)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal))
                {
                    return JiraWireFormat.TheLinkTypeListDefining(linkTypes.Select((type, index) => JiraWireFormat.ALinkTypeDefinition(index, type)));
                }

                if (path.EndsWith(JiraWireFormat.CredentialCheckEndpoint, StringComparison.Ordinal))
                {
                    return "{\"accountId\":\"someone\"}";
                }

                return JiraWireFormat.ACloudResponseTo(request, [issue]);
            }), log);
    }
}
