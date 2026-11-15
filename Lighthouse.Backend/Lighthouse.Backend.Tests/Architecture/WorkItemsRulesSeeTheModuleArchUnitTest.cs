using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.WorkItems.Application.WorkItemsFixtures;
using Lighthouse.Backend.Modules.WorkItemsFixtures.RulesSeeTheModule;
using Lighthouse.Backend.WorkItemsRulesFixtures.Connectors;
using Lighthouse.Backend.WorkItemsRulesFixtures.Forecast;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Work Items is about to move from Services into Modules/WorkItems. Every architecture rule that finds Work Items by
    /// its namespace, folder or file must already see the new place, or the move quietly switches it off: a rule that looks
    /// only at the old place passes because it no longer finds anything there, or fails as if the file had been deleted.
    /// The real code holds no type in the new place yet, so each rule's own Work Items key is read from the rule and run
    /// over made-up types placed below Lighthouse.Backend.Modules.WorkItems and made-up paths under Modules/WorkItems. The
    /// made-up offender must be named; a compliant twin beside it, and a namesake just outside Modules.WorkItems, must not be.
    /// </summary>
    [TestFixture]
    public class WorkItemsRulesSeeTheModuleArchUnitTest
    {
        private const string AMadeUpFileName = "AMadeUpDependencyFile.cs";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsRulesSeeTheModuleArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void AConnectorReachingATypeUnderTheModule_IsCaughtByTheConnectorLeafRule()
        {
            var workItems = GivenTheWorkItemsPatternOf(typeof(ModuleBoundariesArchUnitTest), "WorkItemsSyncPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(AConnectorReachingTheWorkItemsModule), typeof(AConnectorReachingANamesake), typeof(AConnectorReadingValuesOnly)],
                inSubject: _ => true,
                forbidden: ResidesIn(workItems));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AConnectorReachingTheWorkItemsModule),
                [typeof(AConnectorReachingANamesake), typeof(AConnectorReadingValuesOnly)],
                "The module map's Work Items namespaces",
                "a connector is a leaf input adapter; once the work item services live in Modules/WorkItems, a connector reaching into them would pass the leaf rule unseen, and a pattern without its end anchor would treat Modules.WorkItemsFixtures as Work Items.",
                @"add |^Lighthouse\.Backend\.Modules\.WorkItems(\.|$) to WorkItemsSyncPattern on the module map, and leave Dependencies and Parents out of it, because the connectors call the selectors that stay there.");
        }

        [Test]
        public void ADependencyTypeUnderTheModuleReachingTheForecast_IsCaughtByTheForecastIsolationRule()
        {
            var dependencies = GivenTheWorkItemsPatternOf(typeof(DependencySingleDecisionArchUnitTest), "TheNamespaceThisEpicOwns");

            var named = WhenTheRuleRunsOver(
                [typeof(AMadeUpDependencyStepReachingTheForecast), typeof(AMadeUpDependencyStepReadingValuesOnly), typeof(ANamesakeReachingTheForecast)],
                inSubject: typeNamespace => Matches(typeNamespace, dependencies),
                forbidden: IsOneOf(typeof(global::Lighthouse.Backend.Modules.Forecasting.Application.ForecastService)));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AMadeUpDependencyStepReachingTheForecast),
                [typeof(AMadeUpDependencyStepReadingValuesOnly), typeof(ANamesakeReachingTheForecast)],
                "The namespaces the dependency rules own",
                "what a Feature waits on is decided from facts a caller already holds; once the dependency decision lives in Modules/WorkItems, a decision reaching into the simulation would pass unseen.",
                @"add |^Lighthouse\.Backend\.Modules\.WorkItems(\.|$) to TheNamespaceThisEpicOwns.");
        }

        [Test]
        public void TheForecastReachingADependencyTypeUnderTheModule_IsCaughtByTheForecastIsolationRule()
        {
            var dependencies = GivenTheWorkItemsPatternOf(typeof(DependencySingleDecisionArchUnitTest), "TheNamespaceThisEpicOwns");

            var named = WhenTheRuleRunsOver(
                [typeof(AForecastStepReachingTheWorkItemsModule), typeof(AForecastStepReachingANamesake)],
                inSubject: _ => true,
                forbidden: ResidesIn(dependencies));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AForecastStepReachingTheWorkItemsModule),
                [typeof(AForecastStepReachingANamesake)],
                "The namespaces the forecast may not reach",
                "the forecast asks one port and reads one plain answer; once the decision, the circle walk and the reconciler live in Modules/WorkItems, a forecast reaching them would work out a second answer unseen.",
                @"add |^Lighthouse\.Backend\.Modules\.WorkItems(\.|$) to TheNamespaceThisEpicOwns.");
        }

        [TestCase("WhatThisEpicAddedToTheBackend", "")]
        [TestCase("TheOnlyPlaceThatMayRecordTheSource", "Lighthouse.Backend/")]
        public void TheDependencyFolderScans_TakeInAFolderUnderTheModuleBesideTheOldOne(string member, string rootOfTheKey)
        {
            var folders = GivenTheFoldersOf(typeof(DependencySingleDecisionArchUnitTest), member);

            var admitted = WhenTheFoldersJudge(
                folders,
                $"{rootOfTheKey}Services/Implementation/Dependencies/{AMadeUpFileName}",
                $"{rootOfTheKey}Modules/WorkItemsFixtures/Application/{AMadeUpFileName}");

            ThenAFolderUnderTheModuleIsTakenInAndTheOldFolderStays(member, rootOfTheKey, folders, admitted);
        }

        [Test]
        public void TheOneDecisionFiles_AreFoundByTheirNameWhereverTheyLive()
        {
            var reconciler = GivenTheWorkItemsPatternOf(typeof(DependencySingleDecisionArchUnitTest), "TheOnlyFileThatMayChangeIt");
            var honourPolicy = GivenTheWorkItemsPatternOf(typeof(DependencySingleDecisionArchUnitTest), "TheOnlyFileThatMayReadTheLicenceFlag");
            var decision = GivenTheWorkItemsPatternOf(typeof(DependencySingleDecisionArchUnitTest), "TheOnlyFileThatMayAskWhetherTheInstanceHasPaid");

            var productionFileNames = WhenTheProductionSourceIsListedByFileName();

            ThenEachKeyFindsExactlyOneFileByItsNameAlone("The one-decision rules", productionFileNames, reconciler, honourPolicy, decision);
        }

        [Test]
        public void TheWorkItemServiceHistoryCutoffs_FindTheirFileByItsNameWhereverItLives()
        {
            var cutoffFiles = GivenTheCutoffFilesOf(typeof(CalendarDayAnchorSeamArchUnitTest), "TrackerHistoryCutoffs");

            var productionFileNames = WhenTheProductionSourceIsListedByFileName();

            ThenTheWorkItemServiceCutoffsNameTheFileAlone(cutoffFiles, productionFileNames);
        }

        private static string GivenTheWorkItemsPatternOf(Type rule, string member)
            => GivenTheKeyOf(rule, member) as string ?? throw new AssertionException(
                $"{rule.Name}.{member} is not a single namespace pattern or file name. Why it matters: this check runs the rule's own " +
                "Work Items key over a made-up type or file in Modules/WorkItems. How to fix: keep the key a string constant or field.");

        private static List<string> GivenTheFoldersOf(Type rule, string member)
            => GivenTheKeyOf(rule, member) switch
            {
                string folder => [folder],
                IEnumerable<string> folders => [.. folders],
                _ => throw new AssertionException(
                    $"{rule.Name}.{member} is neither a folder nor a list of folders. Why it matters: this check runs the rule's own " +
                    "folders over a made-up file under Modules/WorkItems. How to fix: keep the folders as a string or a string array of path prefixes."),
            };

        private static List<string> GivenTheCutoffFilesOf(Type rule, string member)
        {
            var entries = GivenTheKeyOf(rule, member) as IEnumerable ?? throw new AssertionException(
                $"{rule.Name}.{member} is not a list. How to fix: keep the cutoffs a list of entries that each carry a RelativePath.");

            return [.. entries.Cast<object>().Select(entry =>
                entry.GetType().GetProperty("RelativePath")?.GetValue(entry) as string ?? throw new AssertionException(
                    $"A {rule.Name} cutoff entry carries no RelativePath. Why it matters: this check reads where each cutoff finds its " +
                    "file. How to fix: keep the file on the entry's RelativePath."))];
        }

        private static object GivenTheKeyOf(Type rule, string member)
        {
            var field = rule.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var key = field is { IsLiteral: true } ? field.GetRawConstantValue() : field?.GetValue(null);

            Assert.That(
                key,
                Is.Not.Null,
                $"{rule.Name} has no {member} that says where Work Items lives. Why it matters: this check reads the rule's own key " +
                "and runs it over a made-up type or file in Modules/WorkItems; without the key there is nothing to show the rule sees " +
                $"the new place. How to fix: keep the rule's Work Items namespace, folder or file in a static field named {member} " +
                "and let the rule use it.");

            return key!;
        }

        private static List<string> WhenTheRuleRunsOver(IReadOnlyCollection<Type> madeUpTypes, Func<string, bool> inSubject, Func<TypeReference, bool> forbidden)
            => [.. madeUpTypes
                .Where(madeUpType => inSubject(madeUpType.Namespace!))
                .Where(madeUpType => IlDependencies.DependentsIn(TestModule.Value, $"^{Regex.Escape(madeUpType.Namespace!)}$", forbidden)
                    .Any(dependent => dependent.TypeName == madeUpType.FullName))
                .Select(madeUpType => madeUpType.FullName!)];

        private static List<string> WhenTheFoldersJudge(List<string> folders, params string[] madeUpPaths)
            => [.. madeUpPaths.Where(path => folders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))];

        private static List<string> WhenTheProductionSourceIsListedByFileName()
            => [.. ProductionSource.Files().Select(file => Path.GetFileName(file.RelativePath))];

        private static void ThenTheRuleNamesTheOffenderAndNotItsTwins(List<string> named, Type offender, Type[] twins, string key, string why, string how)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    named,
                    Does.Contain(offender.FullName),
                    $"{key} did not catch {offender.FullName}. Named: [{string.Join(", ", named)}]. Why it matters: {why} How to fix: {how}");

                foreach (var twin in twins)
                {
                    Assert.That(
                        named,
                        Does.Not.Contain(twin.FullName),
                        $"{key} caught {twin.FullName}, which does only what the rule allows or sits outside Modules.WorkItems. Why it " +
                        $"matters: a key that catches everything proves nothing, and a rule that fails on compliant code gets switched off. How to fix: {how}");
                }
            }
        }

        private static void ThenAFolderUnderTheModuleIsTakenInAndTheOldFolderStays(string member, string rootOfTheKey, List<string> folders, List<string> admitted)
        {
            var moduleFolder = $"{rootOfTheKey}Modules/WorkItems/";
            var oldFile = $"{rootOfTheKey}Services/Implementation/Dependencies/{AMadeUpFileName}";
            var namesakeFile = $"{rootOfTheKey}Modules/WorkItemsFixtures/Application/{AMadeUpFileName}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    folders,
                    Has.Some.StartsWith(moduleFolder),
                    $"{member} takes in no folder under {moduleFolder}. Folders: [{string.Join(", ", folders)}]. Why it matters: once the " +
                    "dependency files move into Modules/WorkItems the scan stops reading them, and passes because it reads nothing, or " +
                    $"reports the folder as moved or deleted. How to fix: make {member} the old folder or the module folder the dependency " +
                    "files move to, as path prefixes ending in a slash.");

                Assert.That(
                    admitted,
                    Does.Contain(oldFile),
                    $"{member} no longer takes in {oldFile}. Why it matters: the dependency selectors stay in the old folder, so the scan " +
                    "must keep reading it beside the module. How to fix: keep the old folder in the list.");

                Assert.That(
                    admitted,
                    Does.Not.Contain(namesakeFile),
                    $"{member} takes in {namesakeFile}, which sits outside Modules/WorkItems. Why it matters: a folder without its closing " +
                    "slash reads every folder whose name starts the same. How to fix: end each folder with a slash.");
            }
        }

        private static void ThenEachKeyFindsExactlyOneFileByItsNameAlone(string rules, List<string> productionFileNames, params string[] keys)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var key in keys)
                {
                    Assert.That(
                        key,
                        Is.EqualTo(Path.GetFileName(key)),
                        $"{rules} find {key} by its folder. Why it matters: when the file moves to Modules/WorkItems the rule reports it as " +
                        "moved or deleted, or stops reading it. How to fix: keep only the file name and find the file by that name under " +
                        "the production source, as the fast-with-many-Features rules do.");
                    Assert.That(
                        productionFileNames.Count(fileName => fileName == Path.GetFileName(key)),
                        Is.EqualTo(1),
                        $"The production source should hold exactly one file named {Path.GetFileName(key)}. Why it matters: a rule that " +
                        "finds its file by name alone would read the wrong one, or none. How to fix: give the file a name no other " +
                        "production file has.");
                }
            }
        }

        private static void ThenTheWorkItemServiceCutoffsNameTheFileAlone(List<string> cutoffFiles, List<string> productionFileNames)
        {
            const string workItemService = "WorkItemService.cs";
            var workItemServiceCutoffs = cutoffFiles.Where(file => Path.GetFileName(file) == workItemService).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    workItemServiceCutoffs,
                    Is.EqualTo(new[] { workItemService, workItemService }),
                    $"The history cutoffs of {workItemService} are listed as [{string.Join(", ", workItemServiceCutoffs)}]. Why it matters: " +
                    "both percentile history window lines must keep their stated reason, and a cutoff that names the file by its folder " +
                    "reports the file as gone once it moves to Modules/WorkItems. How to fix: list both window cutoffs by the file name " +
                    $"{workItemService} alone and find the file by that name under the production source, as the fast-with-many-Features rules do.");
                Assert.That(
                    productionFileNames.Count(fileName => fileName == workItemService),
                    Is.EqualTo(1),
                    $"The production source should hold exactly one file named {workItemService}. Why it matters: a cutoff found by name " +
                    "alone would read the wrong file, or none. How to fix: give the file a name no other production file has.");
            }
        }

        private static Func<TypeReference, bool> ResidesIn(string namespacePattern)
            => type => Matches(IlDependencies.NamespaceOf(type), namespacePattern);

        private static Func<TypeReference, bool> IsOneOf(params Type[] types)
            => type => types.Any(candidate => candidate.FullName == type.FullName.Replace('/', '+'));

        private static bool Matches(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}

namespace Lighthouse.Backend.Modules.WorkItems.Application.WorkItemsFixtures
{
    internal static class AMadeUpRefreshStep
    {
        public static int Refresh(int items) => items + 1;
    }

    internal static class AMadeUpDependencyStepReachingTheForecast
    {
        public static bool CanAsk(global::Lighthouse.Backend.Modules.Forecasting.Application.ForecastService forecast) => forecast is not null;
    }

    internal static class AMadeUpDependencyStepReadingValuesOnly
    {
        public static int Decide(int waits) => global::Lighthouse.Backend.WorkItemsRulesFixtures.Values.AValue.Of(waits);
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.RulesSeeTheModule
{
    internal static class ANamesakeRefreshStep
    {
        public static int Refresh(int items) => items + 2;
    }

    internal static class ANamesakeReachingTheForecast
    {
        public static bool CanAsk(global::Lighthouse.Backend.Modules.Forecasting.Application.ForecastService forecast) => forecast is not null;
    }
}

namespace Lighthouse.Backend.WorkItemsRulesFixtures.Values
{
    internal static class AValue
    {
        public static int Of(int count) => count;
    }
}

namespace Lighthouse.Backend.WorkItemsRulesFixtures.Connectors
{
    internal static class AConnectorReachingTheWorkItemsModule
    {
        public static int Fetch(int items) => global::Lighthouse.Backend.Modules.WorkItems.Application.WorkItemsFixtures.AMadeUpRefreshStep.Refresh(items);
    }

    internal static class AConnectorReachingANamesake
    {
        public static int Fetch(int items) => global::Lighthouse.Backend.Modules.WorkItemsFixtures.RulesSeeTheModule.ANamesakeRefreshStep.Refresh(items);
    }

    internal static class AConnectorReadingValuesOnly
    {
        public static int Fetch(int items) => global::Lighthouse.Backend.WorkItemsRulesFixtures.Values.AValue.Of(items);
    }
}

namespace Lighthouse.Backend.WorkItemsRulesFixtures.Forecast
{
    internal static class AForecastStepReachingTheWorkItemsModule
    {
        public static int Run(int items) => global::Lighthouse.Backend.Modules.WorkItems.Application.WorkItemsFixtures.AMadeUpRefreshStep.Refresh(items);
    }

    internal static class AForecastStepReachingANamesake
    {
        public static int Run(int items) => global::Lighthouse.Backend.Modules.WorkItemsFixtures.RulesSeeTheModule.ANamesakeRefreshStep.Refresh(items);
    }
}
