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

        private const string AnotherLinkType = "Relates to";

        private const string ACustomField = "Story Points";

        private const string ANameTheInstanceUsesForBoth = "Blocks";

        [SetUp]
        public void DescribeTheInstanceEveryScenarioStartsFrom()
        {
            TheInstanceDefinesTheLinkType(ALinkType, "was caused by", "causes");
            TheInstanceDefinesTheLinkType(AnotherLinkType, "relates to", "relates to");
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
    }
}
