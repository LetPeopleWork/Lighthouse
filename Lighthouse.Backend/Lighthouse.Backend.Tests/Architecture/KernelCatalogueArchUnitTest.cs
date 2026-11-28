using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures;
using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mono.Cecil;
using Moq;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The shared kernel may hold only shared ids, truly shared value objects and the domain-event base type. Every other
    /// type in the Models belongs to a context or the platform lane, and stays in the Models only while something measured
    /// from the compiled code or the database model keeps it there. The catalogue lists every Models type once, each owned
    /// row's blockers are true, every free type is on the movers list with the namespace it moves to, and every rule that
    /// finds a free type by name keeps its meaning when the type moves.
    /// </summary>
    [TestFixture]
    public class KernelCatalogueArchUnitTest
    {
        private static readonly Type[] TheCheckItself =
        [
            typeof(KernelCatalogueArchUnitTest),
            typeof(KernelCatalogue),
            typeof(ModelsPatternRoles),
            typeof(ModelsStringsTheMoveRenames),
            typeof(NamespaceMoveLaw),
        ];

        // These files name the free types by their Models names on purpose: they are the measurement, not rules a move
        // could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(KernelCatalogue)}.cs",
            $"{nameof(ModelsPatternRoles)}.cs",
            $"{nameof(ModelsStringsTheMoveRenames)}.cs",
            $"{nameof(KernelCatalogueArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(KernelCatalogueArchUnitTest).Assembly));

        private static readonly Lazy<Dictionary<string, MeasuredType>> TheRealMeasurement = new(() => ModelsMeasurement.Measure(TheRealWorld()));

        private static readonly Lazy<Dictionary<string, MeasuredType>> TheMadeUpMeasurement = new(() => ModelsMeasurement.Measure(MadeUpWorld.Of(TestModule.Value)));

        private static readonly NamespaceMoveLaw TheLaw = new(TheModelsMove(), new PatternRoleMap(ModelsPatternRoles.PatternRoles, ModelsPatternRoles.FolderOnlyMatches));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryModelsType_HasExactlyOneCatalogueRow_AndEveryRowNamesAModelsType()
        {
            var measured = GivenTheMeasuredModelsTypes();

            ThenTheCatalogueListsExactlyTheMeasuredTypes(KernelCatalogue.Rows, measured);
        }

        [Test]
        public void TheCatalogueCheck_NamesAPlantedTypeMissingFromTheCatalogue_AndARowForATypeThatDoesNotExist()
        {
            var measured = TheMadeUpMeasurement.Value;

            var failure = WhenTheCatalogueIsComparedExpectingAFailure(MadeUpWorld.ACatalogueMissingThePlantedTypeWithARowForNoType, measured);

            ThenTheFailureNames(failure, nameof(APlantedRecord), MadeUpWorld.ARowForNoType);
        }

        [Test]
        public void TheKernelRows_AreExactlyTheTypesTheSharedKernelMayHold()
        {
            var rows = KernelCatalogue.Rows;

            ThenTheKernelRowsAreExactly(rows, KernelCatalogue.TheKernelTypes);
        }

        [Test]
        public void TheKernelCheck_NamesARowThatClaimsTheKernelForATypeTheKernelMayNotHold()
        {
            var rows = MadeUpWorld.CatalogueClaimingTheKernelFor(nameof(ANotStoredRecord));

            var failure = WhenTheKernelRowsAreComparedExpectingAFailure(rows, MadeUpWorld.TheKernelTypes);

            ThenTheFailureNames(failure, nameof(ANotStoredRecord));
        }

        [Test]
        public void EveryOwnedRow_ClaimsExactlyTheBlockersMeasuredFromTheCompiledCodeAndTheDatabaseModel()
        {
            var measured = GivenTheMeasuredModelsTypes();

            var problems = WhenTheClaimsAreCompared(KernelCatalogue.Rows, measured);

            ThenNoClaimIsFalse(problems);
        }

        [TestCase(nameof(ANotStoredRecord), nameof(KernelBlocker.Entity))]
        [TestCase(nameof(ANotSentRecord), nameof(KernelBlocker.Wire))]
        [TestCase(nameof(APlantedRecord), nameof(KernelBlocker.ReachedFromOutsideItsModule))]
        [TestCase(nameof(AConstantsNobodyNames), nameof(KernelBlocker.HeldBy))]
        public void TheBlockerCheck_NamesARowClaimingABlockerTheTypeIsFreeOf(string type, string blocker)
        {
            var claimed = Enum.Parse<KernelBlocker>(blocker);
            var measured = TheMadeUpMeasurement.Value;

            var problems = WhenTheClaimsAreCompared(MadeUpWorld.CatalogueClaiming(type, claimed), measured);

            ThenTheProblemNamesTheTypeAndTheBlocker(problems, type, claimed);
        }

        [TestCase(nameof(AStoredRecord), nameof(KernelBlocker.Entity))]
        [TestCase(nameof(ASentAnswer), nameof(KernelBlocker.Wire))]
        [TestCase(nameof(AReachedRecord), nameof(KernelBlocker.ReachedFromOutsideItsModule))]
        [TestCase(nameof(AConstantsOnly), nameof(KernelBlocker.HeldBy))]
        public void TheBlockerCheck_AcceptsARowWhoseClaimIsTrue(string type, string blocker)
        {
            var claimed = Enum.Parse<KernelBlocker>(blocker);
            var measured = TheMadeUpMeasurement.Value;

            var problems = WhenTheClaimsAreCompared(MadeUpWorld.CatalogueClaiming(type, claimed), measured);

            ThenNoClaimIsFalse(problems);
        }

        [Test]
        public void EveryFreeType_IsOnTheMoversList_WithItsOwnerAndTheNamespaceItMovesTo()
        {
            var measured = GivenTheMeasuredModelsTypes();

            ThenTheMoversListIsExactlyTheFreeTypes(KernelCatalogue.Movers, measured);
        }

        [Test]
        public void TheMoversCheck_NamesAFreeTypeLeftOffTheList_WithTheNamespaceItMovesTo()
        {
            var measured = TheMadeUpMeasurement.Value;

            var failure = WhenTheMoversAreComparedExpectingAFailure([], measured);

            ThenTheFailureNames(failure, nameof(APlantedRecord), MadeUpWorld.ThePlantedRecordsDestination);
        }

        [TestCaseSource(nameof(TheMovers))]
        public void TheFreeType_IsFoundAsATopLevelTypeInExactlyItsModelsOrItsNewNamespace(string oldFullName, string newFullName)
        {
            var places = WhenTheMoverIsLookedUpInTheCompiledCode(ProductionModule.Value, oldFullName, newFullName);

            ThenItIsFoundInExactlyOnePlace(oldFullName, newFullName, places);
        }

        [TestCaseSource(nameof(TheDeclaredPatterns))]
        public void TheRulePattern_KeepsItsDeclaredRole_WhenTheFreeModelsTypesMove(string field)
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var breaks = WhenTheRoleOfThePatternIsChecked(TheLaw, patterns, field);

            ThenThePatternKeepsItsRole(field, breaks);
        }

        [TestCaseSource(nameof(TheOwnerPatterns))]
        public void TheRulePatternOfAnOwner_SeesEveryFreeTypeOfThatOwnerUnderItsNewName(string field)
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var unseen = WhenTheOwnersFreeTypesAreLookedUpInThePattern(patterns, field, ModelsPatternRoles.SeeTheFreeTypesOfTheseOwnersInTheirNewPlace[field]);

            ThenThePatternSeesThemAll(field, unseen);
        }

        [Test]
        public void EveryRulePatternThatSeesAFreeModelsType_IsDeclared_AndEveryDeclaredPatternIsARulePattern()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var undeclared = WhenTheUndeclaredAndTheStaleDeclarationsAreFound(TheLaw, patterns);

            ThenEveryPatternIsDeclared(undeclared);
        }

        [Test]
        public void TheRoleCheck_NamesAnUndeclaredPatternThatSeesAFreeType()
        {
            var patterns = MadeUpWorld.TheLaw.RulePatternsOf([typeof(MadeUpRules)]);

            var breaks = MadeUpWorld.TheLaw.RoleBreaksOf(patterns);

            ThenTheUndeclaredFieldIsNamed(breaks, $"{nameof(MadeUpRules)}.{nameof(MadeUpRules.TheAlphaModule)}");
        }

        [Test]
        public void EveryTestStringThatNamesAFreeTypeByItsModelsName_IsOnTheRenameList_AndEveryListedStringIsFound()
        {
            var sources = GivenTheSourcesOfTheTestProject();

            var comparison = WhenTheOldNameStringsAreComparedWithTheList(sources);

            ThenTheOldNameStringsAreExactlyTheList(comparison);
        }

        private static IEnumerable<TestCaseData> TheMovers()
            => KernelCatalogue.Movers.Select(mover => new TestCaseData(mover.OldFullName, mover.NewFullName).SetArgDisplayNames(mover.Name));

        private static IEnumerable<TestCaseData> TheDeclaredPatterns()
            => ModelsPatternRoles.PatternRoles.Keys.Order(StringComparer.Ordinal).Select(field => new TestCaseData(field).SetArgDisplayNames(field));

        private static IEnumerable<TestCaseData> TheOwnerPatterns()
            => ModelsPatternRoles.SeeTheFreeTypesOfTheseOwnersInTheirNewPlace.Keys.Order(StringComparer.Ordinal).Select(field => new TestCaseData(field).SetArgDisplayNames(field));

        private static NamespaceMove TheModelsMove()
        {
            var destinations = KernelCatalogue.Movers.Select(mover => Regex.Escape(mover.NewNamespace)).Distinct(StringComparer.Ordinal).ToList();
            var newPlace = destinations.Count == 0 ? "(?!)" : $"^({string.Join('|', destinations)})(\\.|$)";

            return new NamespaceMove([.. KernelCatalogue.Movers], ModelsStringsTheMoveRenames.Entries, newPlace, new Regex("(?!)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout), []);
        }

        private static ModelsWorld TheRealWorld()
        {
            var root = RepositoryRoot();
            var backend = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend");

            return new ModelsWorld(
                ProductionModule.Value,
                KernelCatalogue.TheModelsRoot,
                "Lighthouse.Backend.Modules",
                "Lighthouse.Backend.Platform",
                @"^Lighthouse\.Backend\.API(\.|$)",
                @"^Lighthouse\.Backend\.API\.DTO(\.|$)",
                @"^Lighthouse\.Backend$|^Lighthouse\.Backend\.(Data|Startup|Services\.Implementation\.Repositories)(\.|$)",
                EntitiesOfTheDatabaseModel(),
                KernelCatalogue.TheKernelTypes,
                ModelsMeasurement.FilesOfTheTypesIn(Path.Combine(backend, "Models"), KernelCatalogue.TheModelsRoot),
                PathConsumersIn(root),
                KernelCatalogue.OwnersByDesign,
                ModelsMeasurement.SourcesOfTheModels(Path.Combine(backend, "Models")));
        }

        private static HashSet<string> EntitiesOfTheDatabaseModel()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite("DataSource=:memory:").Options;
            using var context = new LighthouseAppContext(options, Mock.Of<ICryptoService>(), NullLogger<LighthouseAppContext>.Instance);

            return [.. context.Model.GetEntityTypes().Select(entity => ModelsMeasurement.OutermostName(entity.ClrType))];
        }

        // The files outside the backend tests that could read a Models file by its path: scripts, workflows and the
        // frontend.
        private static List<PathConsumer> PathConsumersIn(string root)
        {
            string[] places = ["Scripts", ".github", Path.Combine("Lighthouse.Frontend", "src")];
            string[] kinds = [".sh", ".ps1", ".py", ".yml", ".yaml", ".ts", ".tsx", ".js", ".mjs"];

            return [.. places
                .Select(place => Path.Combine(root, place))
                .Where(Directory.Exists)
                .SelectMany(place => Directory.EnumerateFiles(place, "*", SearchOption.AllDirectories))
                .Where(path => kinds.Contains(Path.GetExtension(path), StringComparer.Ordinal) && !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(path => new PathConsumer(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))];
        }

        private static Dictionary<string, MeasuredType> GivenTheMeasuredModelsTypes()
        {
            var measured = TheRealMeasurement.Value;
            Assert.That(measured, Has.Count.GreaterThan(100), $"Only {measured.Count} top-level types were found in the Models namespaces of the compiled backend, so the catalogue would be compared with almost nothing. How to fix: read the types of the production assembly, not a test double.");
            return measured;
        }

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(KernelCatalogueArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every rule class.");
            return patterns;
        }

        private static List<(string RelativePath, string Source)> GivenTheSourcesOfTheTestProject()
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Where(path => !TheMeasurementFiles.Contains(Path.GetFileName(path), StringComparer.Ordinal))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the scan would pass without reading the test project.");
            return files;
        }

        private static string WhenTheCatalogueIsComparedExpectingAFailure(IReadOnlyList<CatalogueRow> rows, Dictionary<string, MeasuredType> measured)
            => Assert.Throws<AssertionException>(() => ThenTheCatalogueListsExactlyTheMeasuredTypes(rows, measured)).Message;

        private static string WhenTheKernelRowsAreComparedExpectingAFailure(IReadOnlyList<CatalogueRow> rows, IReadOnlyList<string> kernelTypes)
            => Assert.Throws<AssertionException>(() => ThenTheKernelRowsAreExactly(rows, kernelTypes)).Message;

        private static string WhenTheMoversAreComparedExpectingAFailure(IReadOnlyList<MovingType> movers, Dictionary<string, MeasuredType> measured)
            => Assert.Throws<AssertionException>(() => ThenTheMoversListIsExactlyTheFreeTypes(movers, measured)).Message;

        private static List<string> WhenTheClaimsAreCompared(IReadOnlyList<CatalogueRow> rows, Dictionary<string, MeasuredType> measured)
        {
            var problems = new List<string>();

            foreach (var row in rows.Where(row => row.Standing == KernelStanding.Owned && measured.ContainsKey(row.Type)))
            {
                var found = measured[row.Type];
                var claimed = row.Blockers.ToHashSet();

                problems.AddRange(claimed.Where(blocker => !found.Blockers.Contains(blocker)).Select(blocker =>
                    $"{row.Type} claims {blocker}, but the type is free of {blocker}: {ModelsMeasurement.WhatFreeOfMeans(blocker)} " +
                    $"Give the row its true blockers ({Describe(found)}), or, if it has none, move it to its owner."));
                problems.AddRange(found.Blockers.Where(blocker => !claimed.Contains(blocker)).Select(blocker =>
                    $"{row.Type} does not claim {blocker}, but the compiled code shows it ({Describe(found)}). Add the blocker to the row."));

                if (claimed.Count == 0 && found.Owner is { } owner && owner != row.Owner)
                {
                    problems.Add($"{row.Type} is free and owned by {row.Owner}, but the compiled code shows only {owner} uses it. Name {owner} as its owner.");
                }

                if (claimed.Count > 0 && string.IsNullOrWhiteSpace(row.FreedBy))
                {
                    problems.Add($"{row.Type} names no Story that frees it. Name the Story that removes its blocker.");
                }
            }

            return problems;
        }

        private static List<string> WhenTheMoverIsLookedUpInTheCompiledCode(ModuleDefinition module, string oldFullName, string newFullName)
            => [.. new[] { oldFullName, newFullName }.Where(fullName => module.GetType(fullName) is { DeclaringType: null })];

        private static List<string> WhenTheRoleOfThePatternIsChecked(NamespaceMoveLaw law, List<RulePattern> patterns, string field)
        {
            var pattern = patterns.FirstOrDefault(candidate => candidate.Field == field);
            return pattern is null
                ? [$"{field} is declared, but is no rule pattern of the test assembly"]
                : [.. law.RoleBreaksOf([pattern]).Select(found => found.Describe().Trim()).Distinct(StringComparer.Ordinal)];
        }

        private static List<string> WhenTheOwnersFreeTypesAreLookedUpInThePattern(List<RulePattern> patterns, string field, IReadOnlyList<string> owners)
        {
            var pattern = patterns.FirstOrDefault(candidate => candidate.Field == field);
            if (pattern is null)
            {
                return [$"{field} is declared, but is no rule pattern of the test assembly"];
            }

            return [.. KernelCatalogue.Movers
                .Where(mover => owners.Contains(mover.Area, StringComparer.Ordinal))
                .Where(mover => !NamespaceMoveLaw.IsMatch(mover.NewNamespace, pattern.Value))
                .Select(mover => $"{mover.Name} ({mover.NewNamespace})")];
        }

        private static List<string> WhenTheUndeclaredAndTheStaleDeclarationsAreFound(NamespaceMoveLaw law, List<RulePattern> patterns)
        {
            var seenOnlyInTheNewPlace = ModelsPatternRoles.SeeTheFreeTypesOfTheseOwnersInTheirNewPlace;
            var undeclared = law.RoleBreaksOf(patterns)
                .Where(found => found.Break == RoleBreak.NotDeclared && !seenOnlyInTheNewPlace.ContainsKey(found.Pattern.Field))
                .Select(found => $"{found.Pattern.Field} = {found.Pattern.Value}{Environment.NewLine}{found.Describe()}")
                .Distinct(StringComparer.Ordinal);
            var fields = patterns.Select(pattern => pattern.Field).ToHashSet(StringComparer.Ordinal);
            var stale = ModelsPatternRoles.PatternRoles.Keys.Concat(seenOnlyInTheNewPlace.Keys)
                .Where(field => !fields.Contains(field))
                .Select(field => $"{field}: declared, but no rule pattern of the test assembly");

            return [.. undeclared, .. stale];
        }

        private static OldNameComparison WhenTheOldNameStringsAreComparedWithTheList(List<(string RelativePath, string Source)> sources)
        {
            var governed = TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => ModelsPatternRoles.PatternRoles.ContainsKey(pattern.Field)));
            var unlisted = sources.SelectMany(file => TheLaw.OldPathStringsIn(file.RelativePath, file.Source, governed)).ToList();
            var bySource = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var notFound = ModelsStringsTheMoveRenames.Entries
                .Where(entry => !bySource.TryGetValue(entry.File, out var source) || !source.Contains(entry.OldText, StringComparison.Ordinal))
                .Select(entry => $"{entry.File}: {entry.OldText}")
                .ToList();

            return new OldNameComparison(unlisted, notFound);
        }

        private static void ThenTheCatalogueListsExactlyTheMeasuredTypes(IReadOnlyList<CatalogueRow> rows, Dictionary<string, MeasuredType> measured)
        {
            var twice = rows.GroupBy(row => row.Type, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            Assert.That(twice, Is.Empty, $"The catalogue has more than one row for {string.Join(", ", twice)}. Each Models type has exactly one standing: keep one row.");

            var listed = rows.Select(row => row.Type).ToHashSet(StringComparer.Ordinal);
            var missingRows = measured.Values.Where(type => !listed.Contains(type.Name)).OrderBy(type => type.Name, StringComparer.Ordinal).Select(SourceOfTheRow);

            InlineBaseline.AssertMatches(
                measured.Keys,
                listed,
                "The shared kernel holds only shared ids, truly shared value objects and the domain-event base type, so every " +
                "type in the Models needs a row that names its owner and what keeps it there. A type that is new in the Models " +
                "belongs with its owner instead: put it in Modules/<Owner> or Platform/<Area>. If it must stay, add its row as " +
                $"measured:{Environment.NewLine}{string.Join(Environment.NewLine, missingRows)}");
        }

        private static void ThenTheKernelRowsAreExactly(IReadOnlyList<CatalogueRow> rows, IReadOnlyList<string> kernelTypes)
            => InlineBaseline.AssertMatches(
                rows.Where(row => row.Standing == KernelStanding.Kernel).Select(row => row.Type),
                kernelTypes,
                "The shared kernel may hold only shared ids, truly shared value objects and the domain-event base type, and a " +
                "Kernel row skips every blocker check, so a type that is none of these must not stand as Kernel. Give the row " +
                "its owner and its measured blockers with CatalogueRow.Owned, or, for a type that truly is one of these, add it to " +
                "KernelCatalogue.TheKernelTypes.");

        private static void ThenNoClaimIsFalse(List<string> problems)
            => Assert.That(problems, Is.Empty,
                "A catalogue row says why its type stays in the Models, and that reason must be true of the compiled code and " +
                "the database model, or the type could have moved to its owner already:" + Environment.NewLine + string.Join(Environment.NewLine, problems));

        private static void ThenTheProblemNamesTheTypeAndTheBlocker(List<string> problems, string type, KernelBlocker claimed)
            => Assert.That(problems, Has.Some.Contains($"{type} claims {claimed}, but the type is free of {claimed}"),
                $"A row claiming {claimed} for {type}, which is free of it, should fail naming the type and the blocker, but the check said: {string.Join(" | ", problems)}");

        private static void ThenTheMoversListIsExactlyTheFreeTypes(IReadOnlyList<MovingType> movers, Dictionary<string, MeasuredType> measured)
        {
            var free = measured.Values.Where(type => type.Blockers.Count == 0 && !type.IsKernel).ToList();
            var cannotMove = free.Where(type => type.Destination is null).Select(type => $"{type.Name}: {type.WhyItHasNoDestination}").ToList();
            Assert.That(cannotMove, Is.Empty, "A free Models type needs a single owner to move to:" + Environment.NewLine + string.Join(Environment.NewLine, cannotMove));

            InlineBaseline.AssertMatches(
                free.Select(type => $"{type.OldNamespace}.{type.ShortName} -> {type.Destination} ({type.Owner})"),
                movers.Select(mover => $"{mover.OldFullName} -> {mover.NewNamespace} ({mover.Area})"),
                "A Models type that nothing keeps in the shared kernel belongs with the one owner that uses it. Put it on " +
                "KernelCatalogue.Movers with its owner and the namespace it moves to, as written here, and move it there.");
        }

        private static void ThenItIsFoundInExactlyOnePlace(string oldFullName, string newFullName, List<string> places)
            => Assert.That(places, Has.Count.EqualTo(1),
                $"{oldFullName} should be a top-level type either in its Models namespace or, once moved, as {newFullName}, but " +
                $"it was found in {places.Count} of them ({string.Join(", ", places)}). A mover found in neither was renamed or deleted: " +
                "update its entry on KernelCatalogue.Movers and its catalogue row. Found in both, the move left a copy behind: delete it.");

        private static void ThenThePatternKeepsItsRole(string field, List<string> breaks)
            => Assert.That(breaks, Is.Empty,
                $"The rule pattern {field} would not keep its declared role when the free Models types move to their owners:" + Environment.NewLine +
                string.Join(Environment.NewLine, breaks) + Environment.NewLine +
                "A part of the code must keep seeing a type wherever it lives, so widen it with the namespace the type moves to; an " +
                "old location must not see the type in its new place.");

        private static void ThenThePatternSeesThemAll(string field, List<string> unseen)
            => Assert.That(unseen, Is.Empty,
                $"The rule pattern {field} takes in an owner the free types move to, but does not match the new name of: " +
                $"{string.Join(", ", unseen)}. Once they move, its rule would stop guarding them. Widen the pattern, or take the owner off its entry.");

        private static void ThenEveryPatternIsDeclared(List<string> undeclared)
            => Assert.That(undeclared, Is.Empty,
                "These rule patterns see a free Models type but have no declared role, or are declared but gone:" + Environment.NewLine +
                string.Join(Environment.NewLine, undeclared) + Environment.NewLine +
                "How to fix: declare each pattern in ModelsPatternRoles as an old location or a part of the code, or list the owners " +
                "it takes in, and delete a declaration whose pattern is gone.");

        private static void ThenTheUndeclaredFieldIsNamed(List<PatternBreak> breaks, string field)
            => Assert.That(breaks.Where(found => found.Break == RoleBreak.NotDeclared).Select(found => found.Pattern.Field), Has.Member(field),
                $"A rule pattern that sees a free type and has no declared role should be named, but the role check found: {string.Join(" | ", breaks.Select(found => $"{found.Pattern.Field}: {found.Break}"))}");

        private static void ThenTheFailureNames(string failure, params string[] names)
            => Assert.That(names.Where(name => !failure.Contains(name, StringComparison.Ordinal)), Is.Empty,
                $"The check's failure should name each of {string.Join(", ", names)}, but it said: {failure}");

        private static void ThenTheOldNameStringsAreExactlyTheList(OldNameComparison comparison)
            => Assert.That(comparison.Unlisted.Concat(comparison.NotFound), Is.Empty,
                "The strings of the test project that name a free Models type by its Models name differ from ModelsStringsTheMoveRenames." + Environment.NewLine +
                $"Not on the list: {string.Join(Environment.NewLine, comparison.Unlisted)}" + Environment.NewLine +
                $"On the list but not found: {string.Join(Environment.NewLine, comparison.NotFound)}" + Environment.NewLine +
                "A string the move leaves behind would keep naming the old place, so a rule would quietly stop seeing the type. Put each " +
                "string on the list with its new text, and take an entry off once its string is gone.");

        private static string SourceOfTheRow(MeasuredType type)
        {
            var blockers = type.Blockers.Count == 0 ? string.Empty : ", " + string.Join(", ", type.Blockers.Select(blocker => $"KernelBlocker.{blocker}"));
            var owner = type.Owner ?? "?";
            return type.Blockers.Count == 0
                ? $"CatalogueRow.Free(\"{type.Name}\", \"{owner}\"), // {Describe(type)}"
                : $"CatalogueRow.Owned(\"{type.Name}\", \"{owner}\", \"{KernelCatalogue.FreedByFor(type.Blockers)}\"{blockers}), // {Describe(type)}";
        }

        private static string Describe(MeasuredType type)
            => $"contexts [{string.Join(", ", type.Contexts)}], held by [{string.Join(", ", type.Holders)}], path read by [{string.Join(", ", type.PinnedBy)}], file {type.File}";

        private static bool InFixtures(Type type)
            => type.Namespace?.StartsWith(typeof(MadeUpRules).Namespace!, StringComparison.Ordinal) == true;

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the source read.");

            return Directory.GetParent(directory!.FullName)!.FullName;
        }

        private sealed record OldNameComparison(IReadOnlyList<string> Unlisted, IReadOnlyList<string> NotFound);
    }

    /// <summary>A file outside the backend tests, with its text, that may read a Models file by its path.</summary>
    internal sealed record PathConsumer(string File, string Text);

    /// <summary>
    /// What the measurement reads: the compiled module, the namespaces that play each part, the stored types of the database
    /// model, the kernel types, the source file of each Models type and the files that may read one by path.
    /// </summary>
    internal sealed record ModelsWorld(
        ModuleDefinition Module,
        string ModelsRoot,
        string ModulesRoot,
        string PlatformRoot,
        string WireNamespaces,
        string DtoNamespaces,
        string InfrastructureNamespaces,
        IReadOnlySet<string> Entities,
        IReadOnlyList<string> KernelTypes,
        IReadOnlyDictionary<string, string> FileOf,
        IReadOnlyList<PathConsumer> PathConsumers,
        IReadOnlyDictionary<string, string> OwnersByDesign,
        IReadOnlyDictionary<string, string> ModelsSources);

    /// <summary>One top-level Models type as measured: its blockers, the contexts that use it, and where it would move.</summary>
    internal sealed record MeasuredType(
        string Name,
        string OldNamespace,
        string ShortName,
        bool IsKernel,
        IReadOnlyList<KernelBlocker> Blockers,
        IReadOnlyList<string> Contexts,
        IReadOnlyList<string> Holders,
        IReadOnlyList<string> PinnedBy,
        string File,
        string? Owner,
        string? Destination,
        string WhyItHasNoDestination);

    /// <summary>Measures every top-level Models type's blockers from the compiled module and the database model.</summary>
    internal static class ModelsMeasurement
    {
        private static readonly TimeSpan Timeout = NamespaceMoveLaw.PatternTimeout;

        private static readonly Regex APathToAModelsFile = new(@"(?<![A-Za-z0-9_])Models/[A-Za-z0-9_][A-Za-z0-9_/]*", RegexOptions.None, Timeout);

        private static readonly Regex ANamespace = new(@"^\s*namespace\s+(?<name>[A-Za-z0-9_.]+)", RegexOptions.Multiline, Timeout);

        private static readonly Regex ATypeDeclaration = new(
            @"^\s*(?:(?:public|internal|static|sealed|abstract|partial|readonly|file|ref)\s+)*(?:record\s+(?:class|struct)|class|record|struct|interface|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<generic><[^>]*>)?",
            RegexOptions.Multiline,
            Timeout);

        public static string OutermostName(Type type) => type.DeclaringType is null ? type.FullName! : OutermostName(type.DeclaringType);

        public static string WhatFreeOfMeans(KernelBlocker blocker)
            => blocker switch
            {
                KernelBlocker.Entity => "the database model stores no such type.",
                KernelBlocker.Wire => "no controller action signature or result, and no public property of an API DTO, reaches it, directly or through other types.",
                KernelBlocker.HeldBy => "no Models type that stays references it.",
                KernelBlocker.Shared => "types of fewer than two contexts use it.",
                KernelBlocker.ReachedFromOutsideItsModule => "no type outside its owner's module and outside the Models reaches it without already reaching the owner's Domain, no contracts type reaches it, or another blocker already keeps it.",
                KernelBlocker.InfraReached => "neither the data layer, nor a repository, nor the composition root names it.",
                KernelBlocker.PathPinned => "no script, workflow or frontend file names its source file by path.",
                KernelBlocker.Event => "it does not implement the domain-event base type.",
                KernelBlocker.SharesAFile => "every other type in its file is free and moves to the same owner.",
                _ => "the blocker is unknown.",
            };

        /// <summary>The source file of each top-level type declared under the Models folder, keyed by its name after the Models root.</summary>
        public static Dictionary<string, string> FilesOfTheTypesIn(string modelsFolder, string modelsRoot)
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            var projectFolder = Directory.GetParent(modelsFolder)!.FullName;

            foreach (var path in Directory.EnumerateFiles(modelsFolder, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var source = ProductionSource.WithoutComments(File.ReadAllText(path));
                var space = ANamespace.Match(source) is { Success: true } found ? found.Groups["name"].Value : modelsRoot;
                var relative = Path.GetRelativePath(projectFolder, path).Replace('\\', '/');

                foreach (System.Text.RegularExpressions.Match declaration in ATypeDeclaration.Matches(source))
                {
                    var arity = declaration.Groups["generic"].Success ? $"`{declaration.Groups["generic"].Value.Count(character => character == ',') + 1}" : string.Empty;
                    var fullName = $"{space}.{declaration.Groups["name"].Value}{arity}";
                    if (fullName.StartsWith(modelsRoot + ".", StringComparison.Ordinal))
                    {
                        files.TryAdd(fullName[(modelsRoot.Length + 1)..], relative);
                    }
                }
            }

            return files;
        }

        public static Dictionary<string, MeasuredType> Measure(ModelsWorld world)
        {
            var modelsPattern = $@"^{Regex.Escape(world.ModelsRoot)}(\.|$)";
            var types = world.Module.Types
                .Where(type => !type.Name.StartsWith('<') && IlDependencies.ResidesInNamespaceMatching(type, modelsPattern))
                .ToDictionary(type => type.FullName, StringComparer.Ordinal);
            string NameOf(string fullName) => fullName[(world.ModelsRoot.Length + 1)..];
            bool IsAModelsType(TypeReference reference) => reference is not GenericParameter && types.ContainsKey(Outermost(reference).FullName);

            var contexts = ContextsUsingEach(world, IsAModelsType);
            var onTheWire = SentOnTheWire(world);
            var reachedByInfrastructure = ReachedByInfrastructure(world, IsAModelsType);
            var references = IlDependencies.DependentsIn(world.Module, modelsPattern, reference => reference is not GenericParameter)
                .GroupBy(dependent => TopLevel(dependent.TypeName), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.SelectMany(dependent => dependent.Targets).Select(TopLevel).Where(target => target != group.Key).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);

            foreach (var (holder, constants) in ConstantsNamedBySource(world, types, NameOf))
            {
                if (!references.TryGetValue(holder, out var held))
                {
                    held = new HashSet<string>(StringComparer.Ordinal);
                    references[holder] = held;
                }

                held.UnionWith(constants);
            }

            var reachedFromOutside = ReachedFromOutsideTheirModule(world, types.Keys, contexts, IsAModelsType, NameOf);
            var baseBlockers = types.Keys.ToDictionary(
                fullName => fullName,
                fullName => BaseBlockersOf(world, types[fullName], NameOf(fullName), contexts, onTheWire, reachedByInfrastructure),
                StringComparer.Ordinal);

            var sharesAFile = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> staying;
            Dictionary<string, SortedSet<string>> holders;
            Dictionary<string, (string? Owner, string? Destination, string Why)> destinations;
            bool fileRuleGrew;
            bool reachedGrew;
            do
            {
                do
                {
                    staying = types.Keys.Where(fullName => baseBlockers[fullName].Count > 0 || sharesAFile.Contains(fullName) || world.KernelTypes.Contains(NameOf(fullName))).ToHashSet(StringComparer.Ordinal);
                    holders = HeldByClosure(types, references, staying, NameOf);
                    var stayingNow = staying;
                    var destinationsNow = DestinationsOfTheFreeTypes(world, types.Keys.Where(fullName => !stayingNow.Contains(fullName)).ToList(), contexts, references, staying, NameOf);
                    destinations = destinationsNow;
                    fileRuleGrew = false;
                    foreach (var fullName in types.Keys.Where(fullName => !stayingNow.Contains(fullName)).ToList())
                    {
                        var file = world.FileOf.GetValueOrDefault(NameOf(fullName));
                        var mates = file is null ? [] : types.Keys.Where(other => other != fullName && world.FileOf.GetValueOrDefault(NameOf(other)) == file).ToList();
                        var destination = destinationsNow.GetValueOrDefault(fullName).Destination;
                        if (mates.Exists(mate => stayingNow.Contains(mate) || destinationsNow.GetValueOrDefault(mate).Destination != destination))
                        {
                            fileRuleGrew |= sharesAFile.Add(fullName);
                        }
                    }
                }
                while (fileRuleGrew);
                reachedGrew = false;
                foreach (var fullName in types.Keys.Where(fullName => !staying.Contains(fullName) && reachedFromOutside.Contains(fullName)).ToList())
                {
                    baseBlockers[fullName].Add(KernelBlocker.ReachedFromOutsideItsModule);
                    reachedGrew = true;
                }
            }
            while (reachedGrew);

            return types.Keys.ToDictionary(
                NameOf,
                fullName =>
                {
                    var blockers = baseBlockers[fullName].ToList();
                    if (holders[fullName].Count > 0 && !world.KernelTypes.Contains(NameOf(fullName)))
                    {
                        blockers.Add(KernelBlocker.HeldBy);
                    }

                    if (sharesAFile.Contains(fullName))
                    {
                        blockers.Add(KernelBlocker.SharesAFile);
                    }

                    if (blockers.Contains(KernelBlocker.ReachedFromOutsideItsModule) && (blockers.Contains(KernelBlocker.HeldBy) || blockers.Contains(KernelBlocker.SharesAFile)))
                    {
                        blockers.Remove(KernelBlocker.ReachedFromOutsideItsModule);
                    }

                    var (owner, destination, why) = !sharesAFile.Contains(fullName) && destinations.TryGetValue(fullName, out var found) ? found : (null, null, string.Empty);
                    return new MeasuredType(
                        NameOf(fullName),
                        types[fullName].Namespace,
                        types[fullName].Name,
                        world.KernelTypes.Contains(NameOf(fullName)),
                        [.. blockers.Order()],
                        [.. contexts.GetValueOrDefault(fullName, []).Order(StringComparer.Ordinal)],
                        [.. holders[fullName]],
                        PinnedBy(world, NameOf(fullName)),
                        world.FileOf.GetValueOrDefault(NameOf(fullName), "unknown"),
                        owner,
                        destination,
                        why);
                },
                StringComparer.Ordinal);
        }

        private static Dictionary<string, SortedSet<string>> HeldByClosure(Dictionary<string, TypeDefinition> types, Dictionary<string, HashSet<string>> references, HashSet<string> staying, Func<string, string> nameOf)
        {
            var holders = types.Keys.ToDictionary(fullName => fullName, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            bool grew;
            do
            {
                grew = false;
                foreach (var (holder, targets) in references.Where(entry => staying.Contains(entry.Key)).ToList())
                {
                    foreach (var held in targets.Where(types.ContainsKey))
                    {
                        holders[held].Add(nameOf(holder));
                        grew |= staying.Add(held);
                    }
                }
            }
            while (grew);

            return holders;
        }

        private static List<KernelBlocker> BaseBlockersOf(ModelsWorld world, TypeDefinition type, string name, Dictionary<string, HashSet<string>> contexts, HashSet<string> onTheWire, HashSet<string> reachedByInfrastructure)
        {
            var blockers = new List<KernelBlocker>();
            if (world.KernelTypes.Contains(name))
            {
                return blockers;
            }

            if (world.Entities.Contains(type.FullName))
            {
                blockers.Add(KernelBlocker.Entity);
            }

            if (onTheWire.Contains(type.FullName))
            {
                blockers.Add(KernelBlocker.Wire);
            }

            if (contexts.GetValueOrDefault(type.FullName, []).Count >= 2)
            {
                blockers.Add(KernelBlocker.Shared);
            }

            if (reachedByInfrastructure.Contains(type.FullName))
            {
                blockers.Add(KernelBlocker.InfraReached);
            }

            if (PinnedBy(world, name).Count > 0)
            {
                blockers.Add(KernelBlocker.PathPinned);
            }

            if (type.Interfaces.Any(implemented => implemented.InterfaceType.Name == "IDomainEvent"))
            {
                blockers.Add(KernelBlocker.Event);
            }

            return blockers;
        }

        // Whether a type outside the Models would force its owner's inbound rules to grow: it reaches the type from outside
        // the owner's module without already reaching the owner's Domain (a rule matches the code both ways, so reaching the
        // Domain today means being on the list), or a contracts type of any context reaches it. The owner is the one context that
        // uses the type, or else the context the design names for its area; a platform-owned type has no module.
        private static HashSet<string> ReachedFromOutsideTheirModule(ModelsWorld world, IEnumerable<string> fullNames, Dictionary<string, HashSet<string>> contexts, Func<TypeReference, bool> isAModelsType, Func<string, string> nameOf)
        {
            var models = new Regex($@"^{Regex.Escape(world.ModelsRoot)}(\.|$)", RegexOptions.None, Timeout);
            var users = IlDependencies.DependentsIn(world.Module, "^", isAModelsType)
                .Select(dependent => (User: TopLevel(dependent.TypeName), dependent.Targets))
                .Where(dependent => !dependent.User.StartsWith('<') && !models.IsMatch(dependent.User))
                .SelectMany(dependent => dependent.Targets.Select(target => (Target: TopLevel(target), dependent.User)))
                .ToLookup(pair => pair.Target, pair => pair.User, StringComparer.Ordinal);
            var reachedByContracts = IlDependencies.DependentsIn(world.Module, @"(^|\.)Contracts(\.|$)", isAModelsType)
                .SelectMany(dependent => dependent.Targets.Select(TopLevel))
                .ToHashSet(StringComparer.Ordinal);
            var domainReachers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var reached = new HashSet<string>(StringComparer.Ordinal);

            foreach (var fullName in fullNames)
            {
                var name = nameOf(fullName);
                var area = name.Contains('.') ? name[..name.IndexOf('.')] : name;
                var found = contexts.GetValueOrDefault(fullName, []);
                var owner = found.Count == 1 ? found.Single() : world.OwnersByDesign.GetValueOrDefault(name) ?? world.OwnersByDesign.GetValueOrDefault(area);
                if (owner is null or "Platform")
                {
                    continue;
                }

                if (!domainReachers.TryGetValue(owner, out var alreadyInDomain))
                {
                    var domain = $@"^{Regex.Escape(world.ModulesRoot)}\.{Regex.Escape(owner)}\.Domain(\.|$)";
                    alreadyInDomain = IlDependencies.DependentsIn(world.Module, "^", target => IlDependencies.ResidesInNamespaceMatching(target, domain))
                        .Select(dependent => TopLevel(dependent.TypeName))
                        .ToHashSet(StringComparer.Ordinal);
                    domainReachers[owner] = alreadyInDomain;
                }

                var inside = $"{world.ModulesRoot}.{owner}.";
                if (reachedByContracts.Contains(fullName) || users[fullName].Any(user => !user.StartsWith(inside, StringComparison.Ordinal) && !alreadyInDomain.Contains(user)))
                {
                    reached.Add(fullName);
                }
            }

            return reached;
        }

        // The compiler inlines a constant, so no IL edge shows who reads it. For a type whose members are all constants, a Models
        // file that names it holds it: every type of that file is a holder.
        private static Dictionary<string, HashSet<string>> ConstantsNamedBySource(ModelsWorld world, Dictionary<string, TypeDefinition> types, Func<string, string> nameOf)
        {
            var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var typesOfFile = types.Keys.Where(fullName => world.FileOf.ContainsKey(nameOf(fullName))).GroupBy(fullName => world.FileOf[nameOf(fullName)], StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            foreach (var (constants, definition) in types.Where(pair => pair.Value.Fields.Count > 0 && pair.Value.Methods.Count == 0 && pair.Value.Properties.Count == 0 && pair.Value.Fields.All(field => field.IsLiteral)))
            {
                var ownFile = world.FileOf.GetValueOrDefault(nameOf(constants));
                var named = new Regex($@"\b{Regex.Escape(definition.Name)}\b", RegexOptions.None, Timeout);
                foreach (var holder in world.ModelsSources.Where(source => source.Key != ownFile && named.IsMatch(source.Value)).SelectMany(source => typesOfFile.GetValueOrDefault(source.Key, [])))
                {
                    if (!edges.TryGetValue(holder, out var targets))
                    {
                        targets = new HashSet<string>(StringComparer.Ordinal);
                        edges[holder] = targets;
                    }

                    targets.Add(constants);
                }
            }

            return edges;
        }

        public static Dictionary<string, string> SourcesOfTheModels(string modelsFolder)
        {
            var projectFolder = Directory.GetParent(modelsFolder)!.FullName;

            return Directory.EnumerateFiles(modelsFolder, "*.cs", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetRelativePath(projectFolder, path).Replace('\\', '/'), path => ProductionSource.WithoutComments(File.ReadAllText(path)), StringComparer.Ordinal);
        }

        // Contexts are the Modules.<X> namespaces only; the API, the data layer, the platform and the loose services are not.
        private static Dictionary<string, HashSet<string>> ContextsUsingEach(ModelsWorld world, Func<TypeReference, bool> isAModelsType)
        {
            var contextRoot = new Regex($@"^{Regex.Escape(world.ModulesRoot)}\.(?<context>[A-Za-z0-9_]+)(\.|$)", RegexOptions.None, Timeout);
            var users = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var context in world.Module.Types.Select(type => contextRoot.Match(type.Namespace)).Where(found => found.Success).Select(found => found.Groups["context"].Value).Distinct(StringComparer.Ordinal))
            {
                foreach (var target in IlDependencies.DependentsIn(world.Module, $@"^{Regex.Escape(world.ModulesRoot)}\.{Regex.Escape(context)}(\.|$)", isAModelsType).SelectMany(dependent => dependent.Targets))
                {
                    var top = TopLevel(target);
                    if (!users.TryGetValue(top, out var set))
                    {
                        set = new HashSet<string>(StringComparer.Ordinal);
                        users[top] = set;
                    }

                    set.Add(context);
                }
            }

            return users;
        }

        private static HashSet<string> ReachedByInfrastructure(ModelsWorld world, Func<TypeReference, bool> isAModelsType)
            => [.. IlDependencies.DependentsIn(world.Module, world.InfrastructureNamespaces, isAModelsType)
                .Concat(IlDependencies.DependentsIn(world.Module, "^", isAModelsType).Where(dependent => TopLevel(dependent.TypeName).EndsWith("Repository", StringComparison.Ordinal)))
                .SelectMany(dependent => dependent.Targets)
                .Select(TopLevel)];

        // What a controller action takes or answers, what a controller hands to a result, and what a public property of an API
        // DTO holds, followed through the public properties of every type reached.
        private static HashSet<string> SentOnTheWire(ModelsWorld world)
        {
            var all = world.Module.Types.SelectMany(TypeAndItsNestedTypes).ToList();
            var controllers = all.Where(type => IlDependencies.ResidesInNamespaceMatching(type, world.WireNamespaces) && IsAController(type)).ToList();

            var inSignatures = controllers.SelectMany(controller => controller.Methods)
                .Where(method => method.IsPublic)
                .SelectMany(method => method.Parameters.Select(parameter => parameter.ParameterType).Prepend(method.ReturnType));
            var handedToResults = controllers.SelectMany(TypeAndItsNestedTypes).SelectMany(type => type.Methods).Where(method => method.HasBody).SelectMany(ValuesHandedToAResult);
            var dtos = all.Where(type => IlDependencies.ResidesInNamespaceMatching(type, world.DtoNamespaces)).Cast<TypeReference>();

            var sent = new HashSet<string>(StringComparer.Ordinal);
            var definitions = all.ToDictionary(type => type.FullName, StringComparer.Ordinal);
            var pending = new Stack<TypeReference>(inSignatures.Concat(handedToResults).Concat(dtos));

            while (pending.Count > 0)
            {
                foreach (var type in Unwrapped(pending.Pop()).Where(type => sent.Add(type.FullName)))
                {
                    if (definitions.TryGetValue(type.FullName, out var definition))
                    {
                        foreach (var property in definition.Properties.Where(property => property.GetMethod is { IsPublic: true, IsStatic: false }))
                        {
                            pending.Push(property.PropertyType);
                        }
                    }
                }
            }

            return [.. sent.Select(TopLevel)];
        }

        // A free type moves to the one context that uses it: into its Domain when everything it references is the base
        // library, a Models type that stays, that Domain or another free type bound for the same Domain; else into its
        // Application. A free type no context uses belongs to the platform lane, in the area its Models namespace names.
        private static Dictionary<string, (string? Owner, string? Destination, string Why)> DestinationsOfTheFreeTypes(
            ModelsWorld world, List<string> free, Dictionary<string, HashSet<string>> contexts, Dictionary<string, HashSet<string>> references, HashSet<string> staying, Func<string, string> nameOf)
        {
            var answers = new Dictionary<string, (string? Owner, string? Destination, string Why)>(StringComparer.Ordinal);
            var ownerOf = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var fullName in free)
            {
                var users = contexts.GetValueOrDefault(fullName, []);
                if (users.Count == 1)
                {
                    ownerOf[fullName] = users.Single();
                    continue;
                }

                var name = nameOf(fullName);
                var area = name.Split('.') is { Length: > 1 } parts ? parts[0] : name;
                if (world.OwnersByDesign.TryGetValue(name, out var byDesign) || world.OwnersByDesign.TryGetValue(area, out byDesign))
                {
                    if (byDesign == "Platform")
                    {
                        answers[fullName] = ("Platform", $"{world.PlatformRoot}.{area}", string.Empty);
                    }
                    else
                    {
                        ownerOf[fullName] = byDesign;
                    }

                    continue;
                }

                answers[fullName] = (null, null, "no context uses it, so its owner cannot be measured, and the design names none for it or its Models area: name its owner in KernelCatalogue.OwnersByDesign");
            }

            var domainBound = ownerOf.Keys.ToHashSet(StringComparer.Ordinal);
            bool shrank;
            do
            {
                shrank = domainBound.RemoveWhere(fullName => references.GetValueOrDefault(fullName, []).Any(target => !MayBeReachedFromTheDomain(world, target, ownerOf[fullName], staying, domainBound, ownerOf))) > 0;
            }
            while (shrank);

            foreach (var (fullName, owner) in ownerOf)
            {
                answers[fullName] = (owner, $"{world.ModulesRoot}.{owner}.{(domainBound.Contains(fullName) ? "Domain" : "Application")}", string.Empty);
            }

            return answers;
        }

        private static bool MayBeReachedFromTheDomain(ModelsWorld world, string target, string owner, HashSet<string> staying, HashSet<string> domainBound, Dictionary<string, string> ownerOf)
        {
            var space = target.Contains('.', StringComparison.Ordinal) ? target[..target.LastIndexOf('.')] : string.Empty;

            return space.Length == 0
                || Regex.IsMatch(space, ModuleRules.TheBaseLibrary, RegexOptions.None, Timeout)
                || Regex.IsMatch(space, $@"^{Regex.Escape(world.ModulesRoot)}\.{Regex.Escape(owner)}\.Domain(\.|$)", RegexOptions.None, Timeout)
                || staying.Contains(target)
                || (domainBound.Contains(target) && ownerOf[target] == owner);
        }

        private static List<string> PinnedBy(ModelsWorld world, string name)
        {
            if (!world.FileOf.TryGetValue(name, out var file))
            {
                return [];
            }

            return [.. world.PathConsumers
                .Where(consumer => APathToAModelsFile.Matches(consumer.Text).Any(found => found.Value.Length > "Models/".Length && file.StartsWith(found.Value, StringComparison.Ordinal)))
                .Select(consumer => consumer.File)
                .Order(StringComparer.Ordinal)];
        }

        private static IEnumerable<TypeReference> ValuesHandedToAResult(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;

            for (var index = 1; index < instructions.Count; index++)
            {
                if (instructions[index].Operand is MethodReference called
                    && called.DeclaringType.Namespace == "Microsoft.AspNetCore.Mvc"
                    && called.Parameters.Count > 0
                    && called.Parameters[^1].ParameterType.FullName == "System.Object"
                    && ProducedBy(method, instructions[index - 1]) is { } produced)
                {
                    yield return produced;
                }
            }
        }

        private static TypeReference? ProducedBy(MethodDefinition method, Mono.Cecil.Cil.Instruction instruction)
            => instruction.OpCode.Code switch
            {
                Mono.Cecil.Cil.Code.Box => (TypeReference)instruction.Operand,
                Mono.Cecil.Cil.Code.Newobj => ((MethodReference)instruction.Operand).DeclaringType,
                Mono.Cecil.Cil.Code.Call or Mono.Cecil.Cil.Code.Callvirt => ((MethodReference)instruction.Operand).ReturnType,
                Mono.Cecil.Cil.Code.Ldfld or Mono.Cecil.Cil.Code.Ldsfld => ((FieldReference)instruction.Operand).FieldType,
                Mono.Cecil.Cil.Code.Ldloc_0 => method.Body.Variables[0].VariableType,
                Mono.Cecil.Cil.Code.Ldloc_1 => method.Body.Variables[1].VariableType,
                Mono.Cecil.Cil.Code.Ldloc_2 => method.Body.Variables[2].VariableType,
                Mono.Cecil.Cil.Code.Ldloc_3 => method.Body.Variables[3].VariableType,
                Mono.Cecil.Cil.Code.Ldloc_S or Mono.Cecil.Cil.Code.Ldloc => ((Mono.Cecil.Cil.VariableDefinition)instruction.Operand).VariableType,
                _ => null,
            };

        private static bool IsAController(TypeDefinition type)
        {
            for (var baseType = type.BaseType; baseType is not null;)
            {
                if (baseType.FullName is "Microsoft.AspNetCore.Mvc.ControllerBase" or "Microsoft.AspNetCore.Mvc.Controller")
                {
                    return true;
                }

                baseType = (baseType is GenericInstanceType generic ? generic.ElementType : baseType) is TypeDefinition inThisModule
                    ? inThisModule.BaseType
                    : null;
            }

            return false;
        }

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [Outermost(generic.ElementType), .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                GenericParameter => [],
                _ => [Outermost(type)],
            };

        private static TypeReference Outermost(TypeReference type) => type.DeclaringType is null ? type : Outermost(type.DeclaringType);

        private static string TopLevel(string typeName) => typeName.Split(['+', '/'])[0];
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures
{
    using System.Text.RegularExpressions;
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Data;
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;
    using Microsoft.EntityFrameworkCore;
    using Mono.Cecil;

    /// <summary>A made-up backend in the test assembly: a few Models types, two contexts, a database model and a controller.</summary>
    internal static class MadeUpWorld
    {
        public const string ARowForNoType = "ATypeNobodyWrote";

        private const string Root = "Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures";

        public static string ThePlantedRecordsDestination => $"{Root}.Modules.Alpha.Domain";

        public static IReadOnlyList<CatalogueRow> ACatalogueMissingThePlantedTypeWithARowForNoType { get; } =
        [
            CatalogueRow.Owned(nameof(AStoredRecord), "Alpha", "made up", KernelBlocker.Entity),
            CatalogueRow.Owned(nameof(ASentAnswer), "Alpha", "made up", KernelBlocker.Wire),
            CatalogueRow.Owned(nameof(ANotStoredRecord), "Alpha", "made up", KernelBlocker.Shared),
            CatalogueRow.Owned(nameof(ANotSentRecord), "Alpha", "made up", KernelBlocker.Shared),
            CatalogueRow.Free(ARowForNoType, "Alpha"),
        ];

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                [new MovingType(nameof(APlantedRecord), "Alpha", $"{Root}.Models", $"{Root}.Modules.Alpha.Domain")],
                [],
                $@"^{Regex.Escape(Root)}\.Modules\.Alpha\.Domain(\.|$)",
                new Regex("(?!)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));

        public static IReadOnlyList<string> TheKernelTypes { get; } = [];

        public static IReadOnlyList<CatalogueRow> CatalogueClaimingTheKernelFor(string type) => [CatalogueRow.Kernel(type)];

        public static IReadOnlyList<CatalogueRow> CatalogueClaiming(string type, KernelBlocker claimed)
            => [CatalogueRow.Owned(type, "Alpha", "made up", claimed)];

        public static ModelsWorld Of(ModuleDefinition testModule)
        {
            var options = new DbContextOptionsBuilder<AMadeUpCatalogueContext>().UseInMemoryDatabase($"kernel-catalogue-{Guid.NewGuid():N}").Options;
            using var context = new AMadeUpCatalogueContext(options);

            return new ModelsWorld(
                testModule,
                $"{Root}.Models",
                $"{Root}.Modules",
                $"{Root}.Platform",
                $@"^{Regex.Escape(Root)}\.API(\.|$)",
                $@"^{Regex.Escape(Root)}\.API\.DTO(\.|$)",
                $@"^{Regex.Escape(Root)}\.Infrastructure(\.|$)",
                context.Model.GetEntityTypes().Select(entity => ModelsMeasurement.OutermostName(entity.ClrType)).ToHashSet(StringComparer.Ordinal),
                TheKernelTypes,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["AStoredRecord"] = "Models/AStoredRecord.cs", ["AConstantsOnly"] = "Models/AConstantsOnly.cs" },
                [],
                new Dictionary<string, string>(StringComparer.Ordinal) { ["AConstantsNobodyNames"] = "Alpha" },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["Models/AStoredRecord.cs"] = "internal sealed record AStoredRecord(int Id) { public int Cap => AConstantsOnly.Limit; }", ["Models/AConstantsOnly.cs"] = "internal static class AConstantsOnly { public const int Limit = 3; }" });
        }
    }

    /// <summary>A made-up rule class whose pattern takes in the context the planted type moves to, declared nowhere.</summary>
    internal static class MadeUpRules
    {
        public const string TheAlphaModule = @"^Lighthouse\.Backend\.Tests\.Architecture\.KernelCatalogueFixtures\.Modules\.Alpha(\.|$)";
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models
{
    internal sealed record APlantedRecord(int Value);

    internal sealed record AStoredRecord(int Id);

    internal sealed record ASentAnswer(int Value);

    internal sealed record ANotStoredRecord(int Value);

    internal sealed record ANotSentRecord(int Value);

    internal sealed record AReachedRecord(int Value);

    internal static class AConstantsOnly
    {
        public const int Limit = 3;
    }

    internal static class AConstantsNobodyNames
    {
        public const int Limit = 4;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Data
{
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;
    using Microsoft.EntityFrameworkCore;

    internal sealed class AMadeUpCatalogueContext(DbContextOptions<AMadeUpCatalogueContext> options) : DbContext(options)
    {
        public DbSet<AStoredRecord> StoredRecords => Set<AStoredRecord>();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.API
{
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;
    using Microsoft.AspNetCore.Mvc;

    internal abstract class AMadeUpCatalogueController : ControllerBase
    {
        public ActionResult<ASentAnswer> Get() => NotFound();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Modules.Alpha.Application
{
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;

    internal static class AlphaRules
    {
        public static APlantedRecord Planted(int value) => new(value);

        public static ANotStoredRecord NotStored(int value) => new(value);

        public static ANotSentRecord NotSent(int value) => new(value);

        public static AReachedRecord Reached(int value) => new(value);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Services
{
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;

    internal static class AMadeUpOutsideService
    {
        public static AReachedRecord Read(int value) => new(value);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Modules.Beta.Application
{
    using Lighthouse.Backend.Tests.Architecture.KernelCatalogueFixtures.Models;

    internal static class BetaRules
    {
        public static ANotStoredRecord NotStored(int value) => new(value);

        public static ANotSentRecord NotSent(int value) => new(value);
    }
}
