namespace Lighthouse.Backend.Models.Auth
{
    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
