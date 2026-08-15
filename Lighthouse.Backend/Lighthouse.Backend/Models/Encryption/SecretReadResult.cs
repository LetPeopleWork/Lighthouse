namespace Lighthouse.Backend.Models.Encryption
{
    public sealed record SecretReadResult(SecretState State, string? PlainText, string? KeyId);
}
