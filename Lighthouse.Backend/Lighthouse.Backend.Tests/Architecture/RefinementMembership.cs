namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>The layer of the Refinement module a moving type goes to.</summary>
    internal enum RefinementLayer
    {
        Domain,
        Application,
    }

    /// <summary>The kind of file a target naming a moving Refinement type lives in.</summary>
    internal enum RefinementTargetKind
    {
        Suppression,
        Analyser,
        CiSelection,
        TestBudget,
        HostUser,
        Stryker,
    }

    /// <summary>
    /// A type of the Refinement module: found today in its old namespace, after the move in Lighthouse.Backend.Modules.Refinement.Layer.
    /// File is the source file that declares it, which moves whole, so every type of one file has one layer. A port file declares a
    /// port interface and stays in Application with every record and enum it declares.
    /// </summary>
    internal sealed record RefinementType(string Name, RefinementLayer Layer, string OldNamespace, string File, bool InAPortFile)
    {
        public const string NewRootNamespace = "Lighthouse.Backend.Modules.Refinement";

        public string NewNamespace => $"{NewRootNamespace}.{Layer}";

        public string OldFullName => $"{OldNamespace}.{Name}";

        public string NewFullName => $"{NewNamespace}.{Name}";
    }

    /// <summary>A Refinement type that stays where it is, with the part of the code that owns it and why it stays.</summary>
    internal sealed record RefinementTypeThatStays(string Name, string Namespace, string Owner, string Reason)
    {
        public string FullName => $"{Namespace}.{Name}";
    }

    /// <summary>
    /// A target in a suppression, analyser, CI-selection, test-budget, host-user or Stryker file that names a moving type.
    /// Target is written from the type's name on, without its namespace, so the entry holds before and after the move.
    /// </summary>
    internal sealed record RefinementTarget(string File, string Target, RefinementTargetKind Kind);

    /// <summary>
    /// The Refinement module, measured from the compiled code: every type that moves into Lighthouse.Backend.Modules.Refinement
    /// with its layer and old namespace, the Refinement types that stay with their owner and reason, and the targets that name a
    /// moving type. A type is listed with its old namespace and found in that namespace or in its new one, so the map holds before,
    /// during and after the move.
    /// </summary>
    internal static class RefinementMembership
    {
        public const string ImplementationNamespace = "Lighthouse.Backend.Services.Implementation.Refinement";

        public const string InterfacesNamespace = "Lighthouse.Backend.Services.Interfaces.Refinement";

        private const string KernelOwner = "The shared kernel";

        private const string ApiOwner = "The Refinement API, the driving adapter";

        private const string StoredOnTheTeam = "The database context stores it on the team, so moving it would change the stored model";

        private const string OnTheApiWire = "The API sends it, so moving it would change the wire format";

        public static IReadOnlyList<string> ModuleRoot { get; } = ["RefinementModule"];

        public static IReadOnlyList<RefinementType> MovingTypes { get; } =
        [
            .. File(ImplementationNamespace, RefinementLayer.Domain, "NeedBand.cs", "NeedBand"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementCadenceCalendar.cs", "RefinementCadenceCalendar"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementCalendar.cs", "RefinementCalendar"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementList.cs", "RefinementList"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementNeedCalculator.cs", "RefinementNeedCalculator"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementResolution.cs", "RefinementResolution"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "RefinementViewQuery.cs", "RefinementViewQuery"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "SizingLogCommands.cs", "SizingLogCommands"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "SizingRefusal.cs", "SizingRefusal"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "SleYardstickResolver.cs", "SleYardstickResolver"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "StageRuleHealing.cs", "StageRuleHealing"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "StageRuleMatcher.cs", "StageRuleMatcher", "StageMatches"),
            .. File(ImplementationNamespace, RefinementLayer.Application, "VoterIdentityResolver.cs", "VoterIdentityResolver", "VoterResolution", "VoterKeyResolution", "VoterRefusal"),
            .. PortFile(InterfacesNamespace, "IRefinementCalendar.cs", "IRefinementCalendar", "RefinementCalendarFacts", "RefinementCycle"),
            .. PortFile(InterfacesNamespace, "IRefinementViewQuery.cs",
                "IRefinementViewQuery",
                "NeedRange",
                "ReadySource",
                "RefinementNeed",
                "RefinementOutlook",
                "RefinementRow",
                "RefinementView",
                "RowConversation",
                "RowStanding",
                "RowVotes",
                "SizingLog",
                "SizingLogLine",
                "VoterIdentityKind",
                "VoterNames",
                "VoteSplit",
                "Yardstick"),
            .. PortFile(InterfacesNamespace, "ISizingLogCommands.cs", "ISizingLogCommands", "SizingComment", "SizingOutcome", "SizingVote", "Voter"),
        ];

        public static IReadOnlyList<RefinementTypeThatStays> TypesThatStay { get; } =
        [
            new("ISizingLogRepository", "Lighthouse.Backend.Services.Interfaces.Repositories", "Refinement, through the repository ports",
                "Every repository stays with the persistence ports until those are reshaped, and the demo data reaches this port, so moving it would give Refinement a caller other than the API"),
            new("SizingLogRepository", "Lighthouse.Backend.Services.Implementation.Repositories", "Refinement, through the persistence adapters",
                "Every repository stays with the persistence adapters until those are reshaped, beside its port"),
            .. Stays(KernelOwner, "Lighthouse.Backend.Models.Refinement", StoredOnTheTeam,
                "RefinementSettings",
                "RefinementBand",
                "RefinementCadence",
                "StageRules",
                "RefinementStateSetting",
                "ReadinessSetting",
                "DiscussionRules"),
            new("SizingLogEntry", "Lighthouse.Backend.Models.Refinement", KernelOwner, "The database context stores it as the sizing log table"),
            new("RefinementVerdict", "Lighthouse.Backend.Models.Refinement", KernelOwner, "The usage data sends it, so moving it would change what is sent"),
            .. Stays(KernelOwner, "Lighthouse.Backend.Models.Refinement", OnTheApiWire,
                "RefinementStage",
                "NeedUnavailableReason",
                "RowReadiness",
                "SizingAnswer",
                "SizingChannel",
                "SizingEntryKind",
                "YardstickSource"),
            .. Stays(ApiOwner, "Lighthouse.Backend.API", "A controller is the module's driving adapter and stays with the other controllers",
                "RefinementController",
                "RefinementVotesController"),
            new("RefinementSettingsValidator", "Lighthouse.Backend.API.Helpers", ApiOwner, "It checks the settings the API receives, beside the other request validators"),
            .. Stays(ApiOwner, "Lighthouse.Backend.API.DTO", "A data transfer object is the wire shape of the driving adapter and stays with the other ones",
                "RefinementSettingsDto",
                "RefinementViewDto",
                "SizingCommentDto",
                "SizingLogDto",
                "SizingVoteDto"),
            new("WeeklyRecurrence", "Lighthouse.Backend.Modules.OrganisationSetup.Domain", "Organisation set-up",
                "The recurring blackout rules use it as well, so it moves with the organisation set-up, not with Refinement"),
        ];

        public static IReadOnlyList<RefinementTarget> TargetsNamingAMovingType { get; } = [];

        private static IEnumerable<RefinementType> File(string oldNamespace, RefinementLayer layer, string file, params string[] names)
            => names.Select(name => new RefinementType(name, layer, oldNamespace, file, InAPortFile: false));

        private static IEnumerable<RefinementType> PortFile(string oldNamespace, string file, params string[] names)
            => names.Select(name => new RefinementType(name, RefinementLayer.Application, oldNamespace, file, InAPortFile: true));

        private static IEnumerable<RefinementTypeThatStays> Stays(string owner, string inNamespace, string reason, params string[] names)
            => names.Select(name => new RefinementTypeThatStays(name, inNamespace, owner, reason));
    }
}
