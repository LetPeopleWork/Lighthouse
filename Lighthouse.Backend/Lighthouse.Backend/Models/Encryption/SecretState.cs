namespace Lighthouse.Backend.Models.Encryption
{
    // The four states are the whole vocabulary a stored secret can be described in. There is deliberately
    // no "unknown" and no "probably fine": a caller that cannot name which of these four it is holding has
    // to say so, rather than guess.
    public enum SecretState
    {
        Envelope,

        LegacyCbc,

        LegacyPlaintext,

        Unreadable,
    }
}
