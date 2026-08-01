using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// A read a connector refused, carrying the verdict that says why (ADR-126 decision 1). The
    /// driving side of <see cref="IBoardInformationProvider"/> catches this rather than a
    /// provider's own exception type, so a refusal reaches the administrator with the name the
    /// connector already gave it instead of a canned retry message.
    /// </summary>
    public abstract class WorkTrackingReadException(ConnectionValidationResult verdict)
        : Exception(verdict.Message)
    {
        public ConnectionValidationResult Verdict { get; } = verdict;
    }
}
