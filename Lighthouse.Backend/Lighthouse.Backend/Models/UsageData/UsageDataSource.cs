namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which surface an event came from: the web page, the lh command line, or an MCP server. Clients
    /// read the names of these members from the state answer and send only when their own is listed,
    /// so an instance that predates a surface never counts that surface's events as somebody else's.
    ///
    /// Append-only: a member is never renamed, renumbered or removed, because counts already collected
    /// under a name must keep meaning the same surface.
    /// </summary>
    public enum UsageDataSource
    {
        Browser = 0,

        Cli = 1,

        Mcp = 2,
    }
}
