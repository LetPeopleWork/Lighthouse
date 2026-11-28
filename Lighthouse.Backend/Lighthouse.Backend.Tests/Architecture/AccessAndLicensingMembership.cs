namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The types that make up Access and Licensing, measured from the compiled code, by name without their namespace so the
    /// same lists hold before and after the move into Modules/Access and Modules/Licensing. Each list is the layer the type
    /// goes to: Domain reaches only the shared Models and the base library, Adapters.Http holds the ASP.NET pieces, and
    /// Application holds the rest, the interfaces included. ModelsTypesThatMoveIntoAccess holds the shared Models types Access
    /// may take with it. A name the code holds twice is written with its namespace. Access holds 30 types from 29 files,
    /// because the embed-session file also declares EmbedHandshakeOutcome as a second top-level type.
    /// </summary>
    internal static class AccessAndLicensingMembership
    {
        public static IReadOnlyList<string> AccessDomain { get; } =
        [
            "ApiKeyPrincipalFactory",
            "CurrentUserProfileStatus",
            "EmbedHandshakeOutcome",
            "EmbedSessionTokenRedemption",
            "FeatureReadability",
            "GroupClaimParser",
        ];

        public static IReadOnlyList<string> AccessApplication { get; } =
        [
            "ApiKeyService",
            "AuthConfigurationValidator",
            "AuthModeResolver",
            "CurrentUserProfileService",
            "EmbedSessionTokenService",
            "FeatureMoveAuthorization",
            "IApiKeyService",
            "IAuthConfigurationValidator",
            "ICurrentUserProfileService",
            "IEmbedSessionTokenService",
            "IFeatureMoveAuthorization",
            "IOidcGroupSnapshotWriter",
            "OidcGroupSnapshotWriter",
            "RbacAdministrationService",
        ];

        public static IReadOnlyList<string> AccessContracts { get; } =
        [
            "IAuthModeResolver",
            "IRbacAdministrationService",
        ];

        public static IReadOnlyList<string> AccessAdaptersHttp { get; } =
        [
            "AccessMarkerAttribute",
            "ApiKeyAuthenticationHandler",
            "BlockedModeFilter",
            "CheckedInHandlerAttribute",
            "DisabledAuthenticationHandler",
            "RbacGuardAttribute",
            "SignedInOnlyAttribute",
            "SmartAuthSchemeSelector",
        ];

        public static IReadOnlyList<string> AccessModuleRoot { get; } =
        [
            "AccessModule",
        ];

        public static IReadOnlyList<string> LicensingApplication { get; } =
        [
            "ILicenseVerifier",
            "LicenseService",
            "LicenseVerifier",
        ];

        public static IReadOnlyList<string> LicensingContracts { get; } =
        [
            "ILicenseService",
        ];

        public static IReadOnlyList<string> LicensingAdaptersHttp { get; } =
        [
            "LicenseGuardAttribute",
        ];

        public static IReadOnlyList<string> LicensingModuleRoot { get; } =
        [
            "LicensingModule",
        ];

        public static IReadOnlyList<string> ModelsTypesThatMoveIntoAccess { get; } =
        [
            "ApiKeyOwnerResolutionState",
            "ApiKeyValidationResult",
            "AuthConfigurationValidationResult",
            "EffectiveAccess",
        ];
    }
}
