using System.Text;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>One production source file: its path under the production project and its text.</summary>
    internal sealed record ProductionSourceFile(string RelativePath, string Source);

    /// <summary>
    /// The production source tree as a rule can read it. Finds every source file of the production
    /// project (build output left out) and can blank comments, so a rule reads code and not prose.
    /// </summary>
    internal static class ProductionSource
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private static readonly string[] BuildOutputSegments = ["/bin/", "/obj/"];

        /// <summary>Every production source file, paths relative to the production project with forward slashes.</summary>
        public static IReadOnlyList<ProductionSourceFile> Files()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);

            var files = Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(productionRoot, file).Replace('\\', '/'))
                .Where(relativePath => !IsBuildOutput(relativePath))
                .Order(StringComparer.Ordinal)
                .Select(relativePath => new ProductionSourceFile(relativePath, File.ReadAllText(Path.Combine(productionRoot, relativePath))))
                .ToList();

            Assert.That(files, Is.Not.Empty, $"No production sources were found under {productionRoot}, so a scan of them would pass without looking at anything.");
            return files;
        }

        /// <summary>The source with every comment blanked, line breaks kept so a line number still points at the right line.</summary>
        public static string WithoutComments(string source)
        {
            var result = new StringBuilder(source.Length);
            var index = 0;

            while (index < source.Length)
            {
                index = StartsWith(source, index, "//") ? BlankLineComment(source, result, index)
                    : StartsWith(source, index, "/*") ? BlankBlockComment(source, result, index)
                    : source[index] == '"' ? CopyStringLiteral(source, result, index)
                    : Copy(source, result, index);
            }

            return result.ToString();
        }

        private static bool IsBuildOutput(string relativePath)
        {
            var probe = "/" + relativePath;
            return BuildOutputSegments.Any(segment => probe.Contains(segment, StringComparison.Ordinal));
        }

        private static int Copy(string source, StringBuilder result, int index)
        {
            result.Append(source[index]);
            return index + 1;
        }

        private static int BlankLineComment(string source, StringBuilder result, int index)
        {
            while (index < source.Length && source[index] != '\n')
            {
                result.Append(' ');
                index++;
            }

            return index;
        }

        private static int BlankBlockComment(string source, StringBuilder result, int index)
        {
            result.Append("  ");
            index += 2;

            while (index < source.Length && !StartsWith(source, index, "*/"))
            {
                result.Append(source[index] == '\n' ? '\n' : ' ');
                index++;
            }

            if (index < source.Length)
            {
                result.Append("  ");
                index += 2;
            }

            return index;
        }

        // A string is copied untouched so a slash pair inside it is never taken for a comment.
        private static int CopyStringLiteral(string source, StringBuilder result, int index)
        {
            var isVerbatim = index > 0 && source[index - 1] == '@';
            result.Append(source[index]);
            index++;

            while (index < source.Length)
            {
                var current = source[index];
                result.Append(current);
                index++;

                if (current == '\\' && !isVerbatim && index < source.Length)
                {
                    result.Append(source[index]);
                    index++;
                }
                else if (current == '"' && isVerbatim && index < source.Length && source[index] == '"')
                {
                    result.Append(source[index]);
                    index++;
                }
                else if (current == '"' || (current == '\n' && !isVerbatim))
                {
                    break;
                }
            }

            return index;
        }

        private static bool StartsWith(string source, int index, string token)
            => string.CompareOrdinal(source, index, token, 0, token.Length) == 0;

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the production source scan.");
            return directory!.FullName;
        }
    }
}
