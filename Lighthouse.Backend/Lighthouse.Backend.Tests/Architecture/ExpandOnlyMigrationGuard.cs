using System.Globalization;
using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    public static class ExpandOnlyMigrationGuard
    {
        private static readonly string[] DestructiveOperations =
        {
            "DropColumn",
            "DropTable",
            "RenameColumn",
            "RenameTable",
        };

        public static IReadOnlyList<string> FindDestructiveOperationsInUp(string migrationSource)
        {
            var upBody = ExtractUpMethodBody(migrationSource);

            return DestructiveOperations
                .Where(op => upBody.Contains($".{op}(", StringComparison.Ordinal))
                .ToList();
        }

        public static string ExtractUpMethodBody(string migrationSource)
        {
            var upStart = migrationSource.IndexOf("void Up(", StringComparison.Ordinal);
            if (upStart < 0)
            {
                return string.Empty;
            }

            var downStart = migrationSource.IndexOf("void Down(", upStart, StringComparison.Ordinal);
            return downStart < 0
                ? migrationSource[upStart..]
                : migrationSource[upStart..downStart];
        }

        public static long? TimestampOf(string migrationFileName)
        {
            var match = Regex.Match(
                Path.GetFileName(migrationFileName),
                @"^(\d{14})_",
                RegexOptions.None,
                TimeSpan.FromSeconds(1));

            return match.Success
                ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : null;
        }
    }
}
