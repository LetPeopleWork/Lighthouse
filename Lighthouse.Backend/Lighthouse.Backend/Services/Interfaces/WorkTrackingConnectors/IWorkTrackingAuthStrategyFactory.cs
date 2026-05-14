namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IWorkTrackingAuthStrategyFactory
    {
        IWorkTrackingAuthStrategy Resolve(string authenticationMethodKey);
    }
}
