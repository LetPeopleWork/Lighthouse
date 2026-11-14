using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;

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

        private static readonly JiraLinkType Dependency = new("Dependency", "is depended on by", "depends on");

        private static readonly string[] TheStockBlocksPhrasesOutwardFirst = ["blocks", "is blocked by"];

        private static readonly string[] TheOutwardPhrasesOfBlocksAndRelates = ["blocks", "relates to"];

        private static readonly string[] TheTwoPhrasesOfTheDependencyType = ["depends on", "is depended on by"];

        private static readonly DependencySource[] ReadFromThePortfoliosField = [DependencySource.PortfolioField];

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

        /// <summary>
        /// A link is served from both ends, and a Portfolio may hold only the end that does not wait: every
        /// Feature blocks or relates to something outside it. Those links still exist, so a Dependency Field
        /// that matched none of them is told so, with the phrases the Features actually carry.
        /// </summary>
        [Test]
        public async Task Features_carrying_only_the_outward_end_of_other_links_are_told_the_named_link_matched_nothing()
        {
            var subject = AnInstanceDefining(
                [StockBlocks, Dependency, Relates],
                [
                    JiraWireFormat.AnEpic(TheFeature, StockBlocks.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                    JiraWireFormat.AnEpic(WhatGatesIt, Relates.LinkWhoseOutwardIssueIs(WhatBlocksIt), StockBlocks.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                ],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None);
            var findings = features.SelectMany(feature => feature.RefreshFindings).Distinct().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(findings.Select(finding => finding.Kind), Is.EqualTo(new List<RefreshFindingKind> { RefreshFindingKind.NamedLinkMatchedNothing }));
                Assert.That(findings.Select(finding => finding.Seen).FirstOrDefault(), Is.EqualTo(TheOutwardPhrasesOfBlocksAndRelates));
            }
        }

        /// <summary>
        /// A Portfolio that ignores dependencies loses nothing when the link type list cannot be read, so
        /// stopping its refresh would only freeze its Features and forecasts for no gain.
        /// </summary>
        [Test]
        public async Task A_Portfolio_that_ignores_dependencies_is_refreshed_even_when_the_link_type_list_is_refused()
        {
            var portfolio = APortfolioNamingAsItsDependencyField("depends on");
            portfolio.IgnoreDependencies = true;
            var subject = JiraConnectorTestSetup.AConnectorOver(StubTransport.AnsweringWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                return path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AnInstanceAnswering(request, [AFeatureThatBlocksOneAndIsBlockedByAnother()])) };
            }));

            var features = await subject.GetFeaturesForProject(portfolio, CancellationToken.None);

            Assert.That(features.Select(feature => feature.ReferenceId), Is.EqualTo(new List<string> { TheFeature }));
        }

        /// <summary>
        /// Two link types whose ends read alike leave a typed phrase undecided, and reading either would
        /// quietly read the wrong links. Nothing is read, and the log says why, once for the refresh rather
        /// than once per Feature or per download.
        /// </summary>
        [Test]
        public async Task A_phrase_two_link_types_share_reads_nothing_and_is_named_once_in_the_log()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AnInstanceDefining(
                [StockBlocks, Gate],
                [
                    JiraWireFormat.AnEpic(TheFeature, StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt)),
                    JiraWireFormat.AnEpic(WhatItBlocks, Gate.LinkWhoseInwardIssueIs(WhatGatesIt)),
                ],
                log);
            var portfolio = APortfolioNamingAsItsDependencyField("blocks");

            var features = await subject.GetFeaturesForProject(portfolio, CancellationToken.None);
            await subject.GetFeaturesForProject(portfolio, [TheFeature, WhatItBlocks], CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.SelectMany(WhatItWaitsOn), Is.Empty);
                Assert.That(log.Warnings, Has.One.EqualTo(
                    $"Portfolio {portfolio.Name}: its Dependency Field names 'blocks', which more than one Jira link type uses (Blocks, Gate). "
                    + "Name a phrase only one of them uses."));
            }
        }

        /// <summary>
        /// The log line about a shared phrase is written by the refresh that first meets it, not held back
        /// for a later one.
        /// </summary>
        [Test]
        public async Task A_phrase_two_link_types_share_is_named_in_the_log_by_the_first_refresh_that_meets_it()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AnInstanceDefining([StockBlocks, Gate], AFeatureThatBlocksOneAndIsBlockedByAnother(), log);
            var portfolio = APortfolioNamingAsItsDependencyField("blocks");

            await subject.GetFeaturesForProject(portfolio, CancellationToken.None);

            Assert.That(log.Warnings, Has.One.Contains("which more than one Jira link type uses (Blocks, Gate)"));
        }

        /// <summary>
        /// A finding names the Dependency Field by what the administrator typed into it, so the screen can
        /// quote it back to them.
        /// </summary>
        [Test]
        public async Task A_finding_names_what_was_typed_into_the_Dependency_Field()
        {
            var subject = AnInstanceDefining([StockBlocks], AFeatureThatBlocksOneAndIsBlockedByAnother());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Blocks"), CancellationToken.None)).Single();

            Assert.That(feature.RefreshFindings.Single().Named, Is.EqualTo("Blocks"));
        }

        /// <summary>
        /// A named link that matched nothing is told as a finding. The older log line, about links not
        /// called "is blocked by", is for a Portfolio naming nothing and would point this one the wrong way.
        /// </summary>
        [Test]
        public async Task A_named_link_that_matched_nothing_is_not_also_told_about_is_blocked_by()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AnInstanceDefining(
                [StockBlocks, Dependency, Relates],
                JiraWireFormat.AnEpic(TheFeature, Relates.LinkWhoseInwardIssueIs(WhatGatesIt)),
                log);

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.RefreshFindings.Select(finding => finding.Kind), Is.EqualTo(new List<RefreshFindingKind> { RefreshFindingKind.NamedLinkMatchedNothing }));
                Assert.That(log.Warnings, Has.None.Contains("is blocked by"));
            }
        }

        /// <summary>
        /// When Jira will not say which link types it has, a Dependency Field that names no field cannot be
        /// told apart from one naming a link type. Reading on would overwrite the stored dependencies with
        /// nothing, so the refresh stops and says how Jira answered.
        /// </summary>
        [TestCase(HttpStatusCode.ServiceUnavailable, "503 (ServiceUnavailable)")]
        [TestCase(HttpStatusCode.OK, "a reply Lighthouse could not read")]
        public async Task A_link_type_list_that_cannot_be_read_stops_the_refresh_and_says_how_Jira_answered(HttpStatusCode status, string howJiraAnswered)
        {
            var subject = AnInstanceAnsweringTheLinkTypeListWith(status, [AFeatureThatBlocksOneAndIsBlockedByAnother()]);

            var stopped = await Assert.ThrowsAsync<JiraReadException>(
                () => subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None));

            Assert.That(stopped?.Message, Does.Contain($"answered its issue link type list with {howJiraAnswered}, so the refresh was stopped"));
        }

        /// <summary>
        /// A Portfolio naming no Dependency Field still has its link types read when another setting names a
        /// link, and it is refreshed as usual.
        /// </summary>
        [Test]
        public async Task A_Portfolio_naming_no_Dependency_Field_is_refreshed_when_another_setting_names_a_link()
        {
            var hierarchy = new JiraLinkType("Hierarchy", "is child of", "is parent of");
            var subject = AnInstanceDefining([StockBlocks, Gate, hierarchy], JiraWireFormat.AnEpic(TheFeature, hierarchy.LinkWhoseInwardIssueIs(WhatGatesIt)));
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = TheDependencyFieldId + 1,
                DisplayName = "is child of",
                Reference = "is child of",
            });
            portfolio.OwnParentOverride(TheDependencyFieldId + 1);

            var features = await subject.GetFeaturesForProject(portfolio, CancellationToken.None);

            Assert.That(features.Select(feature => feature.ReferenceId), Is.EqualTo(new List<string> { TheFeature }));
        }

        /// <summary>
        /// A Dependency Field that names a field Jira defines is read from that field, even on an instance
        /// whose link types were listed for another setting.
        /// </summary>
        [Test]
        public async Task A_Dependency_Field_naming_a_field_reads_what_was_typed_into_it()
        {
            const string theField = "Waits On";
            var subject = JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal))
                {
                    return JiraWireFormat.TheFieldListDefining([theField]);
                }

                return AnInstanceAnswering(request, [JiraWireFormat.AnIssueCarrying(
                    TheFeature, "Epic", $",\"{JiraWireFormat.TheCustomFieldIdAt(0)}\":\"{WhatGatesIt}\"", StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt))]);
            }), new RecordingLogger<JiraWorkTrackingConnector>());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField(theField), CancellationToken.None)).Single();

            Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { WhatGatesIt }));
        }

        /// <summary>
        /// One Feature that carries the named link is enough: the Portfolio has dependencies, so nothing
        /// went unmatched and the refresh says nothing.
        /// </summary>
        [Test]
        public async Task A_named_link_that_one_Feature_carries_is_not_told_as_matching_nothing()
        {
            var subject = AnInstanceDefining(
                [Dependency, Relates],
                [
                    JiraWireFormat.AnEpic(TheFeature, Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                    JiraWireFormat.AnEpic(WhatGatesIt, Relates.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                ],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.SelectMany(feature => feature.RefreshFindings), Is.Empty);
                Assert.That(WhatItWaitsOn(features.Single(feature => feature.ReferenceId == TheFeature)), Is.EqualTo(new List<string> { WhatItBlocks }));
            }
        }

        /// <summary>
        /// Having no dependencies is the ordinary case, so Features that carry no links at all are told
        /// nothing about the named link.
        /// </summary>
        [Test]
        public async Task Features_carrying_no_links_at_all_are_told_nothing_about_the_named_link()
        {
            var subject = AnInstanceDefining(
                [StockBlocks, Dependency, Relates],
                [JiraWireFormat.AnEpic(TheFeature), JiraWireFormat.AnEpic(WhatItBlocks)],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None);

            Assert.That(features.SelectMany(feature => feature.RefreshFindings), Is.Empty);
        }

        /// <summary>
        /// A Portfolio reading a field of its own has deliberately stopped reading links, so its links are
        /// neither a finding nor a log line, even when its Features carry links that read nothing.
        /// </summary>
        [Test]
        public async Task A_Dependency_Field_naming_an_ordinary_field_says_nothing_about_the_links_its_Features_carry()
        {
            var log = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AnInstanceDefiningFieldsAndLinkTypes(
                ["Blocked By Keys"], [Relates], [JiraWireFormat.AnEpic(TheFeature, Relates.LinkWhoseInwardIssueIs(WhatBlocksIt))], log);

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Blocked By Keys"), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.SelectMany(feature => feature.RefreshFindings), Is.Empty);
                Assert.That(log.Warnings, Is.Empty);
            }
        }

        /// <summary>
        /// A link type's own name says nothing about which end waits, on either Feature, and the finding
        /// names the setting, what was typed and the two phrases to choose between.
        /// </summary>
        [Test]
        public async Task A_link_types_own_name_reads_nothing_on_either_end_and_the_finding_names_both_phrases()
        {
            var subject = AnInstanceDefining(
                [Dependency],
                [
                    JiraWireFormat.AnEpic(TheFeature, Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                    JiraWireFormat.AnEpic(WhatItBlocks, Dependency.LinkWhoseInwardIssueIs(TheFeature)),
                ],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Dependency"), CancellationToken.None);
            var findings = features.SelectMany(feature => feature.RefreshFindings).Distinct().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.SelectMany(WhatItWaitsOn), Is.Empty);
                Assert.That(findings, Has.Count.EqualTo(1), "One finding per refresh, never one per Feature.");
                Assert.That(findings[0].Kind, Is.EqualTo(RefreshFindingKind.NamedLinkPointsBothWays));
                Assert.That(findings[0].Setting, Is.EqualTo(RefreshFindingSetting.DependencyField));
                Assert.That(findings[0].Named, Is.EqualTo("Dependency"));
                Assert.That(findings[0].Seen, Is.EquivalentTo(TheTwoPhrasesOfTheDependencyType));
            }
        }

        /// <summary>
        /// "relates to" is what both ends of the stock Relates type read, so it points nowhere either.
        /// </summary>
        [Test]
        public async Task A_phrase_both_ends_of_a_link_type_share_reads_nothing_and_says_so()
        {
            var subject = AnInstanceDefining(
                [Relates],
                [
                    JiraWireFormat.AnEpic(TheFeature, Relates.LinkWhoseOutwardIssueIs(WhatItBlocks)),
                    JiraWireFormat.AnEpic(WhatItBlocks, Relates.LinkWhoseInwardIssueIs(TheFeature)),
                ],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("relates to"), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.SelectMany(WhatItWaitsOn), Is.Empty);
                Assert.That(
                    features.SelectMany(feature => feature.RefreshFindings).Select(finding => finding.Kind).Distinct(),
                    Is.EqualTo(new List<RefreshFindingKind> { RefreshFindingKind.NamedLinkPointsBothWays }));
            }
        }

        /// <summary>
        /// An instance renamed the inward end of Blocks to "waits for", and it is typed with capitals: the
        /// same result "is blocked by" gives on a stock instance.
        /// </summary>
        [Test]
        public async Task A_renamed_phrase_typed_in_any_case_is_read_like_the_stock_one()
        {
            var renamed = new JiraLinkType("Blocks", "waits for", "blocks");
            var subject = AnInstanceDefining(
                [renamed],
                [
                    JiraWireFormat.AnEpic(TheFeature, renamed.LinkWhoseInwardIssueIs(WhatBlocksIt)),
                    JiraWireFormat.AnEpic(WhatBlocksIt, renamed.LinkWhoseOutwardIssueIs(TheFeature)),
                ],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var features = await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Waits For"), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatItWaitsOn(features.Single(feature => feature.ReferenceId == TheFeature)), Is.EqualTo(new List<string> { WhatBlocksIt }));
                Assert.That(WhatItWaitsOn(features.Single(feature => feature.ReferenceId == WhatBlocksIt)), Is.Empty);
            }
        }

        /// <summary>
        /// Field lookup runs first, so a field that carries the very name of a link phrase is read as the
        /// field and no reference that read a field before changes meaning.
        /// </summary>
        [Test]
        public async Task A_real_field_carrying_the_name_of_a_link_phrase_is_read_as_the_field()
        {
            var subject = AnInstanceDefiningFieldsAndLinkTypes(
                ["depends on"],
                [Dependency],
                [JiraWireFormat.AnIssueCarrying(
                    TheFeature, "Epic", $",\"{JiraWireFormat.TheCustomFieldIdAt(0)}\":\"{WhatGatesIt}\"", Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks))],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None)).Single();

            Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { WhatGatesIt }));
        }

        /// <summary>
        /// Naming a link type replaces "is blocked by" for that Portfolio; reading both would leave nobody
        /// able to say where an edge came from.
        /// </summary>
        [Test]
        public async Task Naming_a_link_replaces_is_blocked_by_for_that_Portfolio()
        {
            var subject = AnInstanceDefining(
                [Dependency, StockBlocks],
                JiraWireFormat.AnEpic(TheFeature, Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks), StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt)));

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("depends on"), CancellationToken.None)).Single();

            Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { WhatItBlocks }));
        }

        /// <summary>
        /// A Portfolio naming nothing reads "is blocked by" and ignores the other links, as it always did.
        /// </summary>
        [Test]
        public async Task A_Portfolio_naming_nothing_still_reads_is_blocked_by()
        {
            var subject = AnInstanceDefining(
                [Dependency, StockBlocks],
                JiraWireFormat.AnEpic(TheFeature, Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks), StockBlocks.LinkWhoseInwardIssueIs(WhatBlocksIt)));

            var feature = (await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud(), CancellationToken.None)).Single();

            Assert.That(WhatItWaitsOn(feature), Is.EqualTo(new List<string> { WhatBlocksIt }));
        }

        /// <summary>
        /// A Dependency Field naming an ordinary field reads the keys typed into it, whatever the Feature's
        /// links say, and records that they came from the Portfolio's field.
        /// </summary>
        [Test]
        public async Task A_Dependency_Field_naming_an_ordinary_field_reads_its_keys_and_not_the_links()
        {
            var subject = AnInstanceDefiningFieldsAndLinkTypes(
                ["Blocked By Keys"],
                [Dependency],
                [JiraWireFormat.AnIssueCarrying(
                    TheFeature, "Epic", $",\"{JiraWireFormat.TheCustomFieldIdAt(0)}\":\"{WhatGatesIt}; {WhatBlocksIt}\"", Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks))],
                new RecordingLogger<JiraWorkTrackingConnector>());

            var feature = (await subject.GetFeaturesForProject(APortfolioNamingAsItsDependencyField("Blocked By Keys"), CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatItWaitsOn(feature), Is.EquivalentTo(new List<string> { WhatGatesIt, WhatBlocksIt }));
                Assert.That(feature.DependsOnReferences.Select(reference => reference.Source).Distinct(), Is.EqualTo(ReadFromThePortfoliosField));
            }
        }

        /// <summary>
        /// The links already arrive with every Feature, so naming one costs the instance nothing. The
        /// baseline is the same Portfolio on the same connection, with the link registered and not named.
        /// </summary>
        [Test]
        public async Task Naming_a_link_costs_the_Jira_instance_no_extra_request()
        {
            var feature = JiraWireFormat.AnEpic(TheFeature, Dependency.LinkWhoseOutwardIssueIs(WhatItBlocks));
            var askedWithTheLinkNamed = new List<string>();
            var askedWithNothingNamed = new List<string>();
            var named = APortfolioNamingAsItsDependencyField("depends on");
            var notNamed = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            notNamed.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = TheDependencyFieldId,
                DisplayName = "depends on",
                Reference = "depends on",
            });

            await AnInstanceCountingItsRequests([Dependency], feature, askedWithTheLinkNamed).GetFeaturesForProject(named, CancellationToken.None);
            await AnInstanceCountingItsRequests([Dependency], feature, askedWithNothingNamed).GetFeaturesForProject(notNamed, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(askedWithNothingNamed, Is.Not.Empty, "positive control: the refresh naming nothing reached Jira at all");
                Assert.That(askedWithTheLinkNamed, Is.EqualTo(askedWithNothingNamed),
                    "A refresh naming the link asks Jira for no more than the same refresh naming nothing.");
            }
        }

        private static JiraWorkTrackingConnector AnInstanceCountingItsRequests(JiraLinkType[] linkTypes, string issue, List<string> asked)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                asked.Add(path);

                return path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    ? JiraWireFormat.TheLinkTypeListDefining(linkTypes.Select((type, index) => JiraWireFormat.ALinkTypeDefinition(index, type)))
                    : AnInstanceAnswering(request, [issue]);
            }), new RecordingLogger<JiraWorkTrackingConnector>());

        private static JiraWorkTrackingConnector AnInstanceDefiningFieldsAndLinkTypes(
            string[] fields, JiraLinkType[] linkTypes, string[] issues, RecordingLogger<JiraWorkTrackingConnector> log)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal))
                {
                    return JiraWireFormat.TheFieldListDefining(fields);
                }

                return path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    ? JiraWireFormat.TheLinkTypeListDefining(linkTypes.Select((type, index) => JiraWireFormat.ALinkTypeDefinition(index, type)))
                    : AnInstanceAnswering(request, issues);
            }), log);

        private static JiraWorkTrackingConnector AnInstanceAnsweringTheLinkTypeListWith(HttpStatusCode status, string[] issues)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.AnsweringWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                return path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    ? new HttpResponseMessage(status) { Content = new StringContent("{}") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AnInstanceAnswering(request, issues)) };
            }));

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

            portfolio.OwnDependencyField(TheDependencyFieldId);

            return portfolio;
        }

        private static JiraWorkTrackingConnector AnInstanceDefining(JiraLinkType[] linkTypes, string issue)
            => AnInstanceDefining(linkTypes, issue, new RecordingLogger<JiraWorkTrackingConnector>());

        private static JiraWorkTrackingConnector AnInstanceDefining(JiraLinkType[] linkTypes, string issue, RecordingLogger<JiraWorkTrackingConnector> log)
            => AnInstanceDefining(linkTypes, [issue], log);

        private static JiraWorkTrackingConnector AnInstanceDefining(JiraLinkType[] linkTypes, string[] issues, RecordingLogger<JiraWorkTrackingConnector> log)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal))
                {
                    return JiraWireFormat.TheLinkTypeListDefining(linkTypes.Select((type, index) => JiraWireFormat.ALinkTypeDefinition(index, type)));
                }

                return AnInstanceAnswering(request, issues);
            }), log);

        private static string AnInstanceAnswering(HttpRequestMessage request, string[] issues)
            => request.RequestUri?.AbsolutePath.EndsWith(JiraWireFormat.CredentialCheckEndpoint, StringComparison.Ordinal) == true
                ? "{\"accountId\":\"someone\"}"
                : JiraWireFormat.ACloudResponseTo(request, issues);
    }
}
