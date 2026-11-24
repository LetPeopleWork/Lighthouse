namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A string in a test file that names a moving Refinement type, file or namespace by its old Services path, and what the
    /// move makes of it: the text it reads after the move, or no text when the move deletes the string.
    /// </summary>
    internal sealed record RefinementRename(string File, string OldText, string? NewText)
    {
        public RenamedString AsRenamedString() => new(File, OldText, NewText ?? string.Empty);
    }

    /// <summary>
    /// Every string in the test project that names Refinement by its old Services path when the Refinement types are about
    /// to move into Lighthouse.Backend.Modules.Refinement. The list was taken from a search of the test project before the
    /// move and only shrinks: an entry leaves the list in the commit that renames or deletes its string.
    /// </summary>
    internal static class RefinementStringsTheMoveRenames
    {
        private const string Architecture = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        private const string OldImplementationFolder = "Services/Implementation/Refinement/";

        private const string OldInterfacesFolder = "Services/Interfaces/Refinement/";

        private const string NewApplicationFolder = "Modules/Refinement/Application/";

        public static IReadOnlyList<RefinementRename> Entries { get; } =
        [
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Implementation.Refinement.VoterIdentityResolver",
                "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver"),
            new(Architecture + "CategoryListOwnershipGuardTest.cs",
                OldImplementationFolder + "RefinementList.cs",
                NewApplicationFolder + "RefinementList.cs"),
            new(Architecture + "RepositoryLeakRatchetArchUnitTest.cs",
                OldImplementationFolder + "RefinementList.cs",
                NewApplicationFolder + "RefinementList.cs"),
            new(Architecture + "TrackerIntegrationModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.Refinement.IRefinementViewQuery",
                "Lighthouse.Backend.Modules.Refinement.Application.IRefinementViewQuery"),

            // The platform lane's list of files that stay in Services names each Refinement file; the row goes when its file moves.
            .. FilesThatLeaveTheStayingList(
                OldImplementationFolder,
                "NeedBand.cs",
                "RefinementCadenceCalendar.cs",
                "RefinementCalendar.cs",
                "RefinementList.cs",
                "RefinementNeedCalculator.cs",
                "RefinementResolution.cs",
                "RefinementViewQuery.cs",
                "SizingLogCommands.cs",
                "SizingRefusal.cs",
                "SleYardstickResolver.cs",
                "StageRuleHealing.cs",
                "StageRuleMatcher.cs",
                "VoterIdentityResolver.cs"),
            .. FilesThatLeaveTheStayingList(
                OldInterfacesFolder,
                "IRefinementCalendar.cs",
                "IRefinementViewQuery.cs",
                "ISizingLogCommands.cs"),
        ];

        private static IEnumerable<RefinementRename> FilesThatLeaveTheStayingList(string folder, params string[] files)
            => files.Select(file => new RefinementRename(Architecture + "PlatformLaneUnitTestPlacementTest.cs", folder + file, NewText: null));
    }
}
