namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>One production source file: its path under the production project and its text.</summary>
    internal sealed record ProductionSourceFile(string RelativePath, string Source);

    /// <summary>
    /// The production source tree as a rule can read it. Finds every source file of the production
    /// project (build output left out) and can blank comments, so a rule reads code and not prose.
    /// </summary>
    // SCAFFOLD: true
    internal static class ProductionSource
    {
        /// <summary>Every production source file, paths relative to the production project with forward slashes.</summary>
        public static IReadOnlyList<ProductionSourceFile> Files()
            => throw new AssertionException("SCAFFOLD: ProductionSource.Files is not built yet");

        /// <summary>The source with every comment blanked, line breaks kept so a line number still points at the right line.</summary>
        public static string WithoutComments(string source)
            => throw new AssertionException("SCAFFOLD: ProductionSource.WithoutComments is not built yet");
    }
}
