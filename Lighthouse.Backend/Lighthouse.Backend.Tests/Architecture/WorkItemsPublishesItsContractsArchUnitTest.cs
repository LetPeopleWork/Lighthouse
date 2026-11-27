using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Work Items publishes what the other contexts may name in a Contracts namespace of its own. The published types are the
    /// entries of the measured map of what each context publishes, taken without the events. Each of them is found once in
    /// the compiled code, in the Contracts namespace of Work Items; none is left in the namespace it had before; and no type
    /// of the production code or the tests outside Work Items names one by its old name.
    /// </summary>
    [TestFixture]
    public class WorkItemsPublishesItsContractsArchUnitTest
    {
        private static readonly string[] ThePublishers = ["WorkItems"];

        private static readonly string[] WhatTheOtherContextsReach =
        [
            "WorkItems:IBlockedItemService",
            "WorkItems:IRefreshLogService",
            "WorkItems:IRuleEvaluator`1",
            "WorkItems:IRuleFieldProvider`1",
            "WorkItems:IWhatTheForecastWaitsFor",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsPublishesItsContractsArchUnitTest).Assembly));

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
        public void WorkItemsPublishesTheTypesTheOthersReach_AndTheListOfThemIsNotEmpty()
        {
            var published = ThePublishedTypes
                .Select(type => $"{type.Area}:{type.Name}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                published,
                Is.EqualTo(WhatTheOtherContextsReach),
                $"The published types of Work Items were read as [{string.Join(" | ", published)}]. Expected the five Work Items types that the other contexts reach. Why it matters: an empty or shrunk list lets every check below pass without looking at anything. How to fix: keep the map of what each context publishes complete for Work Items.");
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
                $"These published types are not found once, in the Contracts namespace of Work Items: {string.Join("; ", misplaced)}. Why it matters: the other contexts may name a context only through its Contracts, and a type kept in its old place, kept in both places or put into another context's Contracts leaves them naming something that is not published. How to fix: move each type into Modules/<Context>/Contracts with the namespace of that folder, and leave no copy behind.");
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
        public void NoTypeOutsideWorkItemsNamesAPublishedTypeByItsOldName()
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
                $"These types name a published type of Work Items by its old name: {string.Join("; ", naming)}. Why it matters: the other contexts may reach Work Items only through its Contracts, so every use must follow the move. How to fix: change the using line, or the full name written in a string, to the Contracts namespace of Work Items.");
        }
    }
}
