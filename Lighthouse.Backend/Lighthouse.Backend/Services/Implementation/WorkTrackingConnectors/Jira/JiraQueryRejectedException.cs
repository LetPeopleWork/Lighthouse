using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Net;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Jira read the query and refused to run it. Its answer names the character it tripped over, and that
    /// sentence is the only part of the failure anyone can act on - a status code tells a user nothing about
    /// what to change. It travels on its own exception so a caller can tell "the query is wrong" apart from
    /// "Jira could not be reached" without reading the message text.
    ///
    /// It stays an <see cref="HttpRequestException"/> through its base type, because several callers along
    /// the sync path still answer "Jira could not be asked" by catching exactly that.
    /// </summary>
    public class JiraQueryRejectedException(string message, string rejectedQuery, HttpStatusCode statusCode)
        : WorkTrackingRefusedException(message, rejectedQuery, statusCode)
    {
    }
}
