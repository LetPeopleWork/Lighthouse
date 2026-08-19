using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Dependencies
{
    /// <summary>
    /// Tells the operator what a refresh found among the dependencies it just read. Everything reported
    /// here is already on screen for the user; it is in the log so a support conversation can start from a
    /// log file rather than from a screenshot.
    /// </summary>
    public interface IDependencyRefreshReporter
    {
        void ReportOn(Portfolio portfolio);
    }
}
