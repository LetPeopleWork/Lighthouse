using System.Net;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Jira read the query and refused to run it. Its answer names the character it tripped over, and that
    /// sentence is the only part of the failure anyone can act on - a status code tells a user nothing about
    /// what to change. It travels on its own exception so a caller can tell "the query is wrong" apart from
    /// "Jira could not be reached" without reading the message text.
    ///
    /// It is an <see cref="HttpRequestException"/> because that is what the refusal used to be thrown as, and
    /// several callers along the sync path still answer "Jira could not be asked" by catching exactly that.
    /// </summary>
    public class JiraQueryRejectedException : HttpRequestException
    {
        public JiraQueryRejectedException()
        {
        }

        public JiraQueryRejectedException(string message)
            : base(message)
        {
        }

        public JiraQueryRejectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public JiraQueryRejectedException(string message, string rejectedQuery, HttpStatusCode statusCode)
            : base(message, null, statusCode)
        {
            RejectedQuery = rejectedQuery;
        }

        public string RejectedQuery { get; } = string.Empty;
    }
}
