namespace Lighthouse.Backend.Configuration
{
    public class EmbedConfiguration
    {
        public const string SectionName = "Embed";

        public int TokenLifetimeSeconds { get; set; } = 60;

        // ADR-132 DQ-2: the token's window is bounded by one machine redirect, the handshake
        // outcome's by a human finishing a login. Sharing the 60s would say "try again" to a
        // sign-in that worked, only for the slowest users.
        public int HandshakeOutcomeLifetimeSeconds { get; set; } = 300;

        public int SessionLifetimeMinutes { get; set; } = 30;
    }
}
