using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Clean;
using Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingAdapters;
using Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingApplication;
using Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingServices;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The pure part of Work Items, sync mode, the fetch fingerprint, the rule evaluator and the dependency policies with
    /// the plain values they read, lives in the module's Domain (the dependency ones in its Dependencies folder) and reaches
    /// only the shared kernel and the base library. The types are named as strings and read from the compiled production
    /// assembly, so this class compiles before and after the move, and a type that has not moved yet is a failure that names it.
    /// The Domain rule also runs on made-up Domain types: each one that reaches a forbidden layer must be reported, and a
    /// compliant twin next to it must not be. The CI test selection must keep following the fetch fingerprint to its new file.
    /// </summary>
    [TestFixture]
    public class WorkItemsDomainLayoutArchUnitTest
    {
        private const string WorkItemsDomainNamespace = "Lighthouse.Backend.Modules.WorkItems.Domain";

        private const string WorkItemsDependenciesDomainNamespace = "Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies";

        private const string WorkItemsDomain = @"^Lighthouse\.Backend\.Modules\.WorkItems\.Domain(\.|$)";

        private const string WorkItemsDomainFolder = "Modules/WorkItems/Domain";

        private const string WorkItemsDependenciesDomainFolder = "Modules/WorkItems/Domain/Dependencies";

        private const string FixtureDomains = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.DomainLayout\.";

        private const string ADomainThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.DomainLayout\.NothingLivesHere(\.|$)";

        private const string FetchFingerprintInItsNewHome = "Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/Domain/FetchFingerprint.cs";

        private const string FetchFingerprintInItsOldHome = "Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/FetchFingerprint.cs";

        // The two generic ones carry their arity, as the compiled code names them.
        private static readonly string[] DomainTypes =
        [
            "SyncModeResolver",
            "FetchFingerprint",
            "RuleEvaluator`1",
            "IRuleEvaluator`1",
        ];

        private static readonly string[] DependencyDomainTypes =
        [
            "DependencyHonourPolicy",
            "IDependencyHonourPolicy",
            "DependencyHonourInput",
            "FeatureDependencyFacts",
            "DependencyCycleDetector",
            "DependencyLoops",
            "DependencyLoop",
            "DependencyFacts",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsDomainLayoutArchUnitTest).Assembly));

        public enum ForbiddenReach
        {
            Services,
            ModuleApplication,
            ModuleAdapters,
        }

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void SyncModeTheFetchFingerprintAndTheRuleEvaluator_LiveInTheModuleDomain_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, WorkItemsDomainNamespace, DomainTypes).Check();
        }

        [Test]
        public void TheDependencyPoliciesAndTheirPlainValues_LiveInTheModuleDomainDependenciesFolder_NotFlatInTheDomain_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, WorkItemsDependenciesDomainNamespace, DependencyDomainTypes).Check();
        }

        [Test]
        public void TheWorkItemsDomainFiles_SitInTheModuleDomainFolders_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(WorkItemsDomainFolder, ["SyncModeResolver.cs", "FetchFingerprint.cs", "RuleEvaluator.cs", "IRuleEvaluator.cs"]);
            ThenEachFileSitsOnlyIn(WorkItemsDependenciesDomainFolder, ["DependencyHonourPolicy.cs", "IDependencyHonourPolicy.cs", "DependencyCycleDetector.cs", "DependencyFacts.cs"]);
        }

        [Test]
        public void TheWorkItemsDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, WorkItemsDomain).Check();
        }

        [TestCase(ForbiddenReach.Services)]
        [TestCase(ForbiddenReach.ModuleApplication)]
        [TestCase(ForbiddenReach.ModuleAdapters)]
        public void AWorkItemsDomainTypeThatReachesAForbiddenLayer_IsReportedAndItsCompliantTwinIsNot(ForbiddenReach reach)
        {
            var (domain, offender, forbiddenTarget) = MadeUpDomainReaching(reach);

            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, domain);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, forbiddenTarget, reach);
        }

        [Test]
        public void AWorkItemsDomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, ADomainThatHoldsNothing);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A Domain rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing " +
                "stays green forever and guards nothing. How to fix: the rule must fail when the Domain it was given holds no type.");

            Assert.That(failure!.Message, Does.Contain(ADomainThatHoldsNothing), "The failure must name the Domain that held nothing, so the reader knows which pattern to correct.");
        }

        [Test]
        public void ACodeChangeToTheFetchFingerprint_StillSelectsTheLiveJiraTests_AtItsNewFile_AndNotAtTheOldOne()
        {
            var selectedAtTheNewFile = WhenTheCiTestSelectionClassifiesForJira(FetchFingerprintInItsNewHome);
            var selectedAtTheOldFile = WhenTheCiTestSelectionClassifiesForJira(FetchFingerprintInItsOldHome);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    selectedAtTheNewFile,
                    Is.EqualTo("true"),
                    $"A change to {FetchFingerprintInItsNewHome} does not select the live Jira suite. Why it matters: the fingerprint decides what the Jira " +
                    "connector fetches, so when its path is stale a change to it ships without the Jira tests running. How to fix: point the Jira " +
                    "pattern in Scripts/test-selection/path-classifier.sh, and its cases in test-path-classifier.sh, at the file's new home.");

                Assert.That(
                    selectedAtTheOldFile,
                    Is.EqualTo("false"),
                    $"The classifier still selects the live Jira suite for {FetchFingerprintInItsOldHome}, a path no file has any more. Why it matters: " +
                    "a pattern kept for a file that moved selects nothing real and hides that the new file is not covered. How to fix: replace the old path with the new one.");
            }
        }

        private static string WhenTheCiTestSelectionClassifiesForJira(string changedPath)
        {
            var repositoryRoot = RepositoryRoot();
            var start = new ProcessStartInfo("bash")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("source Scripts/test-selection/path-classifier.sh && classify_jira \"$1\"");
            start.ArgumentList.Add("classifier");
            start.ArgumentList.Add(changedPath);

            using var process = Process.Start(start)!;
            var answer = process.StandardOutput.ReadToEnd().Trim();
            var errors = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.That(process.ExitCode, Is.Zero, $"The test-selection classifier could not be run: {errors}");
            return answer;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory?.Parent, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the test-selection scripts.");
            return directory!.Parent!.FullName;
        }

        private static (string Domain, string Offender, string ForbiddenTarget) MadeUpDomainReaching(ForbiddenReach reach) => reach switch
        {
            ForbiddenReach.Services => (FixtureDomain("ReachingServices"), typeof(ADomainTypeReachingServices).FullName!, typeof(global::Lighthouse.Backend.Services.WorkItemsFixtures.AServiceType).FullName!),
            ForbiddenReach.ModuleApplication => (FixtureDomain("ReachingApplication"), typeof(ADomainTypeReachingTheModuleApplication).FullName!, typeof(global::Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Application.AnApplicationType).FullName!),
            ForbiddenReach.ModuleAdapters => (FixtureDomain("ReachingAdapters"), typeof(ADomainTypeReachingTheModuleAdapters).FullName!, typeof(global::Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType).FullName!),
            _ => throw new ArgumentOutOfRangeException(nameof(reach)),
        };

        // Each made-up Domain is the offender's namespace together with the compliant twin's.
        private static string FixtureDomain(string offenderNamespace) => $"{FixtureDomains}({offenderNamespace}|Clean)$";

        private static void ThenTheRuleReportsOnlyTheOffender(KernelRuleVerdict verdict, string offender, string forbiddenTarget, ForbiddenReach reach)
        {
            var compliantTwin = typeof(ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary).FullName!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(compliantTwin).And.Contain(offender),
                    $"The rule did not look at both made-up Domain types ({reach}). Why it matters: a twin the rule never looked at " +
                    "proves nothing by passing. How to fix: the rule must examine every type in the Domain namespace it is given.");

                Assert.That(
                    verdict.Offenders.Select(dependent => dependent.TypeName),
                    Is.EqualTo(new[] { offender }),
                    $"Expected exactly {offender} to be reported ({reach}). Why it matters: a Domain type that reaches a forbidden " +
                    "layer must be caught, and a twin that reaches only its own Domain, the Models and the base library must not be, " +
                    "or the rule flags everything. How to fix: the rule allows exactly the Domain itself, the shared kernel, System.* and compiler-made types.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, $"The Domain rule passed with {offender} in it ({reach}).");

            Assert.That(
                failure!.Message,
                Does.Contain(offender).And.Contain(forbiddenTarget),
                "The failure must name both the offending Domain type and the forbidden type it reached, so the reader knows what to undo.");
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

namespace Lighthouse.Backend.Services.WorkItemsFixtures
{
    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Models.WorkItemsFixtures
{
    internal sealed record AKernelValue(int Days);
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Application
{
    internal sealed class AnApplicationType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Adapters
{
    internal sealed class AnAdapterType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingServices
{
    internal static class ADomainTypeReachingServices
    {
        public static int Answer(global::Lighthouse.Backend.Services.WorkItemsFixtures.AServiceType service) => service.Answer;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingApplication
{
    internal static class ADomainTypeReachingTheModuleApplication
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Application.AnApplicationType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.ReachingAdapters
{
    internal static class ADomainTypeReachingTheModuleAdapters
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.DomainLayout.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. It also makes
    /// the compiler write its own helper types, so a rule that mistook those for forbidden types would report it.
    /// </summary>
    internal static class ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary
    {
        private static readonly int[] DaysShown = [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597];

        public static int LongestAbove(IEnumerable<global::Lighthouse.Backend.Models.WorkItemsFixtures.AKernelValue> values, int threshold)
            => values.Where(value => value.Days > threshold).Select(value => value.Days).DefaultIfEmpty(ASiblingInTheSameDomain.Nothing).Max();

        public static IEnumerable<int> Shown()
        {
            foreach (var day in DaysShown)
            {
                yield return Math.Min(day, 1000);
            }
        }
    }

    internal static class ASiblingInTheSameDomain
    {
        public const int Nothing = 0;
    }
}
