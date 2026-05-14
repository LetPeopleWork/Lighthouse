namespace Lighthouse.Backend.Models.OAuth
{
    public enum OAuthCredentialStatus
    {
        Valid = 0,
        RefreshFailed = 1,
        Disconnected = 2,
    }
}
