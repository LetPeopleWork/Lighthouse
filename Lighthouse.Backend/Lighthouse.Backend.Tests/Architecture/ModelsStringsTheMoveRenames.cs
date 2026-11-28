namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every string of the test project that names a free Models type, or its file, by its Models name, with the text it
    /// reads once the type has moved to its owner. The list only shrinks: an entry leaves when its type has moved and the
    /// string was renamed.
    /// </summary>
    internal static class ModelsStringsTheMoveRenames
    {
        public static IReadOnlyList<RenamedString> Entries { get; } = [];
    }
}
