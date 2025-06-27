namespace Lighthouse.Backend.Services.Interfaces
{
    public class OAuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }

    public class AccessibleResource
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public interface IOAuthService
    {
        string GetJiraAuthorizationUrl(string clientId, string redirectUri, string state);
        Task<OAuthTokenResponse> ExchangeCodeForTokens(string clientId, string clientSecret, string code, string redirectUri);
        Task<OAuthTokenResponse> RefreshAccessToken(string clientId, string clientSecret, string refreshToken);
        Task<bool> ValidateAccessToken(string accessToken, string jiraUrl);
        Task<List<AccessibleResource>?> GetAccessibleResources(string accessToken);
    }
}
