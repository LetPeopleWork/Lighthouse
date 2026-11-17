namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IWorkTrackingAuthStrategyFactory
    {
        IWorkTrackingAuthStrategy Resolve(string authenticationMethodKey);
    }
}
