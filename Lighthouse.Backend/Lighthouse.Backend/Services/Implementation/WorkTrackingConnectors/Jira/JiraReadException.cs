using System.Net;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A read Jira would not answer, carrying the verdict the administrator is shown. It is distinct from
    /// <see cref="JiraQueryRejectedException"/>, which is a query Jira read and refused to run: that one is
    /// an <see cref="HttpRequestException"/> because the sync path treats it as "Jira could not be asked",
    /// and a board read must instead reach the wizard as a verdict with a reason on it.
    /// </summary>
    public class JiraReadException : WorkTrackingReadException
    {
        /// <summary>
        /// The input on the connection screen that additional fields are typed into. Every verdict about
        /// those fields has to name it identically, or the message is shown without an input highlighted.
        /// </summary>
        internal const string AdditionalFieldsFieldName = "Additional Fields";

        public JiraReadException(ConnectionValidationResult verdict)
            : base(verdict)
        {
        }

        /// <summary>
        /// Jira would not hand over the saved filter a board is built on. The board itself can be perfectly
        /// visible while its filter is shared with a smaller group, which is why the message names the
        /// account rather than the board.
        /// </summary>
        public static JiraReadException BoardFilterRefused(string filterId, HttpStatusCode status)
            => Unreadable(filterId, status, $"Jira answered {(int)status} ({status}).");

        /// <summary>
        /// Jira handed over the filter but there was no query in it. Same outcome for the user: the board's
        /// scope is unknown, and guessing at it would scope the team to the whole instance.
        /// </summary>
        public static JiraReadException BoardFilterCarriedNoQuery(string filterId, HttpStatusCode status)
            => Unreadable(filterId, status, $"Jira answered {(int)status} ({status}), but the filter it returned holds no query.");

        /// <summary>
        /// Jira would not hand over the list of fields on the instance, so no additional field can be
        /// checked against it. This is not a broken URL: the same connection had just answered on
        /// rest/api/2/myself, so the address and the credential are both good enough to be talked to.
        /// </summary>
        public static JiraReadException FieldListRefused(HttpStatusCode status, string whatJiraSaid)
            => new(ConnectionValidationResult.Failure(
                "field_list_unreadable",
                $"Lighthouse could not read the list of fields on this Jira instance, so it cannot check "
                + $"the additional fields against it. Jira answered {(int)status} ({status}). The account "
                + "this connection signs in with needs to be able to browse at least one project for Jira "
                + "to return the field list; a proxy in front of Jira can also refuse this response, which "
                + "is much larger than the others Lighthouse asks for.",
                $"GET rest/api/latest/field answered {(int)status} {status}. {whatJiraSaid}",
                AdditionalFieldsFieldName));

        private static JiraReadException Unreadable(string filterId, HttpStatusCode status, string whatCameBack)
            => new(ConnectionValidationResult.Failure(
                "board_filter_unreadable",
                $"Lighthouse could not read the filter behind this board. {whatCameBack} The account this connection signs in with may not have permission to see that saved filter - a board stays visible even when the filter behind it is shared with a smaller group. Ask a Jira administrator to share filter {filterId} with that account, or configure the team with a JQL query instead of a board.",
                $"GET rest/api/2/filter/{filterId} answered {(int)status} {status}.",
                "DataRetrievalValue"));
    }
}
