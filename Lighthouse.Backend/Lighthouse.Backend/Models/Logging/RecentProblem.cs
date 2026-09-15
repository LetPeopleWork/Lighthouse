namespace Lighthouse.Backend.Models.Logging
{
    /// <summary>
    /// One warning-or-worse event, kept as a structured record rather than as a line of text. The rolling
    /// log file is written through one of two different templates depending on configuration, so anything
    /// that has to be read by level has to be captured before it is rendered into either of them.
    /// </summary>
    public sealed record RecentProblem(
        DateTimeOffset RecordedAt,
        string Level,
        string Source,
        string Message,
        string? ExceptionType);
}
