using System.Diagnostics.CodeAnalysis;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Who is voting. Without sign-in a voter is the name they declare plus a random key their browser
    /// keeps; only a hash of the key is stored, so the log never holds anything that could speak for them.
    /// With sign-in a voter is their account, whatever name or key the request carries.
    /// Whether sign-in is on is asked of the auth mode, never guessed from a missing profile: without
    /// sign-in every request still carries one shared placeholder subject.
    /// </summary>
    public sealed class VoterIdentityResolver(IAuthModeResolver authModeResolver, IHttpContextAccessor httpContextAccessor)
    {
        public const int ShortestVoterKey = 32;

        public const int LongestVoterName = 100;

        public VoterIdentityKind Kind => authModeResolver.Resolve().Mode == AuthMode.Enabled
            ? VoterIdentityKind.Account
            : VoterIdentityKind.SelfDeclared;

        /// <summary>
        /// The stored key of whoever is reading, to recognise their own votes. Signed in, it is read off the
        /// session's subject rather than the profile, because looking the profile up creates it when missing
        /// and a read must not write.
        /// </summary>
        public string? ReaderKeyFrom(string? presentedVoterKey)
        {
            if (SignInIsOn)
            {
                return SignedInSubject is { } subject ? SizingLogEntry.AccountVoterKeyOf(subject) : null;
            }

            return SelfDeclaredKeyOf(presentedVoterKey);
        }

        /// <param name="signedInPerson">Looks up the caller's profile; asked only when sign-in is on.</param>
        /// <returns>The voter, or what the caller still has to say about who they are before they may write.</returns>
        public async Task<VoterResolution> ForWriteAsync(string? declaredName, string? presentedVoterKey, Func<Task<UserProfile?>> signedInPerson)
        {
            if (SignInIsOn)
            {
                return await SignedInVoterAsync(signedInPerson);
            }

            return SelfDeclared(declaredName, presentedVoterKey);
        }

        /// <summary>
        /// Whose vote is taken back. Taking back writes to the log too, so signed in it asks for the person
        /// exactly as a vote does, and a credential nobody stands behind is refused before the log is read.
        /// Without sign-in the browser's key alone is enough: the take-back reuses the name the vote was cast with.
        /// </summary>
        public async Task<VoterKeyResolution> ForTakeBackAsync(string? presentedVoterKey, Func<Task<UserProfile?>> signedInPerson)
        {
            if (SignInIsOn)
            {
                var signedIn = await SignedInVoterAsync(signedInPerson);
                return new VoterKeyResolution(signedIn.Voter?.Key, signedIn.Refusal);
            }

            return SelfDeclaredKeyOf(presentedVoterKey) is { } key
                ? new VoterKeyResolution(key, null)
                : new VoterKeyResolution(null, VoterRefusal.KeyRequired);
        }

        private bool SignInIsOn => Kind == VoterIdentityKind.Account;

        private static async Task<VoterResolution> SignedInVoterAsync(Func<Task<UserProfile?>> signedInPerson)
            => await signedInPerson() is { } person
                ? VoterResolution.Of(AccountVoter(person))
                : VoterResolution.RefusedFor(VoterRefusal.NeedsAPerson);

        private static VoterResolution SelfDeclared(string? declaredName, string? presentedVoterKey)
        {
            var name = declaredName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return VoterResolution.RefusedFor(VoterRefusal.NameRequired);
            }

            if (name.Length > LongestVoterName)
            {
                return VoterResolution.RefusedFor(VoterRefusal.NameTooLong);
            }

            if (SelfDeclaredKeyOf(presentedVoterKey) is not { } key)
            {
                return VoterResolution.RefusedFor(VoterRefusal.KeyRequired);
            }

            return VoterResolution.Of(new Voter(key, name, null));
        }

        private static string? SelfDeclaredKeyOf(string? presentedVoterKey)
            => IsUsable(presentedVoterKey) ? SizingLogEntry.SelfDeclaredVoterKeyOf(presentedVoterKey) : null;

        private string? SignedInSubject
            => httpContextAccessor.HttpContext?.User is { } user ? CurrentUserProfileService.StableSubjectOf(user) : null;

        // An identity provider need not send a name or an email address, but every account has a subject.
        private static Voter AccountVoter(UserProfile person)
        {
            var shownAs = Filled(person.DisplayName) ?? Filled(person.Email) ?? person.Subject;
            return new Voter(SizingLogEntry.AccountVoterKeyOf(person.Subject), shownAs, person.Id);
        }

        private static string? Filled(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        // A short key could be guessed, and a guessed key speaks for somebody else's votes. A browser's key
        // is hex, so whitespace or a control character means it is not one; a key of nothing but spaces
        // is also what the rate limiter counts as no key at all.
        private static bool IsUsable([NotNullWhen(true)] string? presentedVoterKey)
            => presentedVoterKey is not null
                && presentedVoterKey.Length >= ShortestVoterKey
                && !presentedVoterKey.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
    }

    /// <summary>Exactly one of the two is set: the voter, or why there is none.</summary>
    public sealed record VoterResolution(Voter? Voter, VoterRefusal? Refusal)
    {
        public static VoterResolution Of(Voter voter) => new(voter, null);

        public static VoterResolution RefusedFor(VoterRefusal refusal) => new(null, refusal);
    }

    /// <summary>Exactly one of the two is set: the voter's stored key, or why there is none.</summary>
    public sealed record VoterKeyResolution(string? VoterKey, VoterRefusal? Refusal);

    public enum VoterRefusal
    {
        NameRequired,
        KeyRequired,
        NameTooLong,
        NeedsAPerson,
    }
}
