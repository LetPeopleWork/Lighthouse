namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class WorkTrackingAuthStrategyNotFoundException : Exception
    {
        public WorkTrackingAuthStrategyNotFoundException()
        {
        }

        public WorkTrackingAuthStrategyNotFoundException(string message)
            : base(message)
        {
        }

        public WorkTrackingAuthStrategyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
