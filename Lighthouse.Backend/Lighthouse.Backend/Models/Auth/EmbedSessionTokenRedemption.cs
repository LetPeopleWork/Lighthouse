namespace Lighthouse.Backend.Models.Auth
{
    public readonly record struct EmbedSessionTokenRedemption(bool Succeeded, int ApiKeyId)
    {
        public static EmbedSessionTokenRedemption Refused => new(false, 0);
    }
}
