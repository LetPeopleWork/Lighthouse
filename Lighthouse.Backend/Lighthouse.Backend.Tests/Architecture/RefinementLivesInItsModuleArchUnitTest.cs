using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Refinement types listed as moving on RefinementMembership live in Lighthouse.Backend.Modules.Refinement: each in the
    /// namespace of its layer and in no other, each declared by a file in the folder of that namespace, with no file of the moving
    /// set left under the two old Services folders. The types listed as staying are where the map says. The map is the only owner of what moves where; this
    /// test adds no list of its own.
    /// </summary>
    [TestFixture]
    public class RefinementLivesInItsModuleArchUnitTest
    {
        private const string ProductionProject = "Lighthouse.Backend/Lighthouse.Backend/";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<IReadOnlyList<(string Namespace, string Name)>> TypesInTheCode = new(() =>
            [.. ProductionModule.Value.Types.Select(type => (type.Namespace, type.Name))]);

        [OneTimeTearDown]
        public void DisposeModules()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void EveryMovingRefinementType_IsFoundInItsNewModuleNamespaceOnly()
        {
            var problems = WhenTheNamespacesOfTheMovingTypesAreChecked(TypesInTheCode.Value, RefinementMembership.MovingTypes);

            ThenThereAreNoProblems(problems, "a moving Refinement type is not in its module namespace only",
                "A type that moved must exist once, in Modules.Refinement.<Layer>: a type left in the Services namespace or declared twice is a half move that the rules would read wrongly.",
                "Move the declaring file into Modules/Refinement/<Layer> and change its namespace; delete the old file.");
        }

        [Test]
        public void EveryMovingRefinementFile_SitsInTheFolderOfItsNewNamespace_WithTheTypesTheMapListsForIt()
        {
            var problems = WhenTheFilesOfTheMovingTypesAreChecked(RefinementMembership.MovingTypes, FileExists, ReadFile);

            ThenThereAreNoProblems(problems, "a moving Refinement file is not in the folder of its namespace",
                "Namespace equal to folder is how the module's layers are read from the tree.",
                "Put the file in Modules/Refinement/Domain or Modules/Refinement/Application as the map lists, declaring that namespace and the types the map lists for it.");
        }

        [Test]
        public void NoFileOfTheMovingRefinementSet_IsLeftUnderTheOldServicesFolders()
        {
            var problems = WhenTheOldFoldersAreChecked(RefinementMembership.MovingTypes, FileExists);

            ThenThereAreNoProblems(problems, "a moving Refinement file is still under Services",
                "A file left behind keeps the old namespace alive beside the new one.",
                "Delete the old file once its content lives in the module folder.");
        }

        [Test]
        public void EveryRefinementTypeThatStays_IsStillWhereTheMapListsIt()
        {
            var problems = WhenTheStayingTypesAreChecked(TypesInTheCode.Value, RefinementMembership.TypesThatStay);

            ThenThereAreNoProblems(problems, "a Refinement type that stays has moved or is gone",
                "The sizing-log repository pair, the shared models, the controllers, the validator, the DTOs and WeeklyRecurrence keep their place so the stored model and the wire stay as they are.",
                "Put the type back in the namespace the map lists, or change the map if its owner agreed to move it.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespace_IsNamedAsNotInItsModuleNamespace()
        {
            var left = MovingType("RefinementList");
            var code = CodeAfterTheMove().Select(type => type == (left.NewNamespace, left.Name) ? (left.OldNamespace, left.Name) : type).ToList();

            var problems = WhenTheNamespacesOfTheMovingTypesAreChecked(code, RefinementMembership.MovingTypes);

            ThenTheProblemsNameOnly(problems, left.Name);
        }

        [Test]
        public void ATypeCopiedIntoTheModuleButKeptInItsOldNamespace_IsNamedAsFoundTwice()
        {
            var copied = MovingType("NeedBand");
            var code = CodeAfterTheMove().Append((copied.OldNamespace, copied.Name)).ToList();

            var problems = WhenTheNamespacesOfTheMovingTypesAreChecked(code, RefinementMembership.MovingTypes);

            ThenTheProblemsNameOnly(problems, copied.Name);
        }

        [Test]
        public void AnApplicationTypeDeclaredInTheDomainNamespace_IsNamedAsInTheWrongLayer()
        {
            var misplaced = MovingType("StageRuleMatcher");
            var code = CodeAfterTheMove().Select(type => type == (misplaced.NewNamespace, misplaced.Name) ? (RefinementType.NewRootNamespace + ".Domain", misplaced.Name) : type).ToList();

            var problems = WhenTheNamespacesOfTheMovingTypesAreChecked(code, RefinementMembership.MovingTypes);

            ThenTheProblemsNameOnly(problems, misplaced.Name);
        }

        [Test]
        public void AFileInTheWrongModuleFolder_IsNamedAsNotInTheFolderOfItsNamespace()
        {
            var misplaced = MovingType("SizingRefusal");
            var files = FilesAfterTheMove();
            files.Remove(FileOf(misplaced));
            files[ProductionProject + "Modules/Refinement/Domain/" + misplaced.File] = SourceDeclaring(misplaced.NewNamespace, misplaced.File);

            var problems = WhenTheFilesOfTheMovingTypesAreChecked(RefinementMembership.MovingTypes, files.ContainsKey, files.GetValueOrDefault);

            ThenTheProblemsNameOnly(problems, misplaced.File);
        }

        [Test]
        public void AFileInTheRightFolderThatStillDeclaresTheOldNamespace_IsNamedAsNotDeclaringItsNamespace()
        {
            var stale = MovingType("RefinementCalendar");
            var files = FilesAfterTheMove();
            files[FileOf(stale)] = SourceDeclaring(stale.OldNamespace, stale.File);

            var problems = WhenTheFilesOfTheMovingTypesAreChecked(RefinementMembership.MovingTypes, files.ContainsKey, files.GetValueOrDefault);

            ThenTheProblemsNameOnly(problems, stale.File);
        }

        [Test]
        public void AFileLeftUnderTheOldFolder_IsNamedAsNotMoved()
        {
            var left = MovingType("ISizingLogCommands");
            var files = FilesAfterTheMove();
            files[ProductionProject + OldFolderOf(left) + "/" + left.File] = SourceDeclaring(left.OldNamespace, left.File);

            var problems = WhenTheOldFoldersAreChecked(RefinementMembership.MovingTypes, files.ContainsKey);

            ThenTheProblemsNameOnly(problems, left.File);
        }

        [Test]
        public void AStayingTypeMovedIntoTheModule_IsNamedAsNotWhereTheMapListsIt()
        {
            var moved = RefinementMembership.TypesThatStay.Single(type => type.Name == "SizingLogRepository");
            var code = CodeAfterTheMove().Select(type => type == (moved.Namespace, moved.Name) ? (RefinementType.NewRootNamespace + ".Application", moved.Name) : type).ToList();

            var problems = WhenTheStayingTypesAreChecked(code, RefinementMembership.TypesThatStay);

            ThenTheProblemsNameOnly(problems, moved.Name);
        }

        [Test]
        public void TheCodeAfterTheMove_HasNoProblemInAnyCheck()
        {
            var files = FilesAfterTheMove();

            var problems = WhenTheNamespacesOfTheMovingTypesAreChecked(CodeAfterTheMove(), RefinementMembership.MovingTypes)
                .Concat(WhenTheFilesOfTheMovingTypesAreChecked(RefinementMembership.MovingTypes, files.ContainsKey, files.GetValueOrDefault))
                .Concat(WhenTheOldFoldersAreChecked(RefinementMembership.MovingTypes, files.ContainsKey))
                .Concat(WhenTheStayingTypesAreChecked(CodeAfterTheMove(), RefinementMembership.TypesThatStay))
                .ToList();

            ThenThereAreNoProblems(problems, "the made-up finished move is read as having problems",
                "The checks must pass a correct move, or they would fail for a reason that is not the move.",
                "Fix the check that names a correct placement.");
        }

        private static List<string> WhenTheNamespacesOfTheMovingTypesAreChecked(IEnumerable<(string Namespace, string Name)> code, IEnumerable<RefinementType> moving)
        {
            var found = code.ToList();

            return [.. moving
                .Select(type => (Type: type, Places: found
                    .Where(candidate => candidate.Name == type.Name && (candidate.Namespace == type.OldNamespace || IsInTheModule(candidate.Namespace)))
                    .Select(candidate => candidate.Namespace)
                    .Order(StringComparer.Ordinal)
                    .ToList()))
                .Where(entry => !entry.Places.SequenceEqual([entry.Type.NewNamespace]))
                .Select(entry => $"{entry.Type.Name}: expected only {entry.Type.NewNamespace}, found in [{string.Join(", ", entry.Places)}]")];
        }

        private static List<string> WhenTheFilesOfTheMovingTypesAreChecked(IEnumerable<RefinementType> moving, Func<string, bool> fileExists, Func<string, string?> readFile)
        {
            var problems = new List<string>();

            foreach (var file in moving.GroupBy(type => type.File))
            {
                var first = file.First();
                var path = FileOf(first);

                if (!fileExists(path))
                {
                    problems.Add($"{file.Key}: expected at {path}, which does not exist");
                    continue;
                }

                var source = readFile(path) ?? string.Empty;

                if (!DeclaresNamespace(source, first.NewNamespace))
                {
                    problems.Add($"{file.Key}: sits in the folder of {first.NewNamespace} but does not declare that namespace");
                }

                problems.AddRange(file.Where(type => !DeclaresType(source, type.Name)).Select(type => $"{file.Key}: does not declare {type.Name}"));
            }

            return problems;
        }

        private static List<string> WhenTheOldFoldersAreChecked(IEnumerable<RefinementType> moving, Func<string, bool> fileExists)
            => [.. moving
                .Select(type => (type.File, Path: ProductionProject + OldFolderOf(type) + "/" + type.File))
                .Distinct()
                .Where(file => fileExists(file.Path))
                .Select(file => $"{file.File}: still at {file.Path}")];

        private static List<string> WhenTheStayingTypesAreChecked(IEnumerable<(string Namespace, string Name)> code, IEnumerable<RefinementTypeThatStays> staying)
        {
            var found = code.ToList();

            return [.. staying
                .Where(type => found.Count(candidate => candidate.Namespace == type.Namespace && candidate.Name == type.Name) != 1)
                .Select(type => $"{type.Name}: expected once in {type.Namespace}")];
        }

        private static void ThenThereAreNoProblems(List<string> problems, string what, string why, string how)
            => Assert.That(problems, Is.Empty, $"WHAT: {what}: {string.Join("; ", problems)}. WHY: {why} HOW: {how}");

        private static void ThenTheProblemsNameOnly(List<string> problems, string subject)
        {
            Assert.That(problems, Has.Count.EqualTo(1), $"WHAT: exactly one problem about {subject} was expected, got [{string.Join("; ", problems)}]. WHY: a check that names more or less than the one wrong thing cannot guide the move. HOW: make the check name each wrong placement once and nothing else.");
            Assert.That(problems[0], Does.StartWith(subject), $"WHAT: the problem names another subject than {subject}: {problems[0]}. WHY: the move owner must see which type or file is wrong. HOW: start each problem with the type or file name.");
        }

        private static RefinementType MovingType(string name) => RefinementMembership.MovingTypes.Single(type => type.Name == name);

        private static List<(string Namespace, string Name)> CodeAfterTheMove()
            => [.. RefinementMembership.MovingTypes.Select(type => (type.NewNamespace, type.Name))
                .Concat(RefinementMembership.TypesThatStay.Select(type => (type.Namespace, type.Name)))];

        private static Dictionary<string, string> FilesAfterTheMove()
            => RefinementMembership.MovingTypes
                .GroupBy(type => type.File)
                .ToDictionary(
                    file => FileOf(file.First()),
                    file => string.Join("\n", file.Select(type => SourceDeclaring(type.NewNamespace, type.Name))));

        private static string SourceDeclaring(string ns, string typeOrFile)
            => $"namespace {ns};\npublic class {Path.GetFileNameWithoutExtension(typeOrFile)} {{}}\n";

        private static string FileOf(RefinementType type) => $"{ProductionProject}Modules/Refinement/{type.Layer}/{type.File}";

        private static string OldFolderOf(RefinementType type) => string.Join('/', type.OldNamespace.Split('.').Skip(2));

        private static bool IsInTheModule(string ns) => ns == RefinementType.NewRootNamespace || ns.StartsWith(RefinementType.NewRootNamespace + ".", StringComparison.Ordinal);

        private static bool DeclaresNamespace(string source, string ns)
            => Regex.IsMatch(source, @"^\s*namespace\s+" + Regex.Escape(ns) + @"\s*(;|\{|$)", RegexOptions.Multiline, PatternTimeout);

        private static bool DeclaresType(string source, string name)
            => Regex.IsMatch(source, @"\b(class|record|interface|enum|struct)\s+" + Regex.Escape(name) + @"\b", RegexOptions.None, PatternTimeout);

        private static bool FileExists(string relativePath) => File.Exists(Absolute(relativePath));

        private static string? ReadFile(string relativePath) => File.Exists(Absolute(relativePath)) ? File.ReadAllText(Absolute(relativePath)) : null;

        private static string Absolute(string relativePath) => Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the read of the source files.");

            return Directory.GetParent(directory!.FullName)!.FullName;
        }
    }
}
