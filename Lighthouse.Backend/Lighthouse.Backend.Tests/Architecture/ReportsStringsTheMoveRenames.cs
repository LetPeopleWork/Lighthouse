namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A string in a test file that names a moving Reports type, file or namespace by its old Services path, and what the
    /// move makes of it: the text it reads after the move, or no text when the move deletes the string.
    /// </summary>
    internal sealed record ReportsRename(string File, string OldText, string? NewText)
    {
        public RenamedString AsRenamedString() => new(File, OldText, NewText ?? string.Empty);
    }

    /// <summary>
    /// Every string in the test project that names Reports by its old Services path when the Reports types are about to move
    /// into Lighthouse.Backend.Modules.Reports. The move renamed or deleted each of them, so the list is empty.
    /// </summary>
    internal static class ReportsStringsTheMoveRenames
    {
        public static IReadOnlyList<ReportsRename> Entries { get; } = [];
    }
}
