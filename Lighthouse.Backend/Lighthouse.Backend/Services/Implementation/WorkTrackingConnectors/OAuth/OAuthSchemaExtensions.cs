namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth
{
    public interface IOAuthSchemaExtensions
    {
        IReadOnlyList<string> ExtraOAuthKeys { get; }
    }

    public sealed class OAuthSchemaExtensions : IOAuthSchemaExtensions
    {
        public OAuthSchemaExtensions(IReadOnlyList<string> extraOAuthKeys)
        {
            ArgumentNullException.ThrowIfNull(extraOAuthKeys);
            ExtraOAuthKeys = extraOAuthKeys;
        }

        public OAuthSchemaExtensions() : this(Array.Empty<string>())
        {
        }

        public IReadOnlyList<string> ExtraOAuthKeys { get; }
    }
}
