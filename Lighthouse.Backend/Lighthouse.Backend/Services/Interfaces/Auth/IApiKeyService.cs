using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface IApiKeyService
    {
        /// <summary>
        /// Creates a new API key. Returns the result including the plaintext key (shown only once).
        /// </summary>
        Task<ApiKeyCreationResult> CreateApiKeyAsync(string name, string description, string createdByUser, string ownerSubject);

        /// <summary>
        /// Creates a new API key with optional per-key scope rows. When <paramref name="scope"/> is
        /// non-empty, an <see cref="Lighthouse.Backend.Models.Authorization.ApiKeyPermission"/> row
        /// is persisted for each entry. When null or empty, the key inherits the owner's permissions
        /// at runtime (legacy behaviour).
        /// </summary>
        Task<ApiKeyCreationResult> CreateApiKeyAsync(
            string name,
            string description,
            string createdByUser,
            string ownerSubject,
            IReadOnlyList<ApiKeyScopeDto>? scope);

        /// <summary>
        /// Returns metadata for API keys owned by the given subject. Never includes the plaintext key or hash.
        /// </summary>
        IEnumerable<ApiKeyInfo> GetApiKeysByOwnerSubject(string ownerSubject);

        /// <summary>
        /// Deletes an API key by ID if owned by the given subject. Returns false if not found or not owned by the subject.
        /// </summary>
        Task<bool> DeleteApiKey(int id, string ownerSubject);

        /// <summary>
        /// Validates a plaintext API key and returns owner resolution details for authentication.
        /// </summary>
        Task<ApiKeyValidationResult> ValidateApiKeyWithOwnerAsync(string plainTextKey);

        /// <summary>
        /// Validates a plaintext API key. Returns true if valid, and updates LastUsedAt.
        /// </summary>
        Task<bool> ValidateApiKeyAsync(string plainTextKey);
    }
}
