using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    public interface IUsageDataConsentService
    {
        /// <summary>
        /// Answers for the browser holding <paramref name="token"/>, or for a browser holding none.
        /// A token this instance never minted gets the same answer as no token at all: answering
        /// differently would turn this into a way of testing whether a given token exists here.
        /// </summary>
        Task<UsageDataState> GetStateAsync(string? token, CancellationToken cancellationToken);

        /// <summary>
        /// Records a decision and returns the freshly minted token, once.
        /// </summary>
        Task<string> RecordDecisionAsync(UsageDataDecision decision, CancellationToken cancellationToken);

        /// <summary>
        /// Withdraws the consent held by <paramref name="token"/>, if there is one. Reports nothing:
        /// the caller must answer a browser identically whether or not anything was withdrawn.
        /// </summary>
        Task RevokeAsync(string token, CancellationToken cancellationToken);
    }
}
