namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Reads the references out of a field a person maintains by hand. A parent field holds one value, so a
    /// connector can hand it straight on; a dependency field holds however many the writer typed, which is
    /// why this exists at all.
    /// </summary>
    public static class DependencyFieldReferences
    {
        private static readonly char[] BetweenReferences = [',', ';'];

        /// <summary>
        /// Both separators are accepted because both are what people type, and neither is worth a support
        /// case. Blanks are dropped rather than passed on as empty references: a trailing separator is a
        /// typing habit, not a dependency on nothing.
        /// </summary>
        public static IReadOnlyList<string> In(string? fieldValue)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
            {
                return [];
            }

            return fieldValue
                .Split(BetweenReferences, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }
}
