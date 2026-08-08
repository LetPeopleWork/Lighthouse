namespace Lighthouse.Backend.Configuration
{
    public class EmbedConfiguration
    {
        public const string SectionName = "Embed";

        // Public so the service that falls back on them cannot drift from what an unconfigured
        // instance actually gets.
        public const int DefaultTokenLifetimeSeconds = 60;

        public const int DefaultHandshakeOutcomeLifetimeSeconds = 300;

        public const int DefaultSessionLifetimeMinutes = 30;

        // Off unless an instance opts in. The three embed hops exist for the Jira app, and until the
        // handshake nonce is bound to its requester an instance that frames nothing should not carry
        // the surface at all. Epic #5674.
        public bool Enabled { get; set; }

        public int TokenLifetimeSeconds { get; set; } = DefaultTokenLifetimeSeconds;

        // ADR-137 DQ-2: the token's window is bounded by one machine redirect, the handshake
        // outcome's by a human finishing a login. Sharing the 60s would say "try again" to a
        // sign-in that worked, only for the slowest users.
        public int HandshakeOutcomeLifetimeSeconds { get; set; } = DefaultHandshakeOutcomeLifetimeSeconds;

        public int SessionLifetimeMinutes { get; set; } = DefaultSessionLifetimeMinutes;

        /// <summary>
        /// Two callers need this number and must agree: the cookie's <c>ExpireTimeSpan</c>, and the
        /// window the handshake advertises so the Forge app knows when to stop reusing a session.
        /// They disagreed — the advertised value fell back on a non-positive setting and the cookie
        /// did not — so a configured <c>0</c> produced a cookie that expired on arrival and a grant
        /// promising thirty minutes.
        /// </summary>
        public int ResolveSessionLifetimeMinutes()
        {
            return SessionLifetimeMinutes > 0 ? SessionLifetimeMinutes : DefaultSessionLifetimeMinutes;
        }
    }
}
