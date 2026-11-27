using System.Diagnostics;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Work Item, Team data, blocked, rule, dependency, parent-placement and refresh-log services and their ports live in the
    /// module's Application (the dependency ones in its Dependencies folder), none is left under Services, and the Application
    /// reaches no EF Core, data layer, API or module Adapters. Every rule list that names a moved type
    /// names it in its new place. The types are named as strings and read from the compiled production assembly, so this class
    /// compiles before and after the move, and a type that has not moved yet is a failure that names it. The rules also run on
    /// made-up types: an offender must be reported and its compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class WorkItemsApplicationLayoutArchUnitTest
    {
        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.WorkItems.Application";

        private const string DependenciesApplicationNamespace = "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies";

        private static string ContractsNamespace => ContextContractsMembership.ContractsOf("WorkItems");

        private const string WorkItemsApplication = @"^Lighthouse\.Backend\.Modules\.WorkItems\.Application(\.|$)";

        private const string WorkItemsApplicationFolder = "Modules/WorkItems/Application";

        private const string DependenciesApplicationFolder = "Modules/WorkItems/Application/Dependencies";

        private const string ContractsFolder = "Modules/WorkItems/Contracts";

        private const string WhatTheApplicationMayNotReach =
            @"^(Microsoft\.EntityFrameworkCore|Lighthouse\.Backend\.Data|Lighthouse\.Backend\.API|Lighthouse\.Backend\.Modules\.WorkItems\.Adapters)(\.|$)";

        private const string FixtureApplications = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.ApplicationLayout\.";

        private const string AnApplicationThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.ApplicationLayout\.NothingLivesHere(\.|$)";

        private const string FixtureApplicationNamespace = "Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Application";

        private const string FixtureDependenciesNamespace = "Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Application.Dependencies";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.ApplicationLayout\.Services\.Implementation\.WorkItems(\.|$)";

        private const string FixtureStorage = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.ApplicationLayout\.Neighbours\.Storage(\.|$)";

        private const string FixtureReachingStorageOrClean = FixtureApplications + "(ReachingStorage|Clean)$";

        private const string WhereTheTypesLivedBefore =
            @"(?<![\w.])(Lighthouse\.Backend\.)?Services\.(Implementation|Interfaces)\.((WorkItems|TeamData|WorkItemRules|Dependencies|Parents)\.)?";

        // Generic types carry their arity in the compiled name.
        private static readonly string[] ApplicationTypes =
        [
            "WorkItemService",
            "IWorkItemService",
            "BlockedItemService",
            "TeamDataService",
            "ITeamDataService",
            "RuleEvaluator`1",
            "WorkItemFieldProvider",
            "FeatureFieldProvider",
            "ParentPlacementReport",
            "RecordPlacement",
            "RefreshLogService",
        ];

        private static readonly string[] ContractsTypes =
        [
            "IBlockedItemService",
            "IRuleEvaluator`1",
            "IRuleFieldProvider`1",
            "IRefreshLogService",
            "IWhatTheForecastWaitsFor",
        ];

        private static readonly string[] DependencyApplicationTypes =
        [
            "DependencyDecision",
            "IDependencyDecision",
            "DependencyReconciler",
            "IDependencyReconciler",
            "DependencyRefreshReporter",
            "IDependencyRefreshReporter",
            "FeatureDependencyRendering",
            "DependenciesAsRead",
            "WhatTheForecastWaitsFor",
        ];

        private static readonly string[] ApplicationFiles =
        [
            "WorkItemService.cs",
            "IWorkItemService.cs",
            "BlockedItemService.cs",
            "TeamDataService.cs",
            "ITeamDataService.cs",
            "RuleEvaluator.cs",
            "WorkItemFieldProvider.cs",
            "FeatureFieldProvider.cs",
            "ParentPlacementReport.cs",
            "RefreshLogService.cs",
        ];

        private static readonly string[] ContractsFiles =
        [
            "IBlockedItemService.cs",
            "IRuleEvaluator.cs",
            "IRuleFieldProvider.cs",
            "IRefreshLogService.cs",
            "IWhatTheForecastWaitsFor.cs",
        ];

        private static readonly string[] DependencyApplicationFiles =
        [
            "DependencyDecision.cs",
            "IDependencyDecision.cs",
            "DependencyReconciler.cs",
            "IDependencyReconciler.cs",
            "DependencyRefreshReporter.cs",
            "IDependencyRefreshReporter.cs",
            "FeatureDependencyRendering.cs",
            "WhatTheForecastWaitsFor.cs",
        ];

        private static readonly Regex AnOldFullName = new(
            $@"{WhereTheTypesLivedBefore}(?<type>{string.Join("|", AllMovedNames())})\b",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        private static readonly Regex AnOldFilePath = new(
            @"Services/(Implementation|Interfaces)/((WorkItems|TeamData|WorkItemRules|Dependencies|Parents)/)?(?<type>" + string.Join("|", AllMovedNames()) + @")\.cs",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsApplicationLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheWorkItemTeamDataBlockedRuleParentPlacementAndRefreshLogServices_LiveInTheModuleApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, ApplicationTypes).Check();
        }

        [Test]
        public void TheDependencyServicesAndTheirPorts_LiveInTheModuleApplicationDependenciesFolder_NotFlatInTheApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, DependenciesApplicationNamespace, DependencyApplicationTypes).Check();
        }

        [Test]
        public void TheTypesTheOtherContextsReach_LiveInTheModuleContracts_AndNoneIsLeftInTheApplication()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ContractsNamespace, ContractsTypes).Check();
        }

        [Test]
        public void TheWorkItemsApplicationFiles_SitInTheModuleApplicationFolders_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(WorkItemsApplicationFolder, ApplicationFiles);
            ThenEachFileSitsOnlyIn(DependenciesApplicationFolder, DependencyApplicationFiles);
            ThenEachFileSitsOnlyIn(ContractsFolder, ContractsFiles);
        }

        [Test]
        public void TheWorkItemsApplication_ReachesNoEntityFrameworkNoDataLayerNoApiAndNoModuleAdapters()
        {
            WhenTheApplicationIsCheckedForReachingStorage(ProductionModule.Value, WorkItemsApplication, WhatTheApplicationMayNotReach).Check();
        }

        [Test]
        public void AWorkItemsApplicationTypeThatReachesStorage_IsReportedAndItsCompliantTwinIsNot()
        {
            var verdict = WhenTheApplicationIsCheckedForReachingStorage(FixtureModule.Value, FixtureReachingStorageOrClean, FixtureStorage);

            var offender = typeof(global::Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.ReachingStorage.AnApplicationTypeReachingStorage).FullName!;
            var twin = typeof(global::Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Clean.AnApplicationTypeReachingNothingForbidden).FullName!;
            var storage = typeof(global::Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Neighbours.Storage.AStorageType).FullName!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(twin).And.Contain(offender),
                    "The rule did not look at both made-up Application types. Why it matters: a twin the rule never looked at proves nothing by passing. " +
                    "How to fix: the rule must examine every type in the Application namespace it is given.");

                Assert.That(
                    verdict.Edges,
                    Is.EqualTo(new[] { $"{offender} -> {storage}" }),
                    $"Expected exactly {offender} to be reported as reaching {storage}. Why it matters: an Application type that reaches storage must be caught " +
                    "and a twin that does not must be left alone, or the rule flags everything. How to fix: report every edge from the Application into the forbidden namespaces and nothing else.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, "The Application rule passed with a type that reaches storage.");
            Assert.That(failure!.Message, Does.Contain(offender).And.Contain(storage), "The failure must name the offending type and what it reached.");
        }

        [Test]
        public void AWorkItemsApplicationThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var verdict = WhenTheApplicationIsCheckedForReachingStorage(FixtureModule.Value, AnApplicationThatHoldsNothing, FixtureStorage);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "An Application rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing stays green forever and guards nothing. " +
                "How to fix: the rule must fail when the Application it was given holds no type.");

            Assert.That(failure!.Message, Does.Contain(AnApplicationThatHoldsNothing), "The failure must name the Application pattern that held nothing.");
        }

        [Test]
        public void AnApplicationTypeLeftUnderServices_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplicationNamespace, ["AWellPlacedService"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplicationNamespace, ["ALeftBehindService"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in the Application and has no copy under Services was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left under Services passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindService"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void ADependencyTypePlacedFlatInTheApplication_IsNamed_AndOneInTheDependenciesFolderIsNot()
        {
            var inItsFolder = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureDependenciesNamespace, ["AWellPlacedDependencyService"], FixtureFormerHome);
            var flat = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureDependenciesNamespace, ["AFlatDependencyService"], FixtureFormerHome);

            Assert.DoesNotThrow(inItsFolder.Check, "A dependency type in the Dependencies folder was reported. How to fix: the rule must accept the folder the design names.");

            var failure = Assert.Throws<AssertionException>(flat.Check, "A dependency type placed flat in the Application passed the layout rule. Why it matters: the Dependencies folder is where the single-decision rule looks. How to fix: expect the type in the Dependencies folder, so a flat copy is missing there.");
            Assert.That(failure!.Message, Does.Contain("AFlatDependencyService"), "The failure must name the dependency type that sits flat.");
        }

        [Test]
        public void NoTrackedTestOrProductionSource_NamesAMovedWorkItemsType_AtItsOldHome()
        {
            var stale = ProjectFilesNamingAMovedTypeAtItsOldHome();

            Assert.That(
                stale,
                Is.Empty,
                $"These tracked source files still name a moved Work Items type at its old home: [{string.Join("; ", stale)}]. Why it matters: a rule list or a path that " +
                "names the old full name checks, a type that no longer lives there, so the rule is green over nothing while the moved type goes unguarded. " +
                "How to fix: write each full name, with the type's new home under Modules/WorkItems/Application, leaving the lists compared in both directions.");
        }

        [TestCase("\"Lighthouse.Backend.Services.Implementation.WorkItems.WorkItemService\",", "Lighthouse.Backend.Services.Implementation.WorkItems.WorkItemService")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.TeamData.TeamDataService\",", "Lighthouse.Backend.Services.Implementation.TeamData.TeamDataService")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.RefreshLogService\"", "Lighthouse.Backend.Services.Implementation.RefreshLogService")]
        [TestCase("\"**/Services/Implementation/Dependencies/DependencyRefreshReporter.cs\",", "Services/Implementation/Dependencies/DependencyRefreshReporter.cs")]
        [TestCase("    /// <see cref=\"Services.Implementation.WorkItemRules.RuleEvaluator{T}\"/>, and every", "Services.Implementation.WorkItemRules.RuleEvaluator")]
        public void AnOldHomeInARuleListOrAPath_IsFound(string line, string expectedHit)
        {
            Assert.That(
                StaleNamesIn(line),
                Is.EqualTo(new[] { expectedHit }),
                "The scan missed an old full name or file path. Why it matters: a scan that cannot see an old home cannot fail when a list keeps one. How to fix: match the old namespace and the old folder of every moved type.");
        }

        [TestCase("\"Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService\",")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.WorkItems.SomethingThatDidNotMove\",")]
        [TestCase("\"Lighthouse.Backend.Modules.TrackerIntegration.Application.DependencySourceSelector\",")]
        [TestCase("\"**/Modules/WorkItems/Application/Dependencies/DependencyRefreshReporter.cs\",")]
        [TestCase("    /// <see cref=\"Modules.WorkItems.Application.RuleEvaluator{T}\"/>, and every")]
        public void ANewHomeOrATypeThatStays_IsNotFound(string line)
        {
            Assert.That(
                StaleNamesIn(line),
                Is.Empty,
                "The scan reported a name that is correct. Why it matters: a scan that flags the new home makes the move impossible to finish. How to fix: report only the old namespace or folder of a type that moved.");
        }

        private static OutboundVerdict WhenTheApplicationIsCheckedForReachingStorage(ModuleDefinition module, string application, string forbidden)
            => ModuleRules.OutboundEdges(
                module,
                application,
                forbidden,
                [],
                $"Types in namespaces matching \"{application}\" should reach nothing in namespaces matching \"{forbidden}\", because the Application holds the module's use cases " +
                "and reaches storage only through the ports of the module, so that the module can change how it stores without touching a use case. An Application type that reaches EF Core, " +
                "the data layer, the API or the module's Adapters ties a use case to one way of storing or serving. Ask through a port the Application owns instead.");

        private static IEnumerable<string> AllMovedNames()
            => ApplicationTypes.Concat(DependencyApplicationTypes).Concat(ContractsTypes).Select(name => name.Split('`')[0]);

        private static List<string> StaleNamesIn(string text)
            => [.. AnOldFullName.Matches(text).Select(match => match.Value), .. AnOldFilePath.Matches(text).Select(match => match.Value)];

        private static List<string> ProjectFilesNamingAMovedTypeAtItsOldHome()
        {
            var repositoryRoot = Path.GetDirectoryName(SolutionDirectory())!;
            var thisFile = $"{nameof(WorkItemsApplicationLayoutArchUnitTest)}.cs";

            var files = TrackedFiles(repositoryRoot)
                .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                    && (path.StartsWith("Lighthouse.Backend/Lighthouse.Backend.Tests/", StringComparison.Ordinal)
                        || path.StartsWith("Lighthouse.Backend/Lighthouse.Backend/", StringComparison.Ordinal))
                    && Path.GetFileName(path) != thisFile)
                .ToList();

            Assert.That(files, Is.Not.Empty, "git listed no tracked source file of the backend, so the scan would pass without looking at anything.");

            return
            [
                .. files.SelectMany(file => StaleNamesIn(File.ReadAllText(Path.Combine(repositoryRoot, file))).Distinct(StringComparer.Ordinal)
                    .Select(name => $"{file} names {name}")),
            ];
        }

        // Only tracked files count: a build output or a local config is no part of the committed code.
        private static List<string> TrackedFiles(string repositoryRoot)
        {
            var start = new ProcessStartInfo("git")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("ls-files");

            Process? process = null;
            try
            {
                process = Process.Start(start);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                Assert.Fail($"The scan needs git on the machine to list the tracked files, and it could not be started: {exception.Message}. Why it matters: without it the test cannot tell which files are committed. How to fix: install git and put it on the PATH.");
            }

            using var started = process!;
            var outputRead = started.StandardOutput.ReadToEndAsync();
            var errorsRead = started.StandardError.ReadToEndAsync();
            Task.WaitAll(outputRead, errorsRead);
            started.WaitForExit();

            Assert.That(started.ExitCode, Is.Zero, $"git ls-files failed: {errorsRead.Result}");
            return [.. outputRead.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())];
        }

        private static string SolutionDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the repository root.");
            return directory!.FullName;
        }

        private static void ThenEachFileSitsOnlyIn(string folder, IReadOnlyCollection<string> fileNames)
        {
            var paths = ProductionSource.Files().Select(file => file.RelativePath).ToList();

            var misplaced = fileNames
                .SelectMany(fileName => paths.Where(path => Path.GetFileName(path) == fileName && path != $"{folder}/{fileName}"))
                .ToList();
            var missing = fileNames.Where(fileName => !paths.Contains($"{folder}/{fileName}", StringComparer.Ordinal)).ToList();

            Assert.That(
                missing.Concat(misplaced),
                Is.Empty,
                $"Expected each of {string.Join(", ", fileNames)} to sit in the production project's {folder} folder and nowhere else; " +
                $"missing there: [{string.Join(", ", missing)}], found elsewhere: [{string.Join(", ", misplaced)}]. Why it matters: a " +
                "module shows its layers only when the folder matches the namespace. How to fix: move the file with git mv into " +
                $"{folder}, leaving no copy behind.");
        }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Application
{
    internal sealed class AWellPlacedService
    {
        public int Answer => 42;
    }

    internal sealed class AFlatDependencyService
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Application.Dependencies
{
    internal sealed class AWellPlacedDependencyService
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Services.Implementation.WorkItems
{
    internal sealed class ALeftBehindService
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Neighbours.Storage
{
    internal sealed class AStorageType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.ReachingStorage
{
    internal static class AnApplicationTypeReachingStorage
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Neighbours.Storage.AStorageType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Clean
{
    internal static class AnApplicationTypeReachingNothingForbidden
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.WorkItemsFixtures.ApplicationLayout.Application.AWellPlacedService().Answer;
    }
}
