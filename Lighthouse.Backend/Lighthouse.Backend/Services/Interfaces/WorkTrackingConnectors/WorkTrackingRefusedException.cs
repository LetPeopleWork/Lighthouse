using System.Net;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// A work tracking system read the query Lighthouse sent and refused to run it. Its sentence and the query
    /// it rejected are the only two things anyone can act on, so both travel on the exception: a status code
    /// alone never says what to change, and a warning logged deep inside a connector names neither the
    /// portfolio nor the team whose refresh stopped.
    ///
    /// It lives beside the other connector port types so a background refresh can recognise a refusal without
    /// knowing which work tracking system produced it.
    ///
    /// It is an <see cref="HttpRequestException"/> because callers along the sync path answer "the system
    /// could not be asked" by catching exactly that, and a refusal has always reached them that way.
    /// </summary>
    public class WorkTrackingRefusedException(string message, string rejectedQuery, HttpStatusCode statusCode)
        : HttpRequestException(message, null, statusCode)
    {
        public string RejectedQuery { get; } = rejectedQuery;
    }
}
