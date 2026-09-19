using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Acceptance scenarios - Slice 01: naming a link type where a field name goes. An Additional Field's
    /// reference is free text that Lighthouse resolves against the instance's field list; this gives it a
    /// second place to look, and the second place is consulted only for a reference the field list did not
    /// resolve. Driving port: connection validation, which is the door the administrator actually presses.
    ///
    /// Field lookup keeps precedence, so an instance carrying a field and a link type of one name reads
    /// exactly as it did before and nothing an administrator has already configured changes meaning
    /// underneath them.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6028-parent-from-issue-links")]
    [Category("slice-01")]
    public partial class Slice01LinkTypeReferenceTest
    {
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

        [Test]
        public async Task A_name_that_is_neither_a_field_nor_a_link_type_still_fails()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "A reference matching neither a field nor a link type resolves to nothing, and a connection resting on it cannot be called valid.");
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"),
                    "Giving the reference a second place to look must not change how a reference that finds neither place is reported.");
                Assert.That(verdict.Message, Does.Contain(ATypoForALinkType),
                    $"The administrator has to be told which reference went nowhere. Jira said: {verdict.Message}");
            }
        }

        [Test]
        public async Task The_failure_names_the_link_types_the_instance_does_define()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Message, Does.Contain(ALinkType),
                    $"Someone who mistyped a link type name has to be able to correct the spelling from the error text, which means reading the real names in it. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Contain(AnotherLinkType),
                    $"Listing only some of the types would leave an administrator concluding a type they can see in Jira is not there. Jira said: {verdict.Message}");
            }
        }

        [Test]
        public async Task An_empty_link_type_list_does_not_read_as_a_name_nobody_defined()
        {
            var whenTheInstanceListsItsTypes = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            TheLinkTypeListComesBackEmpty();
            var whenTheListComesBackEmpty = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whenTheListComesBackEmpty.Message, Does.Contain(ATypoForALinkType),
                    $"The reference still went nowhere and still has to be named. Jira said: {whenTheListComesBackEmpty.Message}");
                Assert.That(whenTheListComesBackEmpty.Message, Is.Not.EqualTo(whenTheInstanceListsItsTypes.Message),
                    "An empty list is not a short list. Reporting it the same way tells an administrator their Jira defines no link types, which is what an unaccepted credential looks like from here.");
            }
        }

        [Test]
        public async Task An_empty_link_type_list_points_at_the_credential_not_at_the_configuration()
        {
            var whenTheInstanceListsItsTypes = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            TheLinkTypeListComesBackEmpty();
            var whenTheListComesBackEmpty = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whenTheListComesBackEmpty.Message, Does.Contain("credential").IgnoreCase,
                    $"The likely cause is a credential Jira will not show link types to, and an administrator sent to the field configuration instead spends the afternoon fixing something that was never wrong. Jira said: {whenTheListComesBackEmpty.Message}");
                Assert.That(whenTheListComesBackEmpty.TechnicalDetails, Is.Not.EqualTo(whenTheInstanceListsItsTypes.TechnicalDetails),
                    "The advice that fixes a typo is the wrong advice for a credential, so the two cannot carry the same next step.");
            }
        }

        [Test]
        public async Task A_credential_Jira_refuses_is_answered_before_any_reference_is_judged()
        {
            TheCredentialIsRefused();

            var verdict = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("authentication_failed"),
                    $"A credential Jira will not accept is the thing to fix, and it is the thing the administrator has to read. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Not.Contain(ATypoForALinkType),
                    $"Nothing downstream of the credential check can be trusted to have seen this instance's real fields, so naming a reference as missing here accuses the administrator of a mistake nobody has established. Jira said: {verdict.Message}");
                Assert.That(WhenTheFirstRequestReached(FieldListEndpoint), Is.Negative,
                    "A refused credential ends validation, so nothing goes on to ask what this instance defines.");
            }
        }

        /// <summary>
        /// The credential check is the only call that establishes who Lighthouse is signed in as, and an
        /// empty link type list is meaningless before it has run - Jira answers an unaccepted credential
        /// with 200 and nothing in it. Reordering the two would silently turn "your credential is wrong"
        /// into "you invented a link type", which is why the order is pinned here rather than described.
        /// </summary>
        [Test]
        public async Task The_credential_is_checked_before_the_instance_is_asked_what_it_defines()
        {
            await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            var theCredentialCheck = WhenTheFirstRequestReached(CredentialCheckEndpoint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theCredentialCheck, Is.Not.Negative,
                    "Validating a connection has to establish who it is signed in as at all.");
                Assert.That(theCredentialCheck, Is.LessThan(WhenTheFirstRequestReached(FieldListEndpoint)),
                    "A field list read before the credential is established cannot be told apart from one an unaccepted credential was shown.");
                Assert.That(theCredentialCheck, Is.LessThan(WhenTheFirstRequestReached(IssueLinkTypeEndpoint)),
                    "An empty link type list only means this instance defines none once the credential behind it is known to be accepted.");
            }
        }

        [Test]
        public async Task The_link_type_endpoint_refusing_is_not_a_reference_that_could_not_be_found()
        {
            TheIssueLinkTypeEndpointAnswers(HttpStatusCode.Forbidden);

            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "A reference that could not be checked at all is not a reference that checked out.");
                Assert.That(verdict.Message, Does.Contain(IssueLinkTypeEndpoint),
                    $"Jira refusing one endpoint is something an administrator can act on, and only naming it says which one. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Not.Contain("could not be found"),
                    $"This instance does define the reference; the list saying so was refused. Reporting it as not found sends the administrator to correct a configuration that is already right. Jira said: {verdict.Message}");
            }
        }

        [Test]
        public async Task The_refused_endpoint_is_reported_under_its_own_code_and_says_what_it_refused_with()
        {
            TheIssueLinkTypeEndpointAnswers(HttpStatusCode.Forbidden);

            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("issue_link_types_unreadable"),
                    $"An endpoint Jira turned down is its own outcome, and the code is what anything reading this verdict programmatically has to tell it apart by. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Contain("403"),
                    $"A 401, a 403 and a 502 send the administrator to three different places, and only the status says which one arrived. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Contain(nameof(HttpStatusCode.Forbidden)),
                    $"A number alone is something to go and look up; the name it carries is readable on sight. Jira said: {verdict.Message}");
            }
        }

        [Test]
        public async Task The_two_verdicts_that_suspect_the_credential_highlight_the_credential()
        {
            TheIssueLinkTypeEndpointAnswers(HttpStatusCode.Forbidden);
            var whenTheEndpointWasRefused = await TheVerdictOnAConnectionAskingFor(ALinkType);

            TheIssueLinkTypeEndpointAnswers(HttpStatusCode.OK);
            TheLinkTypeListComesBackEmpty();
            var whenTheListCameBackEmpty = await TheVerdictOnAConnectionAskingFor(ATypoForALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whenTheEndpointWasRefused.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.ApiToken),
                    "The message sends someone to the credential, and the input the form highlights has to be the one the message names or they read past each other.");
                Assert.That(whenTheListCameBackEmpty.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.ApiToken),
                    "An empty list points at the credential too, so the same input has to light up - pointing at the Additional Fields box would contradict the sentence above it.");
            }
        }

        [TestCase(AnEnvelopeThatDoesNotCarryTheList)]
        [TestCase(ABareArrayOfLinkTypes)]
        [TestCase(AnAnswerThatIsNotJsonAtAll)]
        public async Task A_reply_Lighthouse_cannot_read_is_not_a_credential_it_cannot_use(string answered)
        {
            TheLinkTypeListComesBackAs(answered);

            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "A reference the link type list was going to answer for has not been checked when the list could not be read.");
                Assert.That(verdict.Code, Is.EqualTo("issue_link_types_not_understood"),
                    $"A reply in a shape Lighthouse cannot read is neither a refusal nor an empty list, and reusing either code hides the one cause that needs a different remedy from both. Jira said: {verdict.Message}");
                Assert.That(verdict.Message, Does.Contain(IssueLinkTypeEndpoint),
                    $"Naming the endpoint is what lets someone go and look at what it actually answers on their deployment. Jira said: {verdict.Message}");
                Assert.That(verdict.Message + verdict.TechnicalDetails, Does.Not.Contain("credential").IgnoreCase,
                    $"Nothing here established anything about the credential, and an administrator told otherwise spends the afternoon revoking and reissuing a token that was never the problem. Jira said: {verdict.Message}");
                Assert.That(verdict.FieldName, Is.Not.EqualTo(JiraWorkTrackingOptionNames.ApiToken),
                    "The highlighted input is where the form sends someone to fix this, and the token is not what is broken.");
            }
        }

        [Test]
        public async Task A_type_with_no_name_and_a_type_with_no_labels_are_not_types_a_blank_reference_reaches()
        {
            TheInstanceDefinesALinkTypeCarryingNoDirectionalLabels(ATypeCarryingNoDirectionalLabels);
            TheInstanceDefinesALinkTypeCarryingNoName("is duplicated by", "duplicates");
            TheInstanceDefinesALinkTypeNamedTheEmptyString("is blocked by", "blocks");
            TheInstanceDefinesTheLinkType(ATypeDefinedAfterTheOnesMissingTheirNames, "is cloned by", "clones");

            var theReferenceNamingARealType = await TheVerdictOnAConnectionAskingFor(ALinkType);
            var theReferenceNamingNothing = await TheVerdictOnAConnectionAskingFor(AReferenceTypedAsNothingAtAll);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theReferenceNamingARealType.IsValid, Is.True,
                    $"Entries this instance left incomplete must not stop a reference naming a complete one from resolving. Jira said: {theReferenceNamingARealType.Message}");
                Assert.That(theReferenceNamingNothing.IsValid, Is.False,
                    $"A link type with nothing written in either direction carries two empty labels, and an empty reference must not be read as matching them - that resolves a box the administrator never filled in. Jira said: {theReferenceNamingNothing.Message}");
                Assert.That(theReferenceNamingNothing.Message, Does.Not.Contain(", ,"),
                    $"A type that arrived with no name would appear in the list of names to correct a typo to as a gap between two commas, offering something nobody can type. Jira said: {theReferenceNamingNothing.Message}");
                Assert.That(theReferenceNamingNothing.Message, Does.Contain(ATypeDefinedAfterTheOnesMissingTheirNames),
                    $"Skipping an unusable entry must not stop the list at it - the types defined after one are still types the administrator can see in Jira. Jira said: {theReferenceNamingNothing.Message}");
            }
        }
    }
}
