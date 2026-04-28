using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/apikeys")]
    [Route("api/latest/apikeys")]
    [ApiController]
    [Authorize]
    public class ApiKeyController(IApiKeyService apiKeyService) : ControllerBase
    {
        /// <summary>
        /// Creates a new API key. The plaintext key is returned only once.
        /// </summary>
        [HttpPost]
        [ProducesResponseType<ApiKeyCreationResult>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("API key name is required.");
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
            var result = await apiKeyService.CreateApiKeyAsync(request.Name, request.Description ?? string.Empty, userName);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Returns metadata for all API keys. Never includes the plaintext key or hash.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<IEnumerable<ApiKeyInfo>>(StatusCodes.Status200OK)]
        public IActionResult GetApiKeys()
        {
            var keys = apiKeyService.GetAllApiKeys();
            return Ok(keys);
        }

        /// <summary>
        /// Deletes an API key by ID.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteApiKey(int id)
        {
            var deleted = apiKeyService.DeleteApiKey(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
