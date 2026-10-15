namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A link the issue's history says still stands, and when it was made. The history names no link type
    /// in a field of its own, so the sentence Jira wrote for the link ("This work item is caused by PAY-88")
    /// is what tells two links to the same issue apart.
    /// </summary>
    public sealed record StandingLink(string CounterpartKey, string Sentence, DateTime AddedAt);
}
