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
    /// Every string in the test project that names Refinement by its old Services path. The Refinement types have moved into
    /// Lighthouse.Backend.Modules.Refinement and every such string was renamed or deleted with them, so the list is empty.
    /// </summary>
    internal static class RefinementStringsTheMoveRenames
    {
        public static IReadOnlyList<RefinementRename> Entries { get; } = [];
    }
}
