using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Naming a link type where a field name goes. An Additional Field's reference is free text that
    /// Lighthouse resolves against the instance's field list; this gives it a second place to look, and
    /// the second place is consulted only for a reference the field list did not resolve.
    ///
    /// Field lookup keeps precedence, so an instance carrying a field and a link type of one name reads
    /// exactly as it did before and nothing an administrator has already configured changes meaning
    /// underneath them.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6028-parent-from-issue-links")]
    [Category("slice-01")]
    public class Slice01LinkTypeReferenceTest : ParentFromIssueLinksAcceptanceTest
    {
        private const string ALinkType = "Caused by";

        private const string TheLabelALinkTypeCarriesInward = "was caused by";

        private const string TheLabelALinkTypeCarriesOutward = "causes";

        /// <summary>
        /// A live instance ships "Relates" reading "relates to" in both directions, so a reference typed
        /// that way reaches this one type twice over.
        /// </summary>
        private const string AnotherLinkType = "Relates";

        private const string TheLabelAnotherLinkTypeUsesBothWays = "relates to";

        private const string ACustomField = "Story Points";

        private const string ANameTheInstanceUsesForBoth = "Blocks";

        /// <summary>
        /// Each of a link type's three labels can be renamed on its own, so nothing stops an instance
        /// from ending up with one phrase reading against two different types.
        /// </summary>
        private const string ALabelTwoTypesAnswerTo = "duplicates";

        [SetUp]
        public void DescribeTheInstanceEveryScenarioStartsFrom()
        {
            TheInstanceDefinesTheLinkType(ALinkType, TheLabelALinkTypeCarriesInward, TheLabelALinkTypeCarriesOutward);
            TheInstanceDefinesTheLinkType(AnotherLinkType, TheLabelAnotherLinkTypeUsesBothWays, TheLabelAnotherLinkTypeUsesBothWays);
            TheInstanceDefinesTheCustomField(ACustomField);
        }

        [Test]
        public async Task A_reference_naming_a_link_type_validates()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True,
                    $"An Additional Field naming a link type the instance defines must validate. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Not.Contain(ALinkType),
                    "A link type the instance defines must not be reported among the fields that could not be found.");
            }
        }

        [Test]
        public async Task A_field_wins_over_a_link_type_of_the_same_name()
        {
            TheInstanceDefinesTheCustomField(ANameTheInstanceUsesForBoth);
            TheInstanceDefinesTheLinkType(ANameTheInstanceUsesForBoth, "is blocked by", "blocks");

            var verdict = await TheVerdictOnAConnectionAskingFor(ANameTheInstanceUsesForBoth);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True,
                    $"A reference matching a real field must still resolve. Jira said: {verdict.Message}");
                Assert.That(RequestsReaching(IssueLinkTypeEndpoint), Is.Zero,
                    "The field list resolved the reference, so nothing was left to look for among the link types.");
            }
        }

        [Test]
        public async Task No_unresolved_reference_means_no_link_type_call_at_all()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ACustomField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True,
                    $"A reference naming a custom field the instance defines must validate. Jira said: {verdict.Message}");
                Assert.That(RequestsReaching(IssueLinkTypeEndpoint), Is.Zero,
                    "An instance with nothing left unresolved must not pay for the link-type lookup.");
            }
        }

        [Test]
        public async Task The_link_type_list_is_read_once_however_many_references_need_it()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkType, AnotherLinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True,
                    $"Two references naming link types the instance defines must both resolve. Jira said: {verdict.Message}");
                Assert.That(RequestsReaching(IssueLinkTypeEndpoint), Is.EqualTo(1),
                    "The link types are one list, so validating a connection reads it once however many references need it.");
            }
        }

        [TestCase("Caused by")]
        [TestCase("caused by")]
        [TestCase("CAUSED BY")]
        public async Task The_types_name_matches_whatever_the_case(string typed)
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(typed);

            Assert.That(verdict.IsValid, Is.True,
                $"An administrator reading a link type name off their Jira screen types the case they see, and every casing of it names the same type. Jira said: {verdict.Message}");
        }

        [TestCase(ALinkType)]
        [TestCase(TheLabelALinkTypeCarriesInward)]
        [TestCase(TheLabelALinkTypeCarriesOutward)]
        public async Task Any_of_the_three_labels_identifies_the_same_type(string typed)
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(typed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True,
                    $"A link type shows an administrator three labels - its name and the phrase it reads in each direction - and any of them identifies it. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Not.Contain(typed),
                    "A label the instance defines must not be reported among the fields that could not be found.");
            }
        }

        [TestCase("WAS CAUSED BY")]
        [TestCase("CAUSES")]
        public async Task Case_is_ignored_on_the_directional_labels_too(string typed)
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(typed);

            Assert.That(verdict.IsValid, Is.True,
                $"Case is ignored on all three labels, not only on the name. Jira said: {verdict.Message}");
        }

        [Test]
        public async Task A_label_a_type_reads_both_ways_reaches_that_one_type()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(TheLabelAnotherLinkTypeUsesBothWays);

            Assert.That(verdict.IsValid, Is.True,
                $"Reaching one type through both of its directional labels is still one type, and a correctly configured instance must not read as though it named two. Jira said: {verdict.Message}");
        }

        [Test]
        public async Task A_label_two_different_types_answer_to_resolves_to_neither()
        {
            TheInstanceDefinesTheLinkType("Duplicate", "is duplicated by", ALabelTwoTypesAnswerTo);
            TheInstanceDefinesTheLinkType("Cloners", ALabelTwoTypesAnswerTo, "clones");

            var verdict = await TheVerdictOnAConnectionAskingFor(ALabelTwoTypesAnswerTo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "Two different link types answering to one label leaves it genuinely undecided, and picking one of them would silently read the wrong links.");
                Assert.That(verdict.Message, Does.Contain(ALabelTwoTypesAnswerTo),
                    $"The administrator has to be told which reference went nowhere. Jira said: {verdict.Message}");
            }
        }
    }
}
