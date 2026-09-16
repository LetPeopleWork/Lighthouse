using System.Net;

using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// The whole-query fetch, when the tracker will not answer it.
    ///
    /// Removal is a set difference against what the query returned, so a fetch that answers with no records
    /// deletes every record the team or portfolio holds. That is the right answer when the query genuinely
    /// matches nothing and a catastrophic one when the fetch merely failed - and the two are the same value.
    /// So the fetch has to refuse: the cycle fails, one team's update is skipped, and the records stay.
    ///
    /// What the failed cycle costs is one team, not the refresh - every entity's update is wrapped
    /// individually - and the fetch runs before anything is written, so a refusal leaves the stored data
    /// untouched rather than half-updated.
    ///
    /// The tracker's own failure is what has to arrive, unwrapped: an operator reading the log needs to see
    /// the area path Azure DevOps refused, not that something somewhere went wrong.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsFetchRefusalTest
    {
        private const int TheOnlyItem = 1;

        private const string TheFieldListVerdict = "field_list_unreadable";

        private const string WhatTheFieldListVerdictSays = "could not read the list of fields";

        private const string TheAdditionalFieldsInput = "Additional Fields";

        private const string TheReferenceTheConfigurationNames = "Custom.StoryPoints";

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<VssServiceException>(),
                "An expired token or a timeout is not the tracker saying the query matches nothing. Answering "
                + "with no records hands removal an empty query, which deletes every Work Item the team has.");
        }

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenTheFieldLookupFails()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<AzureDevOpsReadException>(),
                "The field lookup runs after the query already succeeded and before any payload is read, so "
                + "its failure is invisible to anything watching the query - and empties the team just the same.");
        }

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenTheTrackerAnswersWithoutAResultSet()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.AnswerTheQueryWithoutAResultSet = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>(),
                "No result set is not the same answer as an empty one, and the difference between them is "
                + "every record the team has.");
        }

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenAPayloadBatchIsRejected()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectPayloadReads = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<VssServiceException>(),
                "Azure DevOps fails a whole batch over one id deleted since the query ran. Reading that as an "
                + "empty query deletes the other one hundred and ninety-nine records in the batch too.");
        }

        [Test]
        public void GetWorkItemsForTeam_ByReferenceId_RefusesWhenTheFieldLookupFails()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, [$"{TheOnlyItem}"], CancellationToken.None),
                Throws.TypeOf<AzureDevOpsReadException>(),
                "The keyed fetch is where the cheap refresh sends its traffic. A failure it answers with no "
                + "records reports the moved items as gone.");
        }

        /// <summary>
        /// This fetch looks work items up by id and never issues a query, so it is the path on which a
        /// verdict that described the connection as freshly proven would have been untrue. The refusal it
        /// carries is read by an operator the same way the one on the connection screen is, so it is
        /// asserted the same way.
        /// </summary>
        [Test]
        public void GetWorkItemsForTeam_ByReferenceId_CarriesTheSameFieldListVerdictTheConnectionScreenShows()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            var refusal = Assert.ThrowsAsync<AzureDevOpsReadException>(
                async () => await subject.GetWorkItemsForTeam(team, [$"{TheOnlyItem}"], CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal!.Verdict.Code, Is.EqualTo(TheFieldListVerdict));
                Assert.That(refusal.Verdict.Message, Does.Contain(WhatTheFieldListVerdictSays));
                Assert.That(refusal.Verdict.FieldName, Is.EqualTo(TheAdditionalFieldsInput));
            }
        }

        [Test]
        public void GetFeaturesForProject_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetFeaturesForProject(portfolio, CancellationToken.None),
                Throws.TypeOf<VssServiceException>(),
                "On the portfolio half an empty answer strips every Feature's portfolio claim, and the "
                + "orphaned-Feature cleanup then deletes outright whatever no portfolio still claims.");
        }

        [Test]
        public void GetParentFeaturesDetails_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetParentFeaturesDetails(portfolio, [$"{TheOnlyItem}"], CancellationToken.None),
                Throws.TypeOf<VssServiceException>(),
                "Parent Features are fetched by the same swallowing path, and a parent that comes back empty "
                + "is one every child Feature stops being able to name.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_StillAnswersWithNoRecordsWhenTheQueryGenuinelyMatchesNothing()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds();

            var workItems = await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(workItems, Is.Empty,
                "The distinction is the whole point. A team whose query really matches nothing has to keep "
                + "answering with nothing, or removal never runs and departed items live forever.");
        }

        [Test]
        public async Task ValidateTeamSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            var result = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Telling an operator whose token just expired to check their query, work item types and "
                    + "mapped states sends them to rewrite a configuration that was never wrong.");
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            var result = await subject.ValidatePortfolioSettings(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Same on the portfolio half: 'no features found' names a configuration problem, and a "
                    + "failed round trip is not one.");
            }
        }

        [Test]
        public async Task ValidateConnection_NamesTheAdditionalFieldsInputWhenAzureDevOpsWillNotHandOverItsFieldList()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.RejectTheFieldLookup = true;

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo(TheFieldListVerdict));
                Assert.That(result.Message, Does.Contain(WhatTheFieldListVerdictSays));
                Assert.That(result.FieldName, Is.EqualTo(TheAdditionalFieldsInput),
                    "The additional fields are the input the administrator can act on. Naming the connection "
                    + "settings instead sends them to rewrite a URL and a token that had just answered a query.");
                Assert.That(result.TechnicalDetails, Does.Contain("The field definitions could not be read."),
                    "Azure DevOps's own sentence is the only thing that tells one refusal from another.");
            }
        }

        [Test]
        public async Task ValidateConnection_DoesNotBlameTheTokenWhenTheFieldListIsTheThingThatCameBackUnauthorised()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.RejectTheFieldLookup = true;
            ado.HowTheFieldLookupIsRefused = AChallengedRefusal();

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(TheFieldListVerdict));
                Assert.That(result.Message, Does.Contain(WhatTheFieldListVerdictSays));
                Assert.That(result.FieldName, Is.EqualTo(TheAdditionalFieldsInput),
                    "dev.azure.com challenges a 401, which is what turns this into an authorisation exception. "
                    + "Reading that as a bad token sends the administrator to replace a credential that had "
                    + "signed in successfully one call earlier.");
            }
        }

        [Test]
        public async Task ValidateConnection_CarriesTheStatusWhenAzureDevOpsAnsweredWithABodyItsOwnErrorContractDoesNotRecognise()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.RejectTheFieldLookup = true;
            ado.HowTheFieldLookupIsRefused = AProxyPageInsteadOfAnError(HttpStatusCode.Forbidden);

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(TheFieldListVerdict));
                Assert.That(result.Message, Does.Contain("403 (Forbidden)"),
                    "A proxy page carries no error contract, so the exception message is the bare status name. "
                    + "The number survives on the exception alone, and the word Forbidden by itself tells "
                    + "nobody what answered.");
                Assert.That(result.FieldName, Is.EqualTo(TheAdditionalFieldsInput));
            }
        }

        [Test]
        public async Task ValidateConnection_StillReportsAnAzureDevOpsItCouldNotReachAgainstTheUrl()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.RejectTheFieldLookup = true;
            ado.HowTheFieldLookupIsRefused = new HttpRequestException("No such host is known. (dev.azure.com:443)");

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
                Assert.That(result.Message, Does.Contain("Could not reach Azure DevOps with the provided URL."));
                Assert.That(result.FieldName, Is.EqualTo(AzureDevOpsWorkTrackingOptionNames.Url),
                    "A transport failure is the one case where the URL really is what the administrator should "
                    + "look at, and it stays that way after the field-list refusal stops being reported as one.");
            }
        }

        [Test]
        public async Task ValidateConnection_StillReportsARefusedCredentialAgainstTheToken()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.RejectTheQuery = true;
            ado.HowTheQueryIsRefused = AChallengedRefusal();

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("authentication_failed"));
                Assert.That(result.Message, Does.Contain("Authentication failed for Azure DevOps."));
                Assert.That(result.FieldName, Is.EqualTo(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken),
                    "The first call a validation makes is a work item query. A credential refused there has "
                    + "proved nothing, and the token is exactly what to go and check.");
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_ReportsARefusedFieldListAsOneRatherThanAsAnUnexpectedError()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            var result = await subject.ValidatePortfolioSettings(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(TheFieldListVerdict),
                    "Validating a portfolio reads the field list too, once its query matched something. "
                    + "An unexpected error is the same dead end there as on the connection screen.");
                Assert.That(result.Message, Does.Contain(WhatTheFieldListVerdictSays));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_ReadsTheFieldWhoseReferenceNameMatchesWhenAnotherIsDisplayedUnderThatName()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            AskFor(TheReferenceTheConfigurationNames, team.WorkTrackingSystemConnection);
            ado.FieldsTheOrganisationHolds.AddRange([
                AField("Story Points", TheReferenceTheConfigurationNames),
                AField(TheReferenceTheConfigurationNames, "Custom.SomethingElse"),
            ]);

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.FieldsOfTheItemRead, Does.Contain(TheReferenceTheConfigurationNames),
                "A field created through the REST API may be displayed under a name that is another field's "
                + "reference name, which the portal would not allow. Two matches are not an error to fail the "
                + "refresh over - the reference name is the exact identifier, so it is the one that wins.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_TellsTheOperatorWhichAdditionalFieldTheOrganisationDoesNotHave()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            AskFor(TheReferenceTheConfigurationNames, team.WorkTrackingSystemConnection);
            ado.FieldsTheOrganisationHolds.Add(AFieldTheOrganisationDoesHold());

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.Log.Warnings, Has.Some.Contains(TheReferenceTheConfigurationNames),
                "Nothing else reports this on a refresh. The field is simply never fetched, on every cycle, "
                + "for as long as the configuration carries it, and the log line is the only trace of it.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_StillReturnsItsWorkItemsWhenAnAdditionalFieldMatchesNothing()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            AskFor(TheReferenceTheConfigurationNames, team.WorkTrackingSystemConnection);
            ado.FieldsTheOrganisationHolds.Add(AFieldTheOrganisationDoesHold());

            var workItems = await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(workItems.Select(workItem => workItem.ReferenceId), Does.Contain($"{TheOnlyItem}"),
                "A stale field reference in a saved configuration is an ordinary mistake. Refusing the refresh "
                + "over one would turn a cosmetic problem into an outage for every team on that connection.");
        }

        [Test]
        public async Task ValidateConnection_StillReportsAnAdditionalFieldThatMatchesNothingInTheOrganisation()
        {
            var (subject, connection, ado) = AnAzureDevOpsConnectionAskingForAnAdditionalField();
            ado.FieldsTheOrganisationHolds.Add(AFieldTheOrganisationDoesHold());

            var result = await subject.ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("additional_fields_invalid"));
                Assert.That(result.Message, Does.Contain("Microsoft.VSTS.Scheduling.StoryPoints"),
                    "The administrator can only fix the field they are told about, so a reference that "
                    + "resolves to nothing has to keep reaching the connection screen by name.");
                Assert.That(result.FieldName, Is.EqualTo(TheAdditionalFieldsInput));
            }
        }

        private static void AskFor(string reference, WorkTrackingSystemConnection connection)
            => connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = "Story Points",
                Reference = reference,
            });

        private static WorkItemField2 AField(string name, string referenceName)
            => new() { Name = name, ReferenceName = referenceName };

        /// <summary>Some field other than the one the configuration asks for, so an empty list is never why a lookup found nothing.</summary>
        private static WorkItemField2 AFieldTheOrganisationDoesHold()
            => AField("Effort", "Microsoft.VSTS.Scheduling.Effort");

        /// <summary>A 401 that carries a challenge header, which is what dev.azure.com answers with.</summary>
        private static VssUnauthorizedException AChallengedRefusal()
            => new("VS30063: You are not authorized to access https://dev.azure.com.");

        /// <summary>
        /// What a WAF or a sign-in page in front of an on-premises server answers with: a status, and a body
        /// that is not the JSON error Azure DevOps would have sent.
        /// </summary>
        private static VssServiceResponseException AProxyPageInsteadOfAnError(HttpStatusCode status)
            => new(status, $"{status}", null);
    }
}
