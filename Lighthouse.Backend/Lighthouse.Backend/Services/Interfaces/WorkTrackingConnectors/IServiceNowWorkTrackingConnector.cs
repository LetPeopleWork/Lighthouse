namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// DI marker for the ServiceNow adapter, mirroring ILinearWorkTrackingConnector.
    /// Deliberately does NOT extend IBoardInformationProvider: ServiceNow has no board concept and
    /// table discovery is unavailable to a least-privilege account, so there is no wizard to feed.
    /// </summary>
    public interface IServiceNowWorkTrackingConnector : IWorkTrackingConnector
    {
    }
}
