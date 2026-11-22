using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneRulesSeeTheMoveFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The platform types are about to move out of Services into Lighthouse.Backend.Platform. Every architecture rule that
    /// finds them by name must give the same answer after the move as before, or the move quietly switches the rule off: a
    /// rule that looks only at the old place passes because it no longer finds anything there.
    /// <para>
    /// Which strings count as rule patterns: every constant string field of this test assembly, in any class and any nested
    /// class, that parses as a regular expression, names Lighthouse.Backend, and is either anchored at its start (a namespace
    /// pattern, wherever it is declared) or declared among the architecture rules. A bare word used as a label or test data
    /// elsewhere ("Encryption", "Implementation", a team called Platform) is not a rule and is not asked. A message or a file
    /// path answers no for both names of every moving type, so it passes without a list of exceptions. A constant that
    /// names one type or one file by its full name or path, rather than a namespace, is on the membership map's list of
    /// strings the move renames and is left to the move that renames it: widening it would make it name two types.
    /// </para>
    /// <para>
    /// Strings that are not constants escape reflection, so the rule files are also read as text. A string there may not
    /// name a moving area as a whole, or a moving type, by its old Services path, unless the membership map lists it as
    /// renamed by a move. Strings that name a type or test that stays where it is (an update pipeline, a handler that
    /// already lives in its module, a test class) are not about the move and pass, as do the rule patterns the first check
    /// already governs, which keep their old alternatives until the old places are empty.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PlatformLaneRulesSeeTheMoveArchUnitTest
    {
        private static readonly string ThePlatformNamespace = @"^Lighthouse\.Backend\.Platform(\.|$)";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string FixtureNamespace = typeof(MadeUpRuleKeys).Namespace!;

        private static readonly string[] MovingAreasOfServices =
            ["Encryption", "DatabaseManagement", "UsageData", "Logging", "DomainEvents", "BackgroundServices.Update", "Update"];

        private static readonly Regex StringLiteral = new(
            @"'(?:[^'\\\r\n]|\\.)'|(?:\$@|@\$|@)""(?<verbatim>(?:[^""]|"""")*)""|\$?""(?<regular>(?:[^""\\\r\n]|\\.)*)""",
            RegexOptions.None,
            PatternTimeout);

        private static readonly Regex PlatformToken = new(@"(?<=\\\.|\(|\|)Platform(?![A-Za-z0-9_])", RegexOptions.None, PatternTimeout);

        private static readonly Regex NamesTheBackend = new(@"Lighthouse\\?\.Backend", RegexOptions.None, PatternTimeout);

        private static readonly string RuleNamespace = typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Namespace!;

        private static readonly Lazy<List<Regex>> OldPathReferences = new(() => [.. OldPathReferencesOfTheMap()]);

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }

            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void EveryRulePatternOfTheTestAssembly_AnswersTheSameForEachMovingPlatformType_UnderItsOldAndItsNewNamespace()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var blind = WhenEachPatternIsAskedAboutEveryMovingTypeUnderBothNames(patterns);

            ThenNoPatternAnswersDifferently(
                blind,
                "a rule that matches a platform type under its old namespace and not under Lighthouse.Backend.Platform stops seeing that type the moment it moves, and the rule then passes because it finds nothing to check.",
                @"add to the pattern an alternative for the area the type moves into, of the form ^Lighthouse\.Backend\.Platform\.<Area>(\.|$), keep every old alternative, and add no alternative that reaches outside Lighthouse.Backend.Platform. A pattern that names one type or file by its full name belongs on the membership map's list of strings the move renames instead.");
        }

        [Test]
        public void ThePreservationCheck_NamesAMadeUpPatternInANestedClassThatSeesOnlyTheOldPlace_AndPassesOnesThatSeeBothOrNeither()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var blind = WhenEachPatternIsAskedAboutEveryMovingTypeUnderBothNames(patterns);

            ThenThePatternsNamedAre(
                blind.Select(finding => finding.Field).Distinct().ToList(),
                named: [$"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.Nested)}.{nameof(MadeUpRuleKeys.Nested.SeesOnlyTheOldPlace)}"],
                notNamed: [$"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.SeesBothPlaces)}", $"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.SeesNeitherPlace)}", $"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.NotARegularExpression)}"],
                "The preservation check, run over made-up rule keys found by reflection",
                "the check must find every constant pattern on its own, nested classes included, and compare the answer under the new name with the answer under the old one; a check that reads a hand list, skips nested classes or looks only at the old answer would let a blind rule through.");
        }

        [Test]
        public void ThePreservationCheck_LeavesAFullNameTheMoveRenames_ToTheMoveThatRenamesIt()
        {
            var cryptoServiceFullName = (string)typeof(SecretCustodySeamArchUnitTest)
                .GetField("CryptoServiceFullName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
                .GetRawConstantValue()!;
            var cryptoService = PlatformLaneMembership.MovingTypes.Single(type => type.Name == "CryptoService");

            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    Regex.IsMatch(cryptoService.OldFullName, cryptoServiceFullName, RegexOptions.None, PatternTimeout) && !Regex.IsMatch(cryptoService.NewFullName, cryptoServiceFullName, RegexOptions.None, PatternTimeout),
                    Is.True,
                    $"The full name {cryptoServiceFullName} should answer yes under the old name and no under the new one; it is the example that shows a designation of one type is not a namespace rule. Why it matters: without it this test proves nothing about the skip. How to fix: pick another full name on the map's list of strings the move renames.");
                Assert.That(
                    patterns.Select(pattern => pattern.Field),
                    Has.None.EqualTo($"{nameof(SecretCustodySeamArchUnitTest)}.CryptoServiceFullName"),
                    "The preservation check took up SecretCustodySeamArchUnitTest.CryptoServiceFullName, a full name the membership map lists as renamed by the key-ring move. Why it matters: widening a full name would make it name two types. How to fix: leave out every constant whose value is on the map's list of strings the move renames for the file of its class.");
                Assert.That(
                    patterns.Select(pattern => pattern.Field),
                    Does.Contain($"{nameof(KernelRules)}.{nameof(KernelRules.TheUpdateQueue)}"),
                    "The preservation check did not take up KernelRules.TheUpdateQueue, a namespace rule of the update queue. Why it matters: a check that misses real rules guards nothing. How to fix: enumerate every constant string field of the test assembly that parses as a regular expression.");
            }
        }

        [Test]
        public void NoPlatformAlternativeOfARulePattern_MatchesAProductionTypeOutsidePlatform()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var reachingOutside = WhenThePlatformAlternativesAreAskedAboutEveryProductionTypeOutsidePlatform(patterns);

            Assert.That(
                reachingOutside,
                Is.Empty,
                "These rule patterns match a production type outside Lighthouse.Backend.Platform only through their Platform alternative:" + Environment.NewLine +
                string.Join(Environment.NewLine, reachingOutside) + Environment.NewLine +
                @"Why it matters: an alternative added for the move must see the moved types and nothing else; one that reaches outside Platform puts types under a rule that was never about them. How to fix: write the alternative as ^Lighthouse\.Backend\.Platform\.<Area>(\.|$), anchored and ending at a namespace boundary.");
        }

        [Test]
        public void ThePlatformAlternativeCheck_NamesAMadeUpAlternativeThatReachesOutsidePlatform_AndPassesAnAnchoredOne()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var reachingOutside = WhenThePlatformAlternativesAreAskedAboutEveryProductionTypeOutsidePlatform(patterns);

            ThenThePatternsNamedAre(
                reachingOutside.Select(finding => finding.Split(' ')[0]).Distinct().ToList(),
                named: [$"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.APlatformAlternativeThatReachesOutside)}"],
                notNamed: [$"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.SeesBothPlaces)}"],
                "The Platform-alternative check, run over made-up rule keys",
                "an unanchored Platform alternative matches production types such as the platform service interface that sit outside Lighthouse.Backend.Platform today; the check must name it and pass an alternative anchored on Lighthouse.Backend.Platform.<Area>.");
        }

        [Test]
        public void TheDispatcherSelection_SelectsTheDispatcherAndItsNestedTypesByType_AndNoTypeThatOnlySpellsItsOldNamespace()
        {
            var dispatcher = PlatformLaneMembership.MovingTypes.Single(type => type.Name == "DomainEventDispatcher");

            var inTheProduct = ApplicationCoreRules.HandlersUseNoServiceLocator(ProductionModule.Value, HandlerSelection.ByDomainEventsNamespace).Examined;
            var inTheTests = ApplicationCoreRules.HandlersUseNoServiceLocator(TestModule.Value, HandlerSelection.ByDomainEventsNamespace).Examined;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    inTheProduct.Select(IlDependencies.DeclaringTypeOf).Distinct(),
                    Is.EquivalentTo(new[] { dispatcher.OldFullName }).Or.EquivalentTo(new[] { dispatcher.NewFullName }),
                    $"The dispatcher selection examined [{string.Join(", ", inTheProduct)}] in the product. Why it matters: the handler rules check the dispatcher through this selection, and it must keep finding the dispatcher and its nested types wherever the dispatcher lives. How to fix: select the dispatcher by its type and the types nested in it.");
                Assert.That(
                    inTheTests,
                    Is.Empty,
                    $"The dispatcher selection examined [{string.Join(", ", inTheTests)}] in the test assembly, which holds no dispatcher, only types whose namespace spells the dispatcher's old namespace (such as {typeof(PlatformLaneRulesSeeTheMoveFixtures.Services.Implementation.DomainEvents.ATypeThatOnlySpellsTheOldDispatcherNamespace).FullName}). Why it matters: a selection by namespace fragment stops seeing the dispatcher once it moves into Lighthouse.Backend.Platform.DomainEvents, and the rules that read it then check nothing. How to fix: select the dispatcher by its exact type and the types nested in it, not by a piece of its namespace.");
            }
        }

        [Test]
        public void NoStringInTheRuleFiles_NamesAMovingAreaOrMovingTypeByItsOldPath_UnlessTheMoveRenamesIt()
        {
            var governedPatterns = GivenThePatternValuesThePreservationCheckGoverns();

            var found = RuleFiles()
                .SelectMany(file => WhenTheStringsOfTheFileAreRead(file.RelativePath, file.Source, governedPatterns))
                .ToList();

            Assert.That(
                found,
                Is.Empty,
                "These strings in the rule files name a moving platform area or type by its old Services path:" + Environment.NewLine +
                string.Join(Environment.NewLine, found) + Environment.NewLine +
                "Why it matters: a string that is not a constant escapes the preservation check, so a selection or message built on it keeps pointing at the old place after the move and finds nothing there. How to fix: select by the type itself (typeof) or word the message without the path; a full name or path that one move renames goes on the membership map's list of strings the move renames, with its new text.");
        }

        [Test]
        public void TheOldPathCheck_NamesMadeUpStringsThatNameTheOldPlace_AndPassesTheOnesThatAreNotAboutTheMove()
        {
            var governedPatterns = GivenThePatternValuesThePreservationCheckGoverns();
            const string aRuleFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpRuleFile.cs";
            const string aFileWithARename = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs";
            var renamedConnectionSecrets = "\"Lighthouse.Backend.Services.Implementation.Encryption.ConnectionSecrets\"";
            var source = string.Join(
                Environment.NewLine,
                "type => type.FullName.Contains(\".Services.Implementation.DomainEvents\", StringComparison.Ordinal),",
                "\"Use the update contracts in Services.Interfaces.Update instead.\"",
                "\"Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs\"",
                "@\"^Lighthouse\\.Backend\\.Services\\.Implementation\\.UsageData(\\.|$)\" + \"|x\"",
                renamedConnectionSecrets,
                "\"Services/Implementation/DomainEvents/DeliveryForecastPublishingHandlerTest.cs\"",
                "\"Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater\"",
                "\"Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.TeamDataRefreshedGoldTest\"",
                $"@\"{KernelRules.TheUpdateQueue}\"");

            var inARuleFile = WhenTheStringsOfTheFileAreRead(aRuleFile, source, governedPatterns);
            var inTheFileWithTheRename = WhenTheStringsOfTheFileAreRead(aFileWithARename, renamedConnectionSecrets, governedPatterns);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    inARuleFile.Select(finding => finding.Split(": ", 2)[0]),
                    Is.EqualTo(new[] { $"{aRuleFile}:1", $"{aRuleFile}:2", $"{aRuleFile}:3", $"{aRuleFile}:4", $"{aRuleFile}:5" }),
                    $"The old-path check named [{string.Join(" | ", inARuleFile)}]. Expected the inline dispatcher namespace fragment, the old update-contracts area in a message, the old path of a moving file, a pattern string that is not a constant, and a moving type's full name in a file the move does not rename it in (lines 1 to 5), and not a handler test that stays, an update pipeline that stays, a test class name, or a constant rule pattern the preservation check governs (lines 6 to 9). Why it matters: a check that names everything gets switched off, and one that names nothing guards nothing. How to fix: name a string when it names a moving area as a whole or a moving type, from Lighthouse.Backend or the start of the string, after taking out what the map renames for that file.");
                Assert.That(
                    inTheFileWithTheRename,
                    Is.Empty,
                    $"The old-path check named [{string.Join(" | ", inTheFileWithTheRename)}] in {aFileWithARename}, where the membership map lists that full name as renamed by the key-ring move. Why it matters: the move renames it in its own commit; naming it now would block a rename the map already plans. How to fix: take out the map's renamed strings for the file being read before looking for old paths.");
            }
        }

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = RulePatternsOf(typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => RulePatternsOf(typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(InFixtures));

        private static HashSet<string> GivenThePatternValuesThePreservationCheckGoverns()
            => [.. GivenTheRulePatternsOfTheTestAssembly()
                .Where(pattern => PlatformLaneMembership.MovingTypes.Any(type => IsMatch(type.OldFullName, pattern.Value) || IsMatch(type.NewFullName, pattern.Value)))
                .Select(pattern => pattern.Value)];

        private static List<BlindAnswer> WhenEachPatternIsAskedAboutEveryMovingTypeUnderBothNames(IEnumerable<RulePattern> patterns)
            => [.. from pattern in patterns
                from type in PlatformLaneMembership.MovingTypes
                let underTheOldName = IsMatch(type.OldFullName, pattern.Value)
                let underTheNewName = IsMatch(type.NewFullName, pattern.Value)
                where underTheOldName != underTheNewName
                select new BlindAnswer(pattern.Field, pattern.Value, type, underTheOldName, underTheNewName)];

        private static List<string> WhenThePlatformAlternativesAreAskedAboutEveryProductionTypeOutsidePlatform(IEnumerable<RulePattern> patterns)
        {
            var outsidePlatform = IlDependencies.TypesIn(ProductionModule.Value, @"^Lighthouse\.Backend(\.|$)")
                .Where(typeName => !IsMatch(typeName, ThePlatformNamespace))
                .ToList();

            return [.. from pattern in patterns
                let withoutPlatform = PlatformToken.Replace(pattern.Value, "(?!)")
                where withoutPlatform != pattern.Value
                from typeName in outsidePlatform
                where IsMatch(typeName, pattern.Value) && !IsMatch(typeName, withoutPlatform)
                select $"{pattern.Field} = {pattern.Value} matches {typeName}"];
        }

        private static List<string> WhenTheStringsOfTheFileAreRead(string relativePath, string source, HashSet<string> governedPatterns)
        {
            var renamedHere = PlatformLaneMembership.StringsTheMoveRenames
                .Where(entry => string.Equals(entry.File, relativePath, StringComparison.Ordinal))
                .Select(entry => entry.OldText)
                .OrderByDescending(oldText => oldText.Length)
                .ToList();
            var withoutComments = ProductionSource.WithoutComments(source);
            var found = new List<string>();

            foreach (Match literal in StringLiteral.Matches(withoutComments))
            {
                if (!literal.Groups["verbatim"].Success && !literal.Groups["regular"].Success)
                {
                    continue;
                }

                var value = literal.Groups["verbatim"].Success
                    ? literal.Groups["verbatim"].Value.Replace("\"\"", "\"", StringComparison.Ordinal)
                    : literal.Groups["regular"].Value.Replace("\\\\", "\\", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);

                if (governedPatterns.Contains(value))
                {
                    continue;
                }

                var remainder = renamedHere.Aggregate(value, (text, oldText) => text.Replace(oldText, " ", StringComparison.Ordinal));
                var normalised = remainder.Replace("\\", string.Empty, StringComparison.Ordinal).Replace('/', '.');

                if (OldPathReferences.Value.Any(reference => reference.IsMatch(normalised)))
                {
                    var line = withoutComments.AsSpan(0, literal.Index).Count('\n') + 1;
                    found.Add($"{relativePath}:{line}: {literal.Value}");
                }
            }

            return found;
        }

        private static void ThenNoPatternAnswersDifferently(List<BlindAnswer> blind, string why, string how)
        {
            var byPattern = blind.GroupBy(finding => finding.Field).ToList();
            var report = byPattern.Select(group =>
                $"{group.Key} = {group.First().Pattern}{Environment.NewLine}" +
                string.Join(Environment.NewLine, group.Take(3).Select(finding =>
                    $"    {finding.Type.OldFullName} -> {Answer(finding.UnderTheOldName)}, {finding.Type.NewFullName} -> {Answer(finding.UnderTheNewName)}")) +
                (group.Count() > 3 ? $"{Environment.NewLine}    and {group.Count() - 3} more moving types" : string.Empty));

            Assert.That(
                byPattern.Select(group => group.Key),
                Is.Empty,
                $"{byPattern.Count} rule patterns answer differently for a moving platform type under its old and its new namespace:{Environment.NewLine}" +
                string.Join(Environment.NewLine, report) + $"{Environment.NewLine}Why it matters: {why} How to fix: {how}");
        }

        private static void ThenThePatternsNamedAre(List<string> found, string[] named, string[] notNamed, string check, string why)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var pattern in named)
                {
                    Assert.That(found, Does.Contain(pattern), $"{check} did not name {pattern}. Named: [{string.Join(", ", found)}]. Why it matters: {why} How to fix: run the same check the real rules get over these made-up keys.");
                }

                foreach (var pattern in notNamed)
                {
                    Assert.That(found, Does.Not.Contain(pattern), $"{check} named {pattern}, which answers the same before and after the move or is not a pattern. Why it matters: a check that names compliant keys gets switched off. How to fix: name a key only when its two answers differ.");
                }
            }
        }

        private static List<RulePattern> RulePatternsOf(IEnumerable<Type> types)
        {
            var renamed = PlatformLaneMembership.StringsTheMoveRenames;

            return [.. from type in types
                from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                where field.IsLiteral && field.FieldType == typeof(string)
                let value = field.GetRawConstantValue() as string
                where value is not null && IsARulePattern(type, value)
                where !renamed.Any(entry => entry.OldText == value && FileStem(entry.File) == OutermostType(type).Name)
                select new RulePattern(FieldName(type, field), value)];
        }

        private static IEnumerable<Regex> OldPathReferencesOfTheMap()
        {
            const string fromTheBackendOrTheStart = @"(?<=^|^\.|[^A-Za-z0-9_.]|[^A-Za-z0-9_.]\.|Lighthouse\.Backend\.)";

            foreach (var area in MovingAreasOfServices.Select(Regex.Escape))
            {
                yield return new Regex($@"{fromTheBackendOrTheStart}Services\.(Implementation|Interfaces)\.{area}(?![A-Za-z0-9_]|\.[A-Za-z_])", RegexOptions.None, PatternTimeout);
            }

            foreach (var type in PlatformLaneMembership.MovingTypes)
            {
                var oldNamespace = type.OldNamespace["Lighthouse.Backend.".Length..];
                var name = type.Name.Split('`')[0];
                yield return new Regex($@"{fromTheBackendOrTheStart}{Regex.Escape(oldNamespace)}\.{Regex.Escape(name)}(?![A-Za-z0-9_])", RegexOptions.None, PatternTimeout);
            }
        }

        private static List<(string RelativePath, string Source)> RuleFiles()
        {
            var root = RepositoryRoot();
            var architecture = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests", "Architecture");
            string[] notRules = [$"{nameof(PlatformLaneMembership)}.cs", $"{nameof(PlatformLaneRulesSeeTheMoveArchUnitTest)}.cs"];

            var files = Directory.EnumerateFiles(architecture, "*.cs", SearchOption.AllDirectories)
                .Where(path => !notRules.Contains(Path.GetFileName(path), StringComparer.Ordinal))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(50), $"Only {files.Count} rule files were found under {architecture}, so the scan would pass without reading the rules.");
            return files;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the rule files cannot be read.");
            return directory!.FullName;
        }

        private static bool IsARulePattern(Type type, string value)
            => NamesTheBackend.IsMatch(value)
                && (value.StartsWith('^') || type.Namespace?.StartsWith(RuleNamespace, StringComparison.Ordinal) == true)
                && ParsesAsAPattern(value);

        private static bool ParsesAsAPattern(string value)
        {
            try
            {
                _ = new Regex(value, RegexOptions.None, PatternTimeout);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool IsMatch(string input, string pattern) => Regex.IsMatch(input, pattern, RegexOptions.None, PatternTimeout);

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true;

        private static Type OutermostType(Type type) => type.DeclaringType is null ? type : OutermostType(type.DeclaringType);

        private static string FileStem(string file) => Path.GetFileName(file).Split('.')[0];

        private static string FieldName(Type type, FieldInfo field)
            => $"{type.FullName![(type.Namespace!.Length + 1)..].Replace('+', '.')}.{field.Name}";

        private static string Answer(bool matches) => matches ? "matches" : "does not match";

        private sealed record RulePattern(string Field, string Value);

        private sealed record BlindAnswer(string Field, string Pattern, PlatformLaneType Type, bool UnderTheOldName, bool UnderTheNewName);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneRulesSeeTheMoveFixtures
{
    /// <summary>Made-up rule keys the checks above are run over, to show they name a blind key and pass the others.</summary>
    internal static class MadeUpRuleKeys
    {
        public const string SeesBothPlaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.DomainEvents|Platform\.DomainEvents)(\.|$)";

        public const string SeesNeitherPlace = @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";

        public const string NotARegularExpression = "(Services.Implementation.DomainEvents";

        public const string APlatformAlternativeThatReachesOutside = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DomainEvents(\.|$)|Platform";

        internal static class Nested
        {
            public const string SeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DomainEvents(\.|$)";
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneRulesSeeTheMoveFixtures.Services.Implementation.DomainEvents
{
    /// <summary>A type whose namespace only spells the dispatcher's old namespace; a selection of the dispatcher must not pick it.</summary>
    internal sealed class ATypeThatOnlySpellsTheOldDispatcherNamespace
    {
        public override string ToString() => nameof(ATypeThatOnlySpellsTheOldDispatcherNamespace);
    }
}
