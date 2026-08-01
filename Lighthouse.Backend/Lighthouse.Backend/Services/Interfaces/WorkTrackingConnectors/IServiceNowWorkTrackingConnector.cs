namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// DI marker for the ServiceNow adapter, mirroring ILinearWorkTrackingConnector.
    /// A stock instance has Visual Task Boards, each carrying the table and the filter a Lighthouse
    /// team needs, so the connector serves the same board port the wizard already uses for Jira,
    /// Azure DevOps and Linear (ADR-125). Table and field discovery remain unavailable to a
    /// least-privilege account (ADR-116); a board is read instead of discovered.
    /// </summary>
    public interface IServiceNowWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider
    {
    }
}
