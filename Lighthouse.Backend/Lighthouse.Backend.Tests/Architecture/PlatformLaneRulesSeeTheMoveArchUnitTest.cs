using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneRulesSeeTheMoveFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The platform types are about to move out of Services into Lighthouse.Backend.Platform. Every architecture rule that
    /// finds them by name must keep its meaning after the move, or the move quietly switches the rule off: a rule that
    /// looks only at the old place passes because it no longer finds anything there.
    /// <para>
    /// Which strings count as rule patterns: every constant or static read-only string field of this test assembly, in any
    /// class and any nested class, that parses as a regular expression, names Lighthouse.Backend, and is either anchored at
    /// its start or declared among the architecture rules. A constant that names one type or one file by its full name or
    /// path is on the membership map's list of strings the move renames and is left to the move that renames it.
    /// </para>
    /// <para>
    /// A pattern is asked on the subject its rule matches it against, the namespace of the outermost type or the full
    /// name, as declared in <see cref="PlatformLanePatternRoles"/>. A pattern that sees a moving type must be declared
    /// there with its role: a part of the code must see the type in its new place too, and only by taking in the whole
    /// area the type moves into; an old location must not see it there. A pattern that sees a moving type and is not
    /// declared fails until it is.
    /// </para>
    /// <para>
    /// Strings that are not constants escape reflection, so the rule files are also read as text. A string there may not
    /// name a moving area as a whole, or a moving type, by its old Services path, unless the membership map lists it as
    /// renamed by a move.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PlatformLaneRulesSeeTheMoveArchUnitTest
    {
        private static readonly string ThePlatformNamespace = @"^Lighthouse\.Backend\.Platform(\.|$)";

        private static readonly string FixtureNamespace = typeof(MadeUpRuleKeys).Namespace!;

        private static readonly Type[] TheCheckItself = [typeof(PlatformLaneRulesSeeTheMoveArchUnitTest), typeof(PlatformLaneMembership), typeof(PlatformLanePatternRoles), typeof(NamespaceMoveLaw)];

        private static readonly string[] MovingAreasOfServices =
            ["Encryption", "DatabaseManagement", "UsageData", "Logging", "DomainEvents", "BackgroundServices.Update", "Update"];

        private static readonly Regex PlatformToken = new(@"(?<=\\\.|\(|\|)Platform(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        private static readonly NamespaceMove ThePlatformMove = new(
            [.. PlatformLaneMembership.MovingTypes.Select(type => new MovingType(type.Name, type.Area, type.OldNamespace, type.NewNamespace))],
            [.. PlatformLaneMembership.StringsTheMoveRenames.Select(entry => new RenamedString(entry.File, entry.OldText, entry.NewText))],
            ThePlatformNamespace,
            PlatformToken,
            MovingAreasOfServices);

        private static readonly NamespaceMoveLaw TheLaw = new(ThePlatformMove, new PatternRoleMap(PlatformLanePatternRoles.PatternRoles, PlatformLanePatternRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpKeys = new(ThePlatformMove, new PatternRoleMap(MadeUpRuleRoles.Roles, MadeUpRuleRoles.FolderOnlyMatches));

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
        public void EveryRulePatternThatSeesAMovingPlatformType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var breaks = TheLaw.RoleBreaksOf(patterns);

            ThenNoPatternBreaksItsRole(breaks);
        }

        [Test]
        public void TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole_AndPassesTheOnesThatKeepIt()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var breaks = TheLawOverTheMadeUpKeys.RoleBreaksOf(patterns);

            ThenTheBreaksNamedAre(
                breaks,
                [
                    (nameof(MadeUpRuleKeys.AContextThatSeesOnlyTheOldPlace), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRuleKeys.AContextNamespaceWidenedByTypeName), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.SeesTheTypeInItsNewPlace),
                    (nameof(MadeUpRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation),
                    (nameof(MadeUpRuleKeys.APlaceGivenATypeNameInTheNewPlace), RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation),
                    (nameof(MadeUpRuleKeys.AContextWidenedIntoAnAreaItNeverSaw), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpRuleKeys.APlatformAlternativeThatReachesOutside), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpRuleKeys.AFolderOnlyMatchThatFollowsTheTypeIntoItsNewPlace), RoleBreak.FollowsATypeItSawOnlyByFolder),
                    ($"{nameof(MadeUpRuleKeys.Nested)}.{nameof(MadeUpRuleKeys.Nested.UndeclaredButSeesTheOldPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpRuleKeys.Nested)}.{nameof(MadeUpRuleKeys.Nested.UndeclaredButSeesOnlyTheNewPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpRuleKeys.Nested)}.{nameof(MadeUpRuleKeys.Nested.ACompositeOfAContextThatSeesOnlyTheOldPlace)}", RoleBreak.StopsSeeingTheType),
                ],
                "a check that asks a namespace rule about full names, lets an undeclared pattern pass, accepts an old location that sees the new place, admits a part of the code into an area it never saw, ignores a composite's component, lets a part of the code follow a type it saw only by its folder, or skips static read-only fields and nested classes lets a rule go blind or wrong at the move, and passes the real rules it should name.");
        }

        [Test]
        public void ThePreservationCheck_LeavesAFullNameTheMoveRenames_ToTheMoveThatRenamesIt()
        {
            var cryptoService = PlatformLaneMembership.MovingTypes.Single(type => type.Name == "CryptoService");
            var renamedRow = PlatformLaneMembership.StringsTheMoveRenames
                .Single(entry => FileStem(entry.File) == nameof(SecretCustodySeamArchUnitTest) && entry.OldText == cryptoService.OldFullName);
            var liveConstant = (string)typeof(SecretCustodySeamArchUnitTest)
                .GetField("CryptoServiceFullName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
                .GetRawConstantValue()!;

            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    IsMatch(cryptoService.OldFullName, renamedRow.OldText) && !IsMatch(cryptoService.NewFullName, renamedRow.OldText),
                    Is.True,
                    $"The full name {renamedRow.OldText} should answer yes under the old name and no under the new one; it is the example that shows a designation of one type is not a namespace rule. Why it matters: without it this test proves nothing about the skip. How to fix: pick another full name on the map's list of strings the move renames.");
                Assert.That(
                    liveConstant,
                    Is.EqualTo(renamedRow.OldText).Or.EqualTo(renamedRow.NewText),
                    $"SecretCustodySeamArchUnitTest.CryptoServiceFullName reads {liveConstant}, which is neither the old text {renamedRow.OldText} nor the new text {renamedRow.NewText} of its row on the map's list of strings the move renames. Why it matters: the skip is only shown for a constant that holds one of the two texts of its row. How to fix: give the constant the old full name before the key-ring move and the new one after it.");
                Assert.That(
                    patterns.Select(pattern => pattern.Field),
                    Has.None.EqualTo($"{nameof(SecretCustodySeamArchUnitTest)}.CryptoServiceFullName"),
                    "The preservation check took up SecretCustodySeamArchUnitTest.CryptoServiceFullName, a full name the membership map lists as renamed by the key-ring move. Why it matters: widening a full name would make it name two types. How to fix: leave out every constant whose value is on the map's list of strings the move renames for the file of its class.");
                Assert.That(
                    patterns.Select(pattern => pattern.Field),
                    Does.Contain($"{nameof(KernelRules)}.{nameof(KernelRules.TheUpdateQueue)}"),
                    "The preservation check did not take up KernelRules.TheUpdateQueue, a namespace rule of the update queue. Why it matters: a check that misses real rules guards nothing. How to fix: enumerate every constant and static read-only string field of the test assembly that parses as a regular expression.");
            }
        }

        [Test]
        public void NoPlatformAlternativeOfARulePattern_MatchesAProductionNamespaceOrTypeOutsidePlatform()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var outside = GivenTheProductionNamesOutsidePlatform();

            var reachingOutside = TheLaw.AlternativesReachingOutside(patterns, outside);

            Assert.That(
                reachingOutside,
                Is.Empty,
                "These rule patterns match a production namespace or type outside Lighthouse.Backend.Platform only through their Platform alternative:" + Environment.NewLine +
                string.Join(Environment.NewLine, reachingOutside) + Environment.NewLine +
                @"Why it matters: an alternative added for the move must see the moved types and nothing else; one that reaches outside Platform puts types under a rule that was never about them. How to fix: write the alternative as ^Lighthouse\.Backend\.Platform\.<Area>(\.|$), anchored and ending at a namespace boundary.");
        }

        [Test]
        public void ThePlatformAlternativeCheck_NamesAMadeUpAlternativeThatReachesOutsidePlatform_AndPassesAnAnchoredOne()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var production = GivenTheProductionNamesOutsidePlatform();
            var madeUpNamespace = "Lighthouse.Backend.Services.PlatformLookalike";
            var outside = new OutsideNames(
                [.. production.Namespaces, madeUpNamespace],
                [.. production.FullNames, madeUpNamespace + ".AMadeUpType"]);

            var reachingOutside = TheLawOverTheMadeUpKeys.AlternativesReachingOutside(patterns, outside);

            var named = reachingOutside.Select(finding => finding.Split(' ')[0]).Distinct().ToList();
            Assert.That(
                named,
                Is.EquivalentTo(new[] { $"{nameof(MadeUpRuleKeys)}.{nameof(MadeUpRuleKeys.APlatformAlternativeThatReachesOutside)}" }),
                $"The Platform-alternative check, asked about the production names plus a made-up outside name (Lighthouse.Backend.Services.PlatformLookalike), named [{string.Join(", ", reachingOutside)}]. Expected only the made-up key whose unanchored Platform alternative matches the made-up outside name, and none of the keys anchored on Lighthouse.Backend.Platform.<Area>. Why it matters: a check that names compliant keys gets switched off, and one that misses a too-wide alternative lets a rule take in types it was never about. How to fix: ask each Platform alternative about the production namespaces or full names outside Platform, on the subject the pattern is declared with.");
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
                .SelectMany(file => TheLaw.OldPathStringsIn(file.RelativePath, file.Source, governedPatterns))
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

            var inARuleFile = TheLaw.OldPathStringsIn(aRuleFile, source, governedPatterns);
            var inTheFileWithTheRename = TheLaw.OldPathStringsIn(aFileWithARename, renamedConnectionSecrets, governedPatterns);

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
            var patterns = TheLaw.RulePatternsOf(typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => TheLaw.RulePatternsOf(typeof(PlatformLaneRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(InFixtures));

        private static HashSet<string> GivenThePatternValuesThePreservationCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly());

        private static OutsideNames GivenTheProductionNamesOutsidePlatform()
            => TheLaw.NamesOutsideTheNewPlace(ProductionModule.Value);

        private static void ThenNoPatternBreaksItsRole(List<PatternBreak> breaks)
        {
            var byPattern = breaks.GroupBy(found => found.Pattern.Field).ToList();
            var report = byPattern.Select(group =>
                $"{group.Key} = {group.First().Pattern.Value} ({group.First().Declared}){Environment.NewLine}" +
                string.Join(Environment.NewLine, group.Take(3).Select(found => found.Describe())) +
                (group.Count() > 3 ? $"{Environment.NewLine}    and {group.Count() - 3} more moving types" : string.Empty));

            Assert.That(
                byPattern.Select(group => group.Key),
                Is.Empty,
                $"{byPattern.Count} rule patterns would not keep their meaning when the platform types move:{Environment.NewLine}" +
                string.Join(Environment.NewLine, report) + Environment.NewLine +
                "Why it matters: a rule matches a pattern against the namespace of a type or its full name; a part of the code that does not match the new namespace stops seeing the type the moment it moves and then passes because it finds nothing, and an old location that matches the new place starts judging code that no longer lives there. " +
                @"How to fix: declare an undeclared pattern in PlatformLanePatternRoles with its subject, role and reason; give a part of the code one alternative per area it saw, ^Lighthouse\.Backend\.Platform\.<Area>(\.|$), and no alternative that names a type; take every Platform alternative off an old location; keep every old alternative; a pattern listed in PlatformLanePatternRoles.FolderOnlyMatches saw that type only by its folder, so take the type's new place out of that pattern and every pattern built from it, and widen it by no area for that type.");
        }

        private static void ThenTheBreaksNamedAre(List<PatternBreak> breaks, (string Field, RoleBreak Break)[] expected, string why)
        {
            var named = breaks
                .Select(found => (Field: found.Pattern.Field[(nameof(MadeUpRuleKeys).Length + 1)..], found.Break))
                .Distinct()
                .ToList();

            Assert.That(
                named,
                Is.EquivalentTo(expected),
                $"The role check, run over made-up rule keys found by reflection, named [{string.Join(", ", named)}] and should have named exactly [{string.Join(", ", expected)}]." + Environment.NewLine +
                string.Join(Environment.NewLine, breaks.Take(12).Select(found => found.Describe())) + Environment.NewLine +
                $"Why it matters: {why} How to fix: run the same check the real rules get over these made-up keys, on the subject and role each is declared with.");
        }

        private static List<(string RelativePath, string Source)> RuleFiles()
        {
            var root = RepositoryRoot();
            var architecture = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests", "Architecture");
            string[] notRules = [$"{nameof(PlatformLaneMembership)}.cs", $"{nameof(PlatformLanePatternRoles)}.cs", $"{nameof(PlatformLaneRulesSeeTheMoveArchUnitTest)}.cs", $"{nameof(NamespaceMoveLaw)}.cs"];

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

        private static bool IsMatch(string input, string pattern) => NamespaceMoveLaw.IsMatch(input, pattern);

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true;

        private static string FileStem(string file) => NamespaceMoveLaw.FileStem(file);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneRulesSeeTheMoveFixtures
{
    /// <summary>Made-up rule keys the checks above are run over, to show they name a key that breaks its role and pass the others.</summary>
    internal static class MadeUpRuleKeys
    {
        public const string SeesBothPlaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.DomainEvents|Platform\.DomainEvents)(\.|$)";

        public const string SeesNeitherPlace = @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";

        public const string NotARegularExpression = "(Services.Implementation.DomainEvents";

        public const string AContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DomainEvents(\.|$)";

        public const string AContextNamespaceWidenedByTypeName = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.DomainEvents(\.|$)|Platform\.DomainEvents\.[^.]+$)";

        public const string AFullNameWidenedByTypeName = AContextNamespaceWidenedByTypeName;

        public const string APlaceWidenedIntoTheNewPlace = SeesBothPlaces;

        public const string APlaceGivenATypeNameInTheNewPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.DomainEvents(\.|$)|^Lighthouse\.Backend\.Platform\.DomainEvents\.DomainEventDispatcher$";

        public const string AContextWidenedByTheWholeArea = @"^Lighthouse\.Backend\.(Services\.Implementation\.Encryption|Platform\.Encryption)(\.|$)";

        public const string AContextWidenedIntoAnAreaItNeverSaw = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.DomainEvents|Platform\.(DomainEvents|Logging))(\.|$)";

        public const string APlatformAlternativeThatReachesOutside = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DomainEvents(\.|$)|Platform";

        public const string AFolderOnlyMatchThatFollowsTheTypeIntoItsNewPlace = @"^Lighthouse\.Backend\.(Services\.Implementation\.BackgroundServices|Platform\.Encryption)\.KeyRingFileWatcher$";

        public const string AFolderOnlyMatchLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.BackgroundServices\.KeyRingFileWatcher$";

        internal static class Nested
        {
            public const string UndeclaredButSeesTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.Logging(\.|$)";

            public static readonly string UndeclaredButSeesOnlyTheNewPlace = @"^Lighthouse\.Backend\.Platform\.Logging(\.|$)";

            public static readonly string ACompositeOfAContextThatSeesOnlyTheOldPlace = AContextThatSeesOnlyTheOldPlace + "|" + SeesNeitherPlace;

            public static readonly string ACompositeOfAContextThatSeesBothPlaces = SeesBothPlaces + "|" + SeesNeitherPlace;

            public static readonly string ADesignationReadFromTheType =
                typeof(Lighthouse.Backend.Data.LighthouseAppContext).Assembly.GetTypes().Single(type => type.Name == "DomainEventDispatcher").FullName!;
        }
    }

    /// <summary>The subject and role of each made-up key, as the real rules have theirs in PlatformLanePatternRoles.</summary>
    internal static class MadeUpRuleRoles
    {
        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            ["MadeUpRuleKeys.SeesBothPlaces"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that already names the new place"),
            ["MadeUpRuleKeys.AContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that was never widened"),
            ["MadeUpRuleKeys.AContextNamespaceWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.Namespace, "a namespace rule widened by type names, which no namespace matches"),
            ["MadeUpRuleKeys.AFullNameWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.FullName, "the same text read on full names, where the type names do match"),
            ["MadeUpRuleKeys.APlaceWidenedIntoTheNewPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location that was given the new place"),
            ["MadeUpRuleKeys.APlaceGivenATypeNameInTheNewPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location given a type-name alternative in the new place, which no namespace matches but which still names the new place"),
            ["MadeUpRuleKeys.AContextWidenedByTheWholeArea"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code widened by its whole area, which takes in the area's interfaces it did not see before"),
            ["MadeUpRuleKeys.AContextWidenedIntoAnAreaItNeverSaw"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code widened into the logging area as well"),
            ["MadeUpRuleKeys.APlatformAlternativeThatReachesOutside"] = PatternDeclaration.Context(PatternSubject.FullName, "a full-name rule with an unanchored Platform alternative"),
            ["MadeUpRuleKeys.AFolderOnlyMatchThatFollowsTheTypeIntoItsNewPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code that saw the key-ring watcher only by its folder and was widened into the watcher's new place"),
            ["MadeUpRuleKeys.AFolderOnlyMatchLeftInTheOldPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code that saw the key-ring watcher only by its folder and stays out of the watcher's new place"),
            ["MadeUpRuleKeys.Nested.ACompositeOfAContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Composite("MadeUpRuleKeys.AContextThatSeesOnlyTheOldPlace", "built from the blind part of the code"),
            ["MadeUpRuleKeys.Nested.ACompositeOfAContextThatSeesBothPlaces"] = PatternDeclaration.Composite("MadeUpRuleKeys.SeesBothPlaces", "built from the part of the code that names both places"),
        };

        /// <summary>The made-up folder-only matches, as the real rules have theirs in PlatformLanePatternRoles.</summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } = new Dictionary<(string Field, string Type), string>
        {
            [("MadeUpRuleKeys.AFolderOnlyMatchThatFollowsTheTypeIntoItsNewPlace", "KeyRingFileWatcher")] = "saw the watcher only by its folder",
            [("MadeUpRuleKeys.AFolderOnlyMatchLeftInTheOldPlace", "KeyRingFileWatcher")] = "saw the watcher only by its folder",
        };
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
