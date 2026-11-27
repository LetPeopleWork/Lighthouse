using System.Diagnostics;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Tracker integration publishes what the other contexts may name in a Contracts namespace of its own. The published types are
    /// the entries of the measured map of what each context publishes, taken without the events. Each of them is found once in the
    /// compiled code, in the Contracts namespace of Tracker integration; none is left in the namespace it had before; and no type of
    /// the production code or the tests outside Tracker integration names one by its old name. The old-name scan reads the compiled
    /// code, so it sees every reference that leaves a trace there; the CSV option names are constants the compiler copies into their
    /// users, which leave no such trace, so their home is checked by name instead. A change in the new Contracts folder must still
    /// make CI run every connector suite, because the connector port lives there.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationPublishesItsContractsArchUnitTest
    {
        private const string TheConnectorPortInItsNewHome = "Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Contracts/IWorkTrackingConnector.cs";

        private const string TheCsvOptionNames = "CsvWorkTrackingOptionNames";

        private static readonly string[] ThePublishers = ["TrackerIntegration"];

        private static readonly string[] WhatTheOtherContextsReach =
        [
            "TrackerIntegration:IDeliveryForecastPublisher",
            "TrackerIntegration:IDeliverySourceProvider",
            "TrackerIntegration:IDescriptionBlockMerger",
            "TrackerIntegration:IFeatureSource",
            "TrackerIntegration:IIncrementalSource",
            "TrackerIntegration:IWorkTrackingConnector",
            "TrackerIntegration:IWorkTrackingConnectorFactory",
            "TrackerIntegration:IWriteBackTarget",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationPublishesItsContractsArchUnitTest).Assembly));

        private static IReadOnlyList<MovingType> ThePublishedTypes { get; } =
        [
            .. ContextContractsMembership.Entries
                .Select(entry => entry.Type)
                .Where(type => ThePublishers.Contains(type.Area, StringComparer.Ordinal))
                .Where(type => !type.OldNamespace.EndsWith(".Models.Events", StringComparison.Ordinal)),
        ];

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
        public void TrackerIntegrationPublishesTheTypesTheOthersReach_AndTheListOfThemIsNotEmpty()
        {
            var published = ThePublishedTypes
                .Select(type => $"{type.Area}:{type.Name}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                published,
                Is.EqualTo(WhatTheOtherContextsReach),
                $"The published types of Tracker integration were read as [{string.Join(" | ", published)}]. Expected the eight Tracker integration types that the other contexts reach. Why it matters: an empty or shrunk list lets every check below pass without looking at anything. How to fix: keep the map of what each context publishes complete for Tracker integration.");
        }

        [Test]
        public void EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext()
        {
            var module = ProductionModule.Value;

            var misplaced = ThePublishedTypes
                .Select(type => (Type: type, Found: module.Types.Where(candidate => candidate.Name == type.Name).Select(candidate => candidate.FullName).Order(StringComparer.Ordinal).ToList()))
                .Where(each => !each.Found.SequenceEqual([each.Type.NewFullName]))
                .Select(each => $"{each.Type.Name} found as [{string.Join(", ", each.Found)}], expected only {each.Type.NewFullName}")
                .ToList();

            Assert.That(
                misplaced,
                Is.Empty,
                $"These published types are not found once, in the Contracts namespace of Tracker integration: {string.Join("; ", misplaced)}. Why it matters: the other contexts may name a context only through its Contracts, and a type kept in its old place, kept in both places or put into another context's Contracts leaves them naming something that is not published. How to fix: move each type into Modules/<Context>/Contracts with the namespace of that folder, and leave no copy behind.");
        }

        [Test]
        public void NoPublishedTypeIsLeftInTheNamespaceItHadBefore()
        {
            var module = ProductionModule.Value;

            var left = ThePublishedTypes
                .Where(type => module.Types.Any(candidate => candidate.Namespace == type.OldNamespace && candidate.Name == type.Name))
                .Select(type => type.OldFullName)
                .ToList();

            Assert.That(
                left,
                Is.Empty,
                $"These published types still live in their old namespace: {string.Join(", ", left)}. Why it matters: a type in its old place is reachable without going through the Contracts of its context. How to fix: move the type into the Contracts namespace of its context.");
        }

        [Test]
        public void NoTypeOutsideTrackerIntegrationNamesAPublishedTypeByItsOldName()
        {
            var oldNames = ThePublishedTypes.Select(type => type.OldFullName).ToHashSet(StringComparer.Ordinal);

            var insideThePublishers = $@"^{Regex.Escape(ContextContractsMembership.TheModulesRoot)}\.({string.Join("|", ThePublishers)})(\.|$)";

            var production = IlDependencies.DependentsIn(ProductionModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')))
                .Where(dependent => !Regex.IsMatch(dependent.TypeName, insideThePublishers, RegexOptions.None, TimeSpan.FromSeconds(1)));

            var tests = IlDependencies.DependentsIn(TestModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')));

            var naming = production
                .Concat(tests)
                .Select(dependent => $"{dependent.TypeName} names {string.Join(", ", dependent.Targets)}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                naming,
                Is.Empty,
                $"These types name a published type of Tracker integration by its old name: {string.Join("; ", naming)}. Why it matters: the other contexts may reach Tracker integration only through its Contracts, so every use must follow the move. How to fix: change the using line, or the full name written in a string, to the Contracts namespace of Tracker integration.");
        }

        [Test]
        public void TheCsvOptionNamesLiveOnlyInTheContractsOfTrackerIntegration()
        {
            var expected = string.Join('.', ContextContractsMembership.ContractsOf("TrackerIntegration"), TheCsvOptionNames);

            var found = typeof(LighthouseAppContext).Assembly.GetTypes()
                .Where(type => type.Name == TheCsvOptionNames)
                .Select(type => type.FullName)
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                found,
                Is.EqualTo(new[] { expected }),
                $"The CSV option names were found as [{string.Join(", ", found)}], expected only {expected}. Why it matters: Flow metrics names these option names, and since the compiler copies constants into their users, no check of the compiled references can see that it reaches past the Contracts of Tracker integration. How to fix: move the class into Modules/TrackerIntegration/Contracts with the namespace of that folder, and leave no copy in the CSV connector's folder.");
        }

        [Test]
        public void AChangeToTheConnectorPortInItsContractsHome_StillMakesCiRunEveryConnectorSuite()
        {
            var selected = WhenTheCiTestSelectionClassifiesAsShared(TheConnectorPortInItsNewHome);

            Assert.That(
                selected,
                Is.EqualTo("true"),
                $"A change to {TheConnectorPortInItsNewHome} is not classified as shared. Why it matters: every connector implements this port, so a change to it that does not run every connector suite can break a connector unseen. How to fix: let the shared pattern in Scripts/test-selection/path-classifier.sh select Tracker integration's Contracts folder beside its Application and Domain, and add the case to test-path-classifier.sh.");
        }

        private static string WhenTheCiTestSelectionClassifiesAsShared(string changedPath)
        {
            var start = new ProcessStartInfo("bash")
            {
                WorkingDirectory = RepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("source Scripts/test-selection/path-classifier.sh && classify_shared \"$1\"");
            start.ArgumentList.Add("classifier");
            start.ArgumentList.Add(changedPath);

            Process? process = null;
            try
            {
                process = Process.Start(start);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                Assert.Fail($"The test-selection classifier needs bash on the machine, and it could not be started: {exception.Message}. Why it matters: without it the test cannot tell whether the connector suites are still selected. How to fix: install bash and put it on the PATH.");
            }

            using var started = process!;
            var outputRead = started.StandardOutput.ReadToEndAsync();
            var errorsRead = started.StandardError.ReadToEndAsync();
            Task.WaitAll(outputRead, errorsRead);
            started.WaitForExit();

            Assert.That(started.ExitCode, Is.Zero, $"The test-selection classifier could not be run: {errorsRead.Result}");
            return outputRead.Result.Trim();
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
    }
}
