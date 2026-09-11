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

        private static JiraReadException Unreadable(string filterId, HttpStatusCode status, string whatCameBack)
            => new(ConnectionValidationResult.Failure(
                "board_filter_unreadable",
                $"Lighthouse could not read the filter behind this board. {whatCameBack} The account this connection signs in with may not have permission to see that saved filter - a board stays visible even when the filter behind it is shared with a smaller group. Ask a Jira administrator to share filter {filterId} with that account, or configure the team with a JQL query instead of a board.",
                $"GET rest/api/2/filter/{filterId} answered {(int)status} {status}.",
                "DataRetrievalValue"));
    }
}
