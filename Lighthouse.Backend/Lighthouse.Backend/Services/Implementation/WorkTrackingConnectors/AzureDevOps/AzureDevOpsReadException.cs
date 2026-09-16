using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.VisualStudio.Services.WebApi;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// A read Azure DevOps would not answer, carrying the verdict the administrator is shown. It is
    /// deliberately neither a Vss* exception nor an HttpRequestException: those are the connector's
    /// signals for "Azure DevOps rejected the connection settings" and "Azure DevOps could not be
    /// reached", and a refused read is neither of those - the same connection answered a query
    /// moments earlier.
    /// </summary>
    public class AzureDevOpsReadException : WorkTrackingReadException
    {
        /// <summary>
        /// The input on the connection screen that additional fields are typed into. Every verdict
        /// about those fields has to name it identically, or the message is shown without an input
        /// highlighted.
        /// </summary>
        internal const string AdditionalFieldsFieldName = "Additional Fields";

        /// <summary>
        /// The input a team's or a portfolio's own query is typed into. Named the same way everywhere for
        /// the same reason as the field above.
        /// </summary>
        internal const string QueryFieldName = "DataRetrievalValue";

        public AzureDevOpsReadException(ConnectionValidationResult verdict)
            : base(verdict)
        {
        }

        /// <summary>
        /// Nothing in the configuration narrows what to fetch - no types, no states, no query of the
        /// operator's - so the WHERE clause comes out with no condition under it. Azure DevOps refuses that
        /// outright, and all the operator would see is whatever the tracker says about the syntax of a query
        /// they never wrote. Saying it here instead names the configuration that caused it, on the request
        /// that caused it.
        /// </summary>
        public static AzureDevOpsReadException NothingNarrowsTheQuery()
            => new(ConnectionValidationResult.Failure(
                "nothing_to_query",
                "This configuration selects no work item types, no states and carries no query of its own, "
                + "so there is nothing for Lighthouse to ask Azure DevOps for. Choose at least one work item "
                + "type, map at least one state, or write a query.",
                "The assembled WIQL would have carried an empty WHERE clause, so no query was sent.",
                QueryFieldName));

        /// <summary>
        /// Azure DevOps would not hand over the list of fields in the organisation, so no additional
        /// field can be checked against it.
        ///
        /// The message says what Azure DevOps answered and what can be done about it, and claims
        /// nothing about what else is or is not working. Two of the paths that reach here - a refresh
        /// looking work items up by id, and a portfolio read - never issue a query first, so a
        /// reassurance that the connection has just been proven good would be false on them. The
        /// status Azure DevOps gave is in the sentence, which is what tells a refused permission apart
        /// from a credential that has expired.
        /// </summary>
        public static AzureDevOpsReadException FieldListRefused(Exception refusal)
            => new(ConnectionValidationResult.Failure(
                "field_list_unreadable",
                "Lighthouse could not read the list of fields in this Azure DevOps organisation, so "
                + "it cannot check the additional fields against it. "
                + $"Azure DevOps answered: {WhatAdoSaid(refusal)} "
                + "The account this connection signs in with needs to be able to read work "
                + "items in at least one project for Azure DevOps to return the field list; a proxy "
                + "in front of an on-premises server can also refuse this response, which is much "
                + "larger than the others Lighthouse asks for.",
                $"GET _apis/wit/fields answered {WhatAdoSaid(refusal)}",
                AdditionalFieldsFieldName));

        /// <summary>
        /// When Azure DevOps answers with a body its own error contract does not recognise - a proxy's
        /// HTML page, plain text - the exception's message is the bare status name, and the status
        /// number survives nowhere else.
        /// </summary>
        private static string WhatAdoSaid(Exception refusal)
            => refusal is VssServiceResponseException response
                ? $"{(int)response.HttpStatusCode} ({response.HttpStatusCode}). {response.Message}"
                : refusal.Message;
    }
}
