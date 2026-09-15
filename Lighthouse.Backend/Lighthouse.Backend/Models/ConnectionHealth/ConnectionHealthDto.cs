namespace Lighthouse.Backend.Models.ConnectionHealth
{
    /// <summary>
    /// One row of the connection health read. <see cref="State"/> travels as its name rather than its
    /// number, because the browser's own union is strings and a renumbering would otherwise silently
    /// relabel the colour an operator reads.
    /// </summary>
    public sealed record ConnectionHealthDto(
        int ConnectionId,
        string ConnectionName,
        string WorkTrackingSystem,
        string State,
        string? Message,
        DateTime? ObservedAt);
}
