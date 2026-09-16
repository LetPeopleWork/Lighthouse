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

        public AzureDevOpsReadException(ConnectionValidationResult verdict)
            : base(verdict)
        {
        }

        /// <summary>
        /// Azure DevOps would not hand over the list of fields in the organisation, so no additional
        /// field can be checked against it. This is not a broken URL and not a bad token: the same
        /// connection had just answered a work item query.
        /// </summary>
        public static AzureDevOpsReadException FieldListRefused(Exception refusal)
            => new(ConnectionValidationResult.Failure(
                "field_list_unreadable",
                "Lighthouse could not read the list of fields in this Azure DevOps organisation, so "
                + "it cannot check the additional fields against it. "
                + $"Azure DevOps answered: {WhatAdoSaid(refusal)} "
                + "The connection itself is good - a work item query on it succeeded moments before "
                + "this. The account this connection signs in with needs to be able to read work "
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
