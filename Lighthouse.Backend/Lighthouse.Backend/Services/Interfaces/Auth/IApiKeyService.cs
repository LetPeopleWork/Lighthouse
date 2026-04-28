using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface IApiKeyService
    {
        /// <summary>
        /// Creates a new API key. Returns the result including the plaintext key (shown only once).
        /// </summary>
        Task<ApiKeyCreationResult> CreateApiKeyAsync(string name, string description, string createdByUser);

        /// <summary>
        /// Returns metadata for all API keys. Never includes the plaintext key or hash.
        /// </summary>
        IEnumerable<ApiKeyInfo> GetAllApiKeys();

        /// <summary>
        /// Deletes an API key by ID. Returns false if not found.
        /// </summary>
        bool DeleteApiKey(int id);

        /// <summary>
        /// Validates a plaintext API key. Returns true if valid, and updates LastUsedAt.
        /// </summary>
        Task<bool> ValidateApiKeyAsync(string plainTextKey);
    }
}
