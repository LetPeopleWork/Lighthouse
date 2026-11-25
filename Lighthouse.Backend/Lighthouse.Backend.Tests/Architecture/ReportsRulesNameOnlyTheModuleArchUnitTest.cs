using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.ReportsRulesNameOnlyTheModuleFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Reports now lives in Lighthouse.Backend.Modules.Reports, so a rule that still names it by its former Services
    /// namespaces or folders keeps a dead alternative that sees nothing, and a rule that points only there switches itself off.
    /// Every rule constant of the test assembly (read by reflection) and every string literal of the test project sources
    /// is therefore swept for the four former forms: the plain name, a regex alternative, a member of a group such as
    /// (Reports|Refinement), and the old folder path. The only files allowed to keep one are the measurement files that record
    /// the move, and that list is compared both ways: a file with a former form that is not listed fails, and a listed file
    /// with no former form left fails too. Each narrowed Reports pattern must still match a type of the module.
    /// Made-up offenders are declared fixtures, so the sweep is shown to fail before it is trusted to pass.
    /// </summary>
    [TestFixture]
    public class ReportsRulesNameOnlyTheModuleArchUnitTest
    {
        private const string TheFixtureNamespace = "Lighthouse.Backend.Tests.Architecture.ReportsRulesNameOnlyTheModuleFixtures";

        private const string TheModulePrefix = @"^Lighthouse\.Backend\.Modules\.Reports";

        // The one deliberate former form of this file: it keeps the old-folder case readable to the sweep, and is why this file is listed.
        private const string TheOldFolderSpelledOut = "Services/Implementation/Reports";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] MeasurementFiles =
        [
            "Architecture/ReportsMembership.cs",
            "Architecture/ReportsMembershipArchUnitTest.cs",
            $"Architecture/{nameof(ReportsRulesNameOnlyTheModuleArchUnitTest)}.cs",
        ];

        private static readonly string[] TypesThatOwnAReportsRule =
        [
            "ModuleBoundariesArchUnitTest",
            "TrackerIntegrationModuleBoundariesArchUnitTest",
            "AccessModuleBoundariesArchUnitTest",
            "LicensingModuleBoundariesArchUnitTest",
        ];

        private static readonly string[] TheOldAccessAndLicensingGuard = ["Auth", "Authorization", "Licensing"];

        private static readonly string[] TheStaleOnes = ["Architecture/MadeUpFinished.cs", "Architecture/MadeUpAbsent.cs"];

        private const int TheNumberOfReportsRuleConstantsTheDesignNames = 4;

        private static readonly Regex TheFormerReportsForms = BuildTheFormerForms();

        private static readonly Regex TheStringLiterals = new(
            "'(?:[^'\\\\]|\\\\.)'|\"\"\"[\\s\\S]*?\"\"\"|@\"(?:[^\"]|\"\")*\"|\"(?:[^\"\\\\\\r\\n]|\\\\.)*\"",
            RegexOptions.CultureInvariant, PatternTimeout);

        [Test]
        public void NoRuleConstantNamesReportsByItsFormerPlace()
        {
            var sources = TheTestProjectSources();
            var types = TheTestAssemblyTypes(excludeFixtures: true);

            var offenders = FormerFormConstants(types, sources, MeasurementFiles);

            Assert.That(offenders, Is.Empty,
                $"{offenders.Count} rule constant(s) of the test assembly (out of {types.Count} types read) still name Reports by a former Services namespace or path:" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders) + Environment.NewLine +
                "Why it matters: the former alternative sees nothing now that Reports lives in its module, so it hides a rule that no longer constrains what it was written for. " +
                "How to fix: name Reports by Lighthouse.Backend.Modules.Reports only, and keep the rule's verdict.");
        }

        [Test]
        public void NoStringLiteralOutsideTheMeasurementFilesNamesReportsByItsFormerPlace()
        {
            var sources = TheTestProjectSources();

            var verdict = Sweep(sources, MeasurementFiles);

            Assert.That(verdict.Offending, Is.Empty,
                $"{verdict.Offending.Count} string literal(s) in the {sources.Count} files of the test project still name Reports by a former Services namespace or path:" + Environment.NewLine +
                string.Join(Environment.NewLine, verdict.Offending) + Environment.NewLine +
                "Why it matters: a rule written as a literal escapes reflection, so only this sweep sees that it still points at the former place. " +
                "How to fix: name Reports by Lighthouse.Backend.Modules.Reports only, or, for a file that records the move on purpose, list it as a measurement file in this test.");
        }

        [Test]
        public void TheListedMeasurementFilesAreExactlyTheFilesThatKeepAFormerForm()
        {
            var sources = TheTestProjectSources();

            var verdict = Sweep(sources, MeasurementFiles);

            Assert.That(verdict.Stale, Is.Empty,
                $"{verdict.Stale.Count} of the {MeasurementFiles.Length} listed measurement files have no former Reports form left or are not in the test project:" + Environment.NewLine +
                string.Join(Environment.NewLine, verdict.Stale) + Environment.NewLine +
                "Why it matters: an exemption that nothing needs any more would let a new former form hide in that file later. " +
                "How to fix: take the file off the measurement list in this test.");
        }

        [Test]
        public void EveryNarrowedReportsPatternStillMatchesATypeOfTheModule()
        {
            var moduleTypes = TheModuleTypeNames();
            var constants = ReportsModuleConstants(TheTestAssemblyTypes(excludeFixtures: true));

            var owners = constants.Select(constant => constant.Owner).Distinct().ToList();
            var dead = DeadModuleAlternatives(constants, moduleTypes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(constants, Has.Count.GreaterThanOrEqualTo(TheNumberOfReportsRuleConstantsTheDesignNames),
                    $"Only {constants.Count} rule constants name Modules.Reports, and the design names {TheNumberOfReportsRuleConstantsTheDesignNames} (the shared pattern, the Tracker integration list, and the Access and Licensing lists)." + Environment.NewLine +
                    "Why it matters: a narrowed rule that disappeared would pass this check for free. How to fix: restore the missing rule constant or its Modules.Reports alternative.");
                Assert.That(owners, Is.SupersetOf(TypesThatOwnAReportsRule),
                    $"The rule constants naming Modules.Reports come from [{string.Join(", ", owners)}], and the design names [{string.Join(", ", TypesThatOwnAReportsRule)}]." + Environment.NewLine +
                    "Why it matters: a rule file whose Reports part vanished no longer constrains Reports. How to fix: keep the Modules.Reports alternative in every owner.");
                Assert.That(dead, Is.Empty,
                    $"{dead.Count} Modules.Reports alternative(s) match none of the {moduleTypes.Count} types of the module:" + Environment.NewLine +
                    string.Join(Environment.NewLine, dead) + Environment.NewLine +
                    "Why it matters: an alternative that matches no Reports type constrains nothing, so the rule passes without looking. " +
                    "How to fix: anchor the alternative so it matches the Reports type it was written for.");
            }
        }

        [Test]
        public void MadeUpRuleConstantsInEachFormerFormAreNamedAndAModulesOnlyConstantIsNot()
        {
            var types = TheTestAssemblyTypes(excludeFixtures: false).Where(InFixtures).ToList();

            var offenders = FormerFormConstants(types, [], []);

            Assert.That(offenders.Select(FieldOf), Is.EquivalentTo(new[]
                {
                    $"{nameof(PlainNameOffender)}.{nameof(PlainNameOffender.Rule)}",
                    $"{nameof(RegexAlternativeOffender)}.{nameof(RegexAlternativeOffender.Rule)}",
                    $"{nameof(GroupMemberOffender)}.{nameof(GroupMemberOffender.Rule)}",
                    $"{nameof(OldFolderPathOffender)}.{nameof(OldFolderPathOffender.Rule)}",
                    $"{nameof(JointPatternOffender)}.{nameof(JointPatternOffender.Rule)}",
                }),
                $"The sweep over {types.Count} made-up fixture types named [{string.Join(", ", offenders.Select(FieldOf))}], and should name the five former-form offenders and not the Modules-only rule." + Environment.NewLine +
                "Why it matters: a sweep that misses a former form, or flags a clean rule, proves nothing about the real rules. How to fix: make the sweep read every former form through its regular expression.");
        }

        [TestCase("plain name")]
        [TestCase("regex alternative")]
        [TestCase("group member")]
        [TestCase("old folder path")]
        public void AMadeUpLiteralInAFormerFormOutsideTheMeasurementFilesIsNamed(string form)
        {
            var spelled = SpelledForms()[form];
            var files = new List<SourceFile>
            {
                new("Architecture/MadeUpOffender.cs", $"private const string Rule = @\"{spelled}\";"),
                new("Architecture/MadeUpClean.cs", $"private const string Rule = @\"{ModulesOnlyRule.Rule}\";"),
            };

            var verdict = Sweep(files, []);

            Assert.That(verdict.Offending, Has.Count.EqualTo(1).And.All.StartWith("Architecture/MadeUpOffender.cs"),
                $"For the made-up {form} literal the sweep named [{string.Join("; ", verdict.Offending)}], and should name only the offending file out of the {files.Count} read." + Environment.NewLine +
                "Why it matters: a sweep blind to this form lets a rule keep pointing at the former place. How to fix: read this form of literal.");
        }

        [Test]
        public void AListedMeasurementFileWithNoFormerFormLeftIsNamedAsStale()
        {
            var files = new List<SourceFile>
            {
                new("Architecture/MadeUpRecord.cs", $"private const string Old = @\"{PlainNameOffender.Rule}\";"),
                new("Architecture/MadeUpFinished.cs", $"private const string Rule = @\"{ModulesOnlyRule.Rule}\";"),
            };

            var verdict = Sweep(files, ["Architecture/MadeUpRecord.cs", "Architecture/MadeUpFinished.cs", "Architecture/MadeUpAbsent.cs"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Offending, Is.Empty, "A listed file that keeps a former form is allowed, and the sweep named it as an offender. How to fix: honour the measurement list.");
                Assert.That(verdict.Stale, Is.EquivalentTo(TheStaleOnes),
                    $"Of three listed files the sweep named [{string.Join(", ", verdict.Stale)}] as stale, and should name the one with no former form and the one that is not in the project." + Environment.NewLine +
                    "Why it matters: a stale exemption lets a new former form hide in that file. How to fix: compare the measurement list with the files that keep a former form, both ways.");
            }
        }

        [Test]
        public void AMadeUpPatternAnchoredToNoReportsTypeIsNamedDead()
        {
            var moduleTypes = TheModuleTypeNames();
            var constants = ReportsModuleConstants(TheTestAssemblyTypes(excludeFixtures: false).Where(InFixtures).ToList());

            var dead = DeadModuleAlternatives(constants, moduleTypes);

            Assert.That(dead, Has.Count.EqualTo(1).And.All.Contain(nameof(DeadModulesAlternative)),
                $"Of {constants.Count} made-up Modules.Reports constants, the check named [{string.Join("; ", dead)}], and should name only the one anchored to a type that does not exist." + Environment.NewLine +
                "Why it matters: an alternative that matches nothing makes the rule pass without looking. How to fix: match each alternative against the types of the module.");
        }

        [Test]
        public void TheJointTrackerIntegrationPatternNamesReportsOnlyByTheModule_AndKeepsTheOldAccessAndLicensingGuard()
        {
            var joint = TheJointTrackerIntegrationPattern();

            var reportsUnderServices = ReportsUnderServicesIn(joint);
            var guardMissing = AccessAndLicensingGuardMissingFrom(joint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reportsUnderServices, Is.Empty,
                    $"The joint Tracker integration pattern still names Reports under a former Services namespace in [{string.Join("; ", reportsUnderServices)}]." + Environment.NewLine +
                    "Why it matters: Reports no longer has types there, so that alternative matches nothing and only looks like a rule. " +
                    "How to fix: drop Reports from the Services alternative and keep it by its module only.");
                Assert.That(joint, Does.Contain(@"Modules\.Reports"),
                    "The joint Tracker integration pattern no longer names the Reports module." + Environment.NewLine +
                    "Why it matters: Tracker integration would then be free to reach Reports. How to fix: keep the Modules.Reports alternative.");
                Assert.That(guardMissing, Is.Empty,
                    $"The joint Tracker integration pattern no longer names [{string.Join(", ", guardMissing)}] under a former Services namespace." + Environment.NewLine +
                    "Why it matters: the Access and Licensing checks need Tracker integration to keep catching a connector that reaches the old Access and Licensing places. " +
                    "How to fix: keep the Auth, Authorization and Licensing alternatives under Services and drop only Reports.");
            }
        }

        [Test]
        public void AMadeUpJointPatternKeepingReportsUnderServicesOrDroppingTheGuardIsNamedAndTheNarrowedOneIsNot()
        {
            var keepsReports = ReportsUnderServicesIn(JointPatternOffender.Rule);
            var narrowed = ReportsUnderServicesIn(JointPatternKeepingOnlyTheGuard.Rule);
            var guardKept = AccessAndLicensingGuardMissingFrom(JointPatternKeepingOnlyTheGuard.Rule);
            var guardDropped = AccessAndLicensingGuardMissingFrom(ModulesOnlyRule.Rule);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keepsReports, Is.Not.Empty,
                    "The check did not name Reports under Services in the made-up joint pattern that keeps it. Why it matters: a check blind to it lets the dead alternative stay. How to fix: read every alternative that starts under Services.");
                Assert.That(narrowed, Is.Empty,
                    $"The check named [{string.Join("; ", narrowed)}] in a pattern that keeps only Auth, Authorization and Licensing under Services. How to fix: flag only Reports.");
                Assert.That(guardKept, Is.Empty,
                    $"The check reported [{string.Join(", ", guardKept)}] missing from a pattern that keeps the whole guard. How to fix: look for each name among the Services alternatives.");
                Assert.That(guardDropped, Is.EquivalentTo(TheOldAccessAndLicensingGuard),
                    $"The check reported [{string.Join(", ", guardDropped)}] missing from a pattern that dropped the whole guard, and should name all three. Why it matters: a check blind to a lost guard lets Tracker integration reach the old Access and Licensing places. How to fix: look for each name among the Services alternatives.");
            }
        }

        private static string TheJointTrackerIntegrationPattern()
        {
            var owner = typeof(ReportsRulesNameOnlyTheModuleArchUnitTest).Assembly.GetTypes()
                .Single(type => type.Name == "TrackerIntegrationModuleBoundariesArchUnitTest");
            var field = owner.GetField("RefinementReportsAccessAndLicensing", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            Assert.That(field, Is.Not.Null, "The joint Tracker integration pattern constant was not found. How to fix: keep it under its name in TrackerIntegrationModuleBoundariesArchUnitTest.");
            return (string)(field!.IsLiteral ? field.GetRawConstantValue()! : field.GetValue(null)!);
        }

        private static List<string> ServicesAlternativesIn(string pattern)
            => [.. TopLevelAlternatives(pattern).Where(alternative => alternative.Contains(@"Services\.", StringComparison.Ordinal))];

        private static List<string> ReportsUnderServicesIn(string pattern)
            => [.. ServicesAlternativesIn(pattern).Where(alternative => TheFormerReportsForms.IsMatch(alternative))];

        private static List<string> AccessAndLicensingGuardMissingFrom(string pattern)
        {
            var services = string.Join("|", ServicesAlternativesIn(pattern));
            return [.. TheOldAccessAndLicensingGuard
                .Where(name => !Regex.IsMatch(services, $@"\b{name}\b", RegexOptions.CultureInvariant, PatternTimeout))];
        }

        private static Regex BuildTheFormerForms()
        {
            var separator = @"\\*[./]";
            var layer = @"(?:Implementation|Interfaces|\((?:Implementation\|Interfaces|Interfaces\|Implementation)\))";
            var area = @"(?:Reports\b|\([^)""]*\bReports\b[^)""]*\))";
            return new Regex(layer + separator + area, RegexOptions.CultureInvariant, PatternTimeout);
        }

        private static Dictionary<string, string> SpelledForms() => new()
        {
            ["plain name"] = PlainNameOffender.Rule,
            ["regex alternative"] = RegexAlternativeOffender.Rule,
            ["group member"] = GroupMemberOffender.Rule,
            ["old folder path"] = OldFolderPathOffender.Rule,
        };

        private sealed record SourceFile(string Path, string Text);

        private sealed record Constant(string Owner, string Field, string Value);

        private sealed record SweepVerdict(List<string> Offending, List<string> Stale);

        private static string FieldOf(string offender) => offender[..offender.IndexOf(" = ", StringComparison.Ordinal)];

        private static List<string> FormerFormsIn(string text)
            => [.. TheStringLiterals.Matches(text).Select(literal => TheFormerReportsForms.Match(literal.Value)).Where(found => found.Success).Select(found => found.Value)];

        private static SweepVerdict Sweep(IReadOnlyCollection<SourceFile> files, IReadOnlyCollection<string> measurementFiles)
        {
            var found = files.Select(file => (file.Path, Forms: FormerFormsIn(file.Text))).Where(file => file.Forms.Count > 0).ToList();

            var offending = found.Where(file => !measurementFiles.Contains(file.Path, StringComparer.Ordinal))
                .Select(file => $"{file.Path}: {string.Join(", ", file.Forms.Distinct())}")
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToList();
            var stale = measurementFiles.Where(listed => found.All(file => file.Path != listed)).OrderBy(listed => listed, StringComparer.Ordinal).ToList();

            return new SweepVerdict(offending, stale);
        }

        private static List<Constant> StringConstantsOf(IEnumerable<Type> types)
            => [.. from type in types
                where !type.ContainsGenericParameters
                from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                where field.FieldType == typeof(string) && (field.IsLiteral || field.IsInitOnly)
                let value = (field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null)) as string
                where value is not null
                select new Constant(OutermostOf(type).Name, $"{OutermostOf(type).Name}.{field.Name}", value)];

        private static List<string> FormerFormConstants(IReadOnlyCollection<Type> types, IReadOnlyCollection<SourceFile> sources, IReadOnlyCollection<string> measurementFiles)
        {
            var recordedTypes = measurementFiles
                .SelectMany(listed => sources.Where(file => file.Path == listed))
                .Select(file => file.Text)
                .ToList();

            return [.. StringConstantsOf(types)
                .Where(constant => TheFormerReportsForms.IsMatch(constant.Value))
                .Where(constant => !recordedTypes.Any(text => Regex.IsMatch(text, $@"\b(class|record|struct|enum|interface)\s+{Regex.Escape(constant.Owner)}\b", RegexOptions.CultureInvariant, PatternTimeout)))
                .Select(constant => $"{constant.Field} = {constant.Value}")];
        }

        private static List<Constant> ReportsModuleConstants(IReadOnlyCollection<Type> types)
            => [.. StringConstantsOf(types).Where(constant => constant.Value.Contains(@"Modules\.Reports", StringComparison.Ordinal))];

        private static List<string> DeadModuleAlternatives(IEnumerable<Constant> constants, IReadOnlyCollection<string> moduleTypes)
            => [.. from constant in constants
                   from alternative in TopLevelAlternatives(constant.Value)
                   where alternative.StartsWith(TheModulePrefix, StringComparison.Ordinal)
                   where !moduleTypes.Any(name => Regex.IsMatch(name, alternative, RegexOptions.CultureInvariant, PatternTimeout))
                   select $"{constant.Field}: {alternative}"];

        private static List<string> TopLevelAlternatives(string pattern)
        {
            var parts = new List<string>();
            var depth = 0;
            var start = 0;

            var index = 0;
            while (index < pattern.Length)
            {
                var character = pattern[index];
                if (character == '\\')
                {
                    index += 2;
                    continue;
                }

                depth += character switch { '(' => 1, ')' => -1, _ => 0 };

                if (character == '|' && depth == 0)
                {
                    parts.Add(pattern[start..index]);
                    start = index + 1;
                }

                index++;
            }

            parts.Add(pattern[start..]);
            return parts;
        }

        private static List<string> TheModuleTypeNames()
        {
            var names = typeof(LighthouseAppContext).Assembly.GetTypes()
                .Select(type => type.FullName!)
                .Where(name => name.StartsWith("Lighthouse.Backend.Modules.Reports.", StringComparison.Ordinal))
                .ToList();

            Assert.That(names, Is.Not.Empty, "The compiled backend holds no type under Lighthouse.Backend.Modules.Reports, so no pattern can be shown to match one. How to fix: build the backend with the Reports module in place.");
            return names;
        }

        private static List<Type> TheTestAssemblyTypes(bool excludeFixtures)
        {
            var types = typeof(ReportsRulesNameOnlyTheModuleArchUnitTest).Assembly.GetTypes()
                .Where(type => !excludeFixtures || !InAnyFixtures(type))
                .ToList();

            Assert.That(types, Has.Count.GreaterThan(50), $"Only {types.Count} types were read from the test assembly, so the sweep would pass without reading the rules.");
            return types;
        }

        private static bool InAnyFixtures(Type type) => type.Namespace?.Split('.').Any(part => part.EndsWith("Fixtures", StringComparison.Ordinal)) == true;

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(TheFixtureNamespace, StringComparison.Ordinal) == true;

        private static Type OutermostOf(Type type)
        {
            while (type.DeclaringType is not null)
            {
                type = type.DeclaringType;
            }

            return type;
        }

        private static List<SourceFile> TheTestProjectSources()
        {
            var root = TheTestProjectRoot();
            var sep = new[] { Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !sep.Any(part => path.Contains(part, StringComparison.Ordinal)))
                .Select(path => new SourceFile(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(50), $"Only {files.Count} source files were found under {root}, so the sweep would pass without reading the rules.");
            return files;
        }

        private static string TheTestProjectRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the test sources cannot be read.");
            return Path.Combine(directory!.FullName, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ReportsRulesNameOnlyTheModuleFixtures
{
    // Made-up rule constants; each former form is assembled from pieces so no single literal spells it.
    internal static class PlainNameOffender
    {
        public const string Rule = "Lighthouse.Backend.Services." + "Implementation" + "." + "Reports";
    }

    internal static class RegexAlternativeOffender
    {
        public const string Rule = @"^Lighthouse\.Backend\.Services\." + @"(Implementation|Interfaces)" + @"\.Reports($|\..*)";
    }

    internal static class GroupMemberOffender
    {
        public const string Rule = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\." + @"(Reports|Refinement)(\.|$)";
    }

    internal static class OldFolderPathOffender
    {
        public const string Rule = "Lighthouse.Backend/Services/" + "Implementation" + "/" + "Reports";
    }

    internal static class JointPatternOffender
    {
        public const string Rule = @"^Lighthouse\.Backend\.Services\." + @"(Implementation|Interfaces)" + @"\.(Reports|Auth|Authorization|Licensing)(\.|$)|^Lighthouse\.Backend\.Modules\.Reports(\.|$)";
    }

    internal static class JointPatternKeepingOnlyTheGuard
    {
        public const string Rule = @"^Lighthouse\.Backend\.Services\." + @"(Implementation|Interfaces)" + @"\.(Auth|Authorization|Licensing)(\.|$)|^Lighthouse\.Backend\.Modules\.Reports(\.|$)";
    }

    internal static class ModulesOnlyRule
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\.Reports(\.|$)";
    }

    internal static class DeadModulesAlternative
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\.Reports\.Nowhere($|\+.*)";
    }
}
