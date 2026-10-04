using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Who is voting. Without sign-in a voter is the name they declare plus a random key their browser
    /// keeps; only a hash of the key is stored, so the log never holds anything that could speak for them.
    /// Whether sign-in is on is asked of the auth mode, never guessed from a missing profile: without
    /// sign-in every request still carries one shared placeholder subject.
    /// </summary>
    public sealed class VoterIdentityResolver(IAuthModeResolver authModeResolver)
    {
        public const int ShortestVoterKey = 32;

        public const int LongestVoterName = 100;

        public VoterIdentityKind Kind => authModeResolver.Resolve().Mode == AuthMode.Enabled
            ? VoterIdentityKind.Account
            : VoterIdentityKind.SelfDeclared;

        /// <summary>The stored key of whoever presents this browser key, to recognise their own votes on a read.</summary>
        public string? ReaderKeyFrom(string? presentedVoterKey)
            => Kind == VoterIdentityKind.SelfDeclared && IsUsable(presentedVoterKey)
                ? SelfDeclaredKeyOf(presentedVoterKey!)
                : null;

        /// <returns>The voter, or what the caller still has to say about who they are before they may write.</returns>
        public VoterResolution ForWrite(string? declaredName, string? presentedVoterKey)
        {
            if (Kind != VoterIdentityKind.SelfDeclared)
            {
                return VoterResolution.RefusedFor(VoterRefusal.NeedsAPerson);
            }

            var name = declaredName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return VoterResolution.RefusedFor(VoterRefusal.NameRequired);
            }

            if (name.Length > LongestVoterName)
            {
                return VoterResolution.RefusedFor(VoterRefusal.NameTooLong);
            }

            if (!IsUsable(presentedVoterKey))
            {
                return VoterResolution.RefusedFor(VoterRefusal.KeyRequired);
            }

            return VoterResolution.Of(new Voter(SelfDeclaredKeyOf(presentedVoterKey!), name, null));
        }

        // A short key could be guessed, and a guessed key speaks for somebody else's votes.
        private static bool IsUsable(string? presentedVoterKey)
            => presentedVoterKey is not null && presentedVoterKey.Length >= ShortestVoterKey;

        private static string SelfDeclaredKeyOf(string presentedVoterKey)
            => SizingLogEntry.SelfDeclaredVoterKeyOf(presentedVoterKey);
    }

    /// <summary>Exactly one of the two is set: the voter, or why there is none.</summary>
    public sealed record VoterResolution(Voter? Voter, VoterRefusal? Refusal)
    {
        public static VoterResolution Of(Voter voter) => new(voter, null);

        public static VoterResolution RefusedFor(VoterRefusal refusal) => new(null, refusal);
    }

    public enum VoterRefusal
    {
        NameRequired,
        KeyRequired,
        NameTooLong,
        NeedsAPerson,
    }
}
