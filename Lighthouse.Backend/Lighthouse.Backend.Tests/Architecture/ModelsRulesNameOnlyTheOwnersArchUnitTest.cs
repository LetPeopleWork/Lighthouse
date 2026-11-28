using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A type that left the Models for its owner is named by its new namespace only. Every rule pattern of the test assembly
    /// (read by reflection) and every source file of the test project is swept for a moved type's former Models name, with
    /// no movers list and no rename list to excuse one. A pattern that names a former Models place on purpose is declared an
    /// old location in the Models pattern roles; the only file that may spell the former names is this one, and that list is
    /// compared with the expected paths. Made-up patterns and sources show the sweep fails before it is trusted to pass.
    /// </summary>
    [TestFixture]
    public class ModelsRulesNameOnlyTheOwnersArchUnitTest
    {
        private static string TheModelsRoot => "Lighthouse.Backend.Models";

        private const string TheOracleFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ModelsRulesNameOnlyTheOwnersArchUnitTest.cs";

        private static readonly string[] FilesThatMaySpellAFormerModelsName = [TheOracleFile];

        private static readonly string[] TheOnlyAllowedFile = [TheOracleFile];

        private static readonly string[] ThePatternsTheSweepShouldName = ["MadeUp.WidenedAlternative", "MadeUp.ModelsNameOfARootType"];

        private static readonly string[] TheSourcesTheSweepShouldName = ["Architecture/MadeUpOffender.cs", "Architecture/MadeUpEscaped.cs"];

        // Every type that left the Models, with the namespace it left. A type of the root Models namespace has none of its own.
        private static readonly (string Name, string OldNamespace)[] TheMovedTypes =
        [
            ("AdditionalFieldInUse", TheModelsRoot),
            ("AppSettingKeys", TheModelsRoot + ".AppSettings"),
            ("ConnectionFieldDefault", TheModelsRoot),
            ("ConnectionFieldDefaults", TheModelsRoot),
            ("CurrentUserProfileStatus", TheModelsRoot + ".Auth"),
            ("DefaultHeldFor", TheModelsRoot),
            ("DefaultNamingAField", TheModelsRoot),
            ("DeliverySourceBindability", TheModelsRoot + ".DeliverySources"),
            ("EmbedSessionTokenRedemption", TheModelsRoot + ".Auth"),
            ("ManualRankComparer", TheModelsRoot),
            ("OptionalFeatureKeys", TheModelsRoot + ".OptionalFeatures"),
            ("OwnerNamingFields", TheModelsRoot),
            ("OwnerTerms", TheModelsRoot),
            ("OwnersWhoseFetchMoved", TheModelsRoot),
            ("PortfolioSize", TheModelsRoot),
            ("RemovedAdditionalField", TheModelsRoot),
            ("ReportOwnerGoneException", TheModelsRoot + ".Reports"),
            ("RuleSetSemantics", TheModelsRoot + ".WorkItemRules"),
            ("UsageDataEmitPermit", TheModelsRoot + ".UsageData"),
            ("UsageDataEventReported", TheModelsRoot + ".UsageData"),
            ("UsageDataEventShapes", TheModelsRoot + ".UsageData"),
            ("UsageDataSuppressionReason", TheModelsRoot + ".UsageData"),
        ];

        [Test]
        public void NoMoversAndNoRenamedStringsAreLeftToExcuseAFormerName()
        {
            var movers = KernelCatalogue.Movers.Select(mover => mover.OldFullName).ToList();
            var renames = ModelsStringsTheMoveRenames.Entries.Select(entry => $"{entry.File}: {entry.OldText}").ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(movers, Is.Empty,
                    $"The movers list still holds {movers.Count} type(s): [{string.Join(", ", movers)}]." + Environment.NewLine +
                    "Why it matters: a listed mover lets a rule keep its former Models name. How to fix: move the type and take its entry off the list.");
                Assert.That(renames, Is.Empty,
                    $"The rename list still holds {renames.Count} string(s): [{string.Join("; ", renames)}]." + Environment.NewLine +
                    "Why it matters: a renamed string means a rule still spells a former Models name. How to fix: spell the type by its new namespace and empty the list.");
            }
        }

        [Test]
        public void NoRulePatternNamesAMovedTypeByItsModelsNameUnlessItIsDeclaredAnOldLocation()
        {
            var patterns = TheRulePatternsOfTheTestAssembly();

            var offenders = PatternsNamingAMovedTypeByItsModelsName(patterns, TheOldLocations());

            Assert.That(offenders, Is.Empty,
                $"{offenders.Count} of the {patterns.Count} rule patterns of the test assembly still name a moved type by its Models name:" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders) + Environment.NewLine +
                "Why it matters: the Models alternative added for the move sees nothing now that the type lives with its owner, so it hides a rule that no longer constrains what it was written for. " +
                "How to fix: name the owner by its module or platform namespace only, and keep the rule's verdict.");
        }

        [Test]
        public void NoSourceFileOfTheTestProjectNamesAMovedTypeByItsModelsName()
        {
            var sources = TheSourcesOfTheTestProject();

            var offenders = SourcesNamingAMovedTypeByItsModelsName(sources, FilesThatMaySpellAFormerModelsName);

            Assert.That(offenders, Is.Empty,
                $"{offenders.Count} of the {sources.Count} source files of the test project still name a moved type by its Models name:" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders) + Environment.NewLine +
                "Why it matters: a literal that points at the former Models place matches nothing and only looks like a rule. " +
                "How to fix: write the type's new namespace.");
        }

        [Test]
        public void TheOnlyFileAllowedToSpellAFormerModelsNameIsThisOracle()
        {
            var sources = TheSourcesOfTheTestProject().Select(source => source.RelativePath).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FilesThatMaySpellAFormerModelsName, Is.EquivalentTo(TheOnlyAllowedFile),
                    $"The files allowed to spell a former Models name are [{string.Join(", ", FilesThatMaySpellAFormerModelsName)}], and only this oracle should be." + Environment.NewLine +
                    "Why it matters: a wider list is an allowance a rule could hide in. How to fix: take every other file off the list.");
                Assert.That(sources, Does.Contain(TheOracleFile),
                    $"The allowed file {TheOracleFile} is not among the {sources.Count} files of the test project." + Environment.NewLine +
                    "Why it matters: an exemption for a file that does not exist lets the path be reused later. How to fix: correct the path.");
            }
        }

        [Test]
        public void AMadeUpPatternKeepingAModelsAlternativeForAMovedTypeIsNamedAndNarrowedDeclaredOrWholeKernelPatternsAreNot()
        {
            var patterns = new List<RulePattern>
            {
                new("MadeUp.WidenedAlternative", @"^Lighthouse\.Backend\.(Models\.Reports|Modules\.Reports\.Domain)($|\..*)"),
                new("MadeUp.ModelsNameOfARootType", @"^Lighthouse\.Backend\.Models\.PortfolioSize$"),
                new("MadeUp.NarrowedToTheModule", @"^Lighthouse\.Backend\.Modules\.Reports\.Domain($|\..*)"),
                new("MadeUp.AnOldLocationDeclaredAsSuch", @"^Lighthouse\.Backend\.Models\.Auth(\.|$)"),
                new("MadeUp.TheWholeKernel", @"^(System|Lighthouse\.Backend\.Models)(\.|$)"),
                new("MadeUp.NeverNamesTheModels", @"^Lighthouse\.Backend\.Modules\.Access(\.|$)"),
            };

            var offenders = PatternsNamingAMovedTypeByItsModelsName(patterns, new HashSet<string> { "MadeUp.AnOldLocationDeclaredAsSuch" });

            Assert.That(
                offenders.Select(offender => offender.Split(':')[0]),
                Is.EquivalentTo(ThePatternsTheSweepShouldName),
                $"Of {patterns.Count} made-up patterns the sweep named [{string.Join("; ", offenders)}], and should name the widened alternative and the root type's Models name only." + Environment.NewLine +
                "Why it matters: a sweep that misses one of them, or flags a narrowed or declared pattern, proves nothing about the real rules. How to fix: match each pattern against the former namespace and full name of every moved type.");
        }

        [Test]
        public void AMadeUpSourceNamingAMovedTypeByItsModelsNameIsNamedAndTheNewNameAndASiblingAreNot()
        {
            var files = new List<TestProjectSource>
            {
                new("Architecture/MadeUpOffender.cs", "private const string Old = \"Lighthouse.Backend.Models.Auth.CurrentUserProfileStatus\";"),
                new("Architecture/MadeUpEscaped.cs", @"private const string Old = @""^Lighthouse\.Backend\.Models\.PortfolioSize$"";"),
                new("Architecture/MadeUpNewName.cs", "private const string New = \"Lighthouse.Backend.Modules.Access.Domain.CurrentUserProfileStatus\";"),
                new("Architecture/MadeUpSibling.cs", "private const string Other = \"Lighthouse.Backend.Models.PortfolioSizeHistory\";"),
                new("Architecture/MadeUpAllowed.cs", "private const string Old = \"Lighthouse.Backend.Models.Auth.CurrentUserProfileStatus\";"),
            };

            var offenders = SourcesNamingAMovedTypeByItsModelsName(files, ["Architecture/MadeUpAllowed.cs"]);

            Assert.That(
                offenders.Select(offender => offender.Split(':')[0]),
                Is.EquivalentTo(TheSourcesTheSweepShouldName),
                $"Of {files.Count} made-up files the sweep named [{string.Join("; ", offenders)}], and should name the plain and the escaped spelling only." + Environment.NewLine +
                "Why it matters: a sweep blind to a spelling lets a rule keep pointing at the former place, and one that flags the new name or a longer name fails for the wrong reason. How to fix: match the whole former name, in either spelling, and nothing longer.");
        }

        private static List<string> PatternsNamingAMovedTypeByItsModelsName(IEnumerable<RulePattern> patterns, HashSet<string> declaredOldLocations)
            => [.. patterns
                .Where(pattern => !declaredOldLocations.Contains(pattern.Field))
                .Where(pattern => pattern.Value.Replace("\\.", ".", StringComparison.Ordinal).Contains("Models", StringComparison.Ordinal))
                .Select(pattern => (pattern.Field, Named: MovedTypesTheModelsPartOfARegexSees(pattern.Value)))
                .Where(found => found.Named.Count > 0)
                .Select(found => $"{found.Field}: sees the former Models name of {string.Join(", ", found.Named)}")
                .Order(StringComparer.Ordinal)];

        // A pattern that takes in the whole Models, the kernel itself, names no moved type; one that sees a moved type's former
        // place but not a Models namespace nothing ever lived in singles that place out.
        private static List<string> MovedTypesTheModelsPartOfARegexSees(string pattern)
        {
            try
            {
                var takesInTheWholeModels = Regex.IsMatch($"{TheModelsRoot}.NothingEverLivedHere", pattern, RegexOptions.None, NamespaceMoveLaw.PatternTimeout)
                    || Regex.IsMatch($"{TheModelsRoot}.NothingEverLivedHere.NeitherDidThis", pattern, RegexOptions.None, NamespaceMoveLaw.PatternTimeout);
                if (takesInTheWholeModels)
                {
                    return [];
                }

                return [.. TheMovedTypes
                    .Where(moved => Regex.IsMatch($"{moved.OldNamespace}.{moved.Name}", pattern, RegexOptions.None, NamespaceMoveLaw.PatternTimeout)
                        || (moved.OldNamespace != TheModelsRoot && Regex.IsMatch(moved.OldNamespace, pattern, RegexOptions.None, NamespaceMoveLaw.PatternTimeout)))
                    .Select(moved => $"{moved.OldNamespace}.{moved.Name}")];
            }
            catch (ArgumentException)
            {
                return [];
            }
        }

        private static List<string> SourcesNamingAMovedTypeByItsModelsName(IEnumerable<TestProjectSource> sources, IReadOnlyCollection<string> allowed)
            => [.. sources
                .Where(source => !allowed.Contains(source.RelativePath, StringComparer.Ordinal))
                .Select(source => (source.RelativePath, Named: MovedTypesNamedIn(source.Text)))
                .Where(found => found.Named.Count > 0)
                .Select(found => $"{found.RelativePath}: names the former Models name of {string.Join(", ", found.Named)}")
                .Order(StringComparer.Ordinal)];

        private static List<string> MovedTypesNamedIn(string text)
        {
            var plain = text.Replace("\\.", ".", StringComparison.Ordinal);
            return [.. TheMovedTypes
                .Select(moved => $"{moved.OldNamespace}.{moved.Name}")
                .Where(oldName => Regex.IsMatch(plain, $@"(?<![A-Za-z0-9_.]){Regex.Escape(oldName)}(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout))];
        }

        private static HashSet<string> TheOldLocations()
            => ModelsPatternRoles.PatternRoles.Where(role => role.Value.Role == PatternRole.Place).Select(role => role.Key).ToHashSet(StringComparer.Ordinal);

        private static List<RulePattern> TheRulePatternsOfTheTestAssembly()
        {
            var law = new NamespaceMoveLaw(
                new NamespaceMove([], [], string.Empty, new Regex("(?!)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout), []),
                new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));

            var patterns = law.RulePatternsOf(typeof(ModelsRulesNameOnlyTheOwnersArchUnitTest).Assembly.GetTypes()
                .Where(type => type != typeof(ModelsRulesNameOnlyTheOwnersArchUnitTest))
                .Where(type => type.Namespace?.Contains("Fixtures", StringComparison.Ordinal) != true));

            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the sweep would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of the test assembly's types.");
            return patterns;
        }

        private static List<TestProjectSource> TheSourcesOfTheTestProject()
        {
            var testProject = TheTestProjectFolder();
            var repositoryRoot = Directory.GetParent(Directory.GetParent(testProject)!.FullName)!.FullName;
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Select(path => new TestProjectSource(Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the sweep would pass without reading the test project. How to fix: read every .cs file of Lighthouse.Backend.Tests outside bin and obj.");
            return files;
        }

        private static string TheTestProjectFolder()
        {
            var folder = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Lighthouse.Backend.Tests.csproj")))
            {
                folder = folder.Parent;
            }

            return folder?.FullName ?? throw new InvalidOperationException("Lighthouse.Backend.Tests.csproj was not found above the test directory, so the sources of the test project cannot be read.");
        }

        private sealed record TestProjectSource(string RelativePath, string Text);
    }
}
