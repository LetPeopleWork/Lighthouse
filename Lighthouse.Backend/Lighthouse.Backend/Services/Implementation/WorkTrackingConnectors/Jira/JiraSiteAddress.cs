namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The address a Jira site answers its API on, as opposed to whichever page of it was copied out of the
    /// browser. Jira Cloud's address bar shows the site under a path such as /jira/, but the REST API and the
    /// cloud-id lookup only answer at the site root, so a Cloud address is cut back to its scheme and host.
    /// Any other address may be a Data Center installation served under a context path, which every request
    /// has to keep, so only a trailing slash is dropped there.
    /// </summary>
    public static class JiraSiteAddress
    {
        private const string AtlassianCloudHostSuffix = ".atlassian.net";

        private static readonly char[] EndOfAuthority = ['/', '?', '#'];

        /// <summary>
        /// Anything that is not an absolute URL comes back exactly as given, so the check that tells a user
        /// their URL is invalid still sees what they typed.
        /// </summary>
        public static string Of(string raw)
        {
            var address = raw.Trim();
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                return raw;
            }

            if (uri.Host.EndsWith(AtlassianCloudHostSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return SchemeAndAuthorityAsTyped(address, uri);
            }

            return address.EndsWith('/') ? address[..^1] : address;
        }

        /// <summary>
        /// The value a connection option is stored with: a Jira connection's site URL is kept as its site
        /// address, and every other option, including those of other systems, exactly as entered.
        /// </summary>
        public static string ForOption(WorkTrackingSystems workTrackingSystem, string optionKey, string value)
            => workTrackingSystem == WorkTrackingSystems.Jira && optionKey == JiraWorkTrackingOptionNames.Url
                ? Of(value)
                : value;

        private static string SchemeAndAuthorityAsTyped(string address, Uri uri)
        {
            var authorityStart = uri.Scheme.Length + Uri.SchemeDelimiter.Length;
            var authorityEnd = address.IndexOfAny(EndOfAuthority, authorityStart);

            return authorityEnd < 0 ? address : address[..authorityEnd];
        }
    }
}
