using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Organisation set-up publishes what the other contexts may name in a Contracts namespace of its own, and its open host
    /// narrows from its Application and Domain to that Contracts. The published types are the entries of the measured map of what
    /// each context publishes, taken without the events. Each of them is found once in the compiled code, in the Contracts
    /// namespace of Organisation set-up; none is left in the namespace it had before; and no type of the production code or the
    /// tests outside Organisation set-up names one by its old name. The old-name scan reads the compiled code only, so a full
    /// type name written inside a string or a comment leaves no trace it can see. The open host is read from the lists and rule
    /// targets that apply it, and the list of edges the context rule still tolerates must hold no edge into a published type.
    /// </summary>
    [TestFixture]
    public class OrganisationSetupPublishesItsContractsArchUnitTest
    {
        private const string ThePublisher = "OrganisationSetup";

        private static readonly string[] WhatTheOtherContextsReach =
        [
            "OrganisationSetup:IAppSettingService",
            "OrganisationSetup:IBlackoutPeriodService",
            "OrganisationSetup:IFeatureOrdering",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(OrganisationSetupPublishesItsContractsArchUnitTest).Assembly));

        private static IReadOnlyList<MovingType> ThePublishedTypes { get; } =
        [
            .. ContextContractsMembership.Entries
                .Select(entry => entry.Type)
                .Where(type => type.Area == ThePublisher)
                .Where(type => !type.OldNamespace.EndsWith(".Models.Events", StringComparison.Ordinal)),
        ];

        private static string TheSetUpModule => $"{ContextContractsMembership.TheModulesRoot}.{ThePublisher}";

        private static string TheSetUpContracts => ContextContractsMembership.ContractsOf(ThePublisher);

        private static string[] TheSetUpNamespacesBehindTheContracts =>
        [
            TheSetUpModule + ".Domain",
            TheSetUpModule + ".Application",
            TheSetUpModule + ".Application.Connections",
            TheSetUpModule + ".Application.OptionalFeatures",
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
        public void OrganisationSetupPublishesTheTypesTheOthersReach_AndTheListOfThemIsNotEmpty()
        {
            var published = ThePublishedTypes
                .Select(type => $"{type.Area}:{type.Name}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                published,
                Is.EqualTo(WhatTheOtherContextsReach),
                $"The published types of Organisation set-up were read as [{string.Join(" | ", published)}]. Expected the three Organisation set-up types that the other contexts reach. Why it matters: an empty or shrunk list lets every check below pass without looking at anything. How to fix: keep the map of what each context publishes complete for Organisation set-up.");
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
                $"These published types are not found once, in the Contracts namespace of Organisation set-up: {string.Join("; ", misplaced)}. Why it matters: the other contexts may name a context only through its Contracts, and a type kept in its old place, kept in both places or put into another context's Contracts leaves them naming something that is not published. How to fix: move each type into Modules/OrganisationSetup/Contracts with the namespace of that folder, and leave no copy behind.");
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
        public void NoTypeOutsideOrganisationSetupNamesAPublishedTypeByItsOldName()
        {
            var oldNames = ThePublishedTypes.Select(type => type.OldFullName).ToHashSet(StringComparer.Ordinal);

            var insideThePublisher = $@"^{Regex.Escape(TheSetUpModule)}(\.|$)";

            var production = IlDependencies.DependentsIn(ProductionModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')))
                .Where(dependent => !Regex.IsMatch(dependent.TypeName, insideThePublisher, RegexOptions.None, TimeSpan.FromSeconds(1)));

            var tests = IlDependencies.DependentsIn(TestModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')));

            var naming = production
                .Concat(tests)
                .Select(dependent => $"{dependent.TypeName} names {string.Join(", ", dependent.Targets)}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                naming,
                Is.Empty,
                $"These types name a published type of Organisation set-up by its old name: {string.Join("; ", naming)}. Why it matters: the other contexts may reach Organisation set-up only through its Contracts, so every use must follow the move. How to fix: change the using line to the Contracts namespace of Organisation set-up.");
        }

        [Test]
        public void TheOpenHostListAdmitsOnlyTheContractsOfOrganisationSetup()
        {
            var admitted = StaticListOf(typeof(OrganisationSetupIsAnOpenHostArchUnitTest), "SetUpNamespacesOpenToDownstreamModules");

            Assert.That(
                admitted,
                Is.EqualTo(new[] { TheSetUpContracts }),
                $"The namespaces of Organisation set-up that the open-host check treats as open to the downstream contexts are [{string.Join(", ", admitted)}]. Expected only {TheSetUpContracts}. Why it matters: Organisation set-up is an open host through its Contracts, and an Application or Domain namespace left open lets a downstream context name types that are not published. How to fix: list only the Contracts namespace of Organisation set-up as open.");
        }

        [Test]
        public void NoDownstreamRuleReadingOrganisationSetupLetsItsApplicationOrDomainThrough_AndEveryOneLetsItsContractsThrough()
        {
            var problems = TheDownstreamRulesThatReadOrganisationSetupAsForeign()
                .SelectMany(rule => Problems(rule.Description, rule.Target))
                .ToList();

            Assert.That(
                problems,
                Is.Empty,
                $"These outbound rules do not admit Organisation set-up through its Contracts only: {string.Join("; ", problems)}. Why it matters: a downstream context that still reaches Organisation set-up's Application or Domain by right can name types that are not published, and one that cannot reach its Contracts cannot use what is published. How to fix: let each rule's pattern leave out the Contracts namespace of Organisation set-up only, so its Application and Domain are read as another context's code.");
        }

        [Test]
        public void TheListOfToleratedCrossContextEdgesHoldsNoEdgeIntoAPublishedType()
        {
            var tolerated = StaticListOf(typeof(ContextContractsArchUnitTest), "CrossContextEdgesOutsideContracts");
            var oldNames = ThePublishedTypes.Select(type => type.OldFullName).ToList();

            var intoPublishedTypes = tolerated
                .Where(entry => oldNames.Any(name => entry.EndsWith($" -> {name}", StringComparison.Ordinal)))
                .ToList();

            Assert.That(
                intoPublishedTypes,
                Is.Empty,
                $"The list of tolerated cross-context edges still names these edges into a published type of Organisation set-up: {string.Join("; ", intoPublishedTypes)}. Why it matters: once the types are published, an edge into them is the legal way in, and a listed one hides the day the edge goes back to the old place. How to fix: take each of these entries off the list; the list then holds only the edges into code that is not published.");
        }

        // A rule is checked here only when it reads both set-up's Adapters and its Application as foreign. Refinement's forbidden
        // code reads only set-up's Adapters, so it is left out: edges from it into set-up's other code belong to the contracts rule.
        private static List<(string Description, string Target)> TheDownstreamRulesThatReadOrganisationSetupAsForeign()
        {
            var table = StaticValueOf(typeof(OrganisationSetupIsAnOpenHostArchUnitTest), "DownstreamOutboundRules") as System.Collections.IEnumerable;

            Assert.That(table, Is.Not.Null, "The open-host check keeps no table of the downstream outbound rules under the name DownstreamOutboundRules. Why it matters: this check reads the rules from that one table so none can be left out. How to fix: keep the table under that name, or name the new member here.");

            return
            [
                .. table!.Cast<ITuple>()
                    .Select(row => (RuleClass: (Type)row[0]!, Module: (string)row[1]!, Rule: (string)row[2]!))
                    .Select(row => (Description: $"{row.Module} {row.Rule}", Target: StaticValueOf(row.RuleClass, row.Rule) as string ?? string.Empty))
                    .Where(rule => Reads(rule.Target, TheSetUpModule + ".Adapters") && Reads(rule.Target, TheSetUpModule + ".Application")),
            ];
        }

        private static List<string> Problems(string description, string target)
        {
            var admittedThatShouldBeForeign = TheSetUpNamespacesBehindTheContracts.Where(candidate => !Reads(target, candidate)).Select(candidate => $"{description} lets {candidate} through");
            var foreignThatShouldBeAdmitted = Reads(target, TheSetUpContracts) ? [$"{description} reads {TheSetUpContracts} as foreign"] : Array.Empty<string>();

            return [.. admittedThatShouldBeForeign, .. foreignThatShouldBeAdmitted];
        }

        private static bool Reads(string target, string candidate)
            => Regex.IsMatch(candidate, target, RegexOptions.None, TimeSpan.FromSeconds(1));

        private static string[] StaticListOf(Type ruleClass, string member)
        {
            var value = StaticValueOf(ruleClass, member);

            Assert.That(value, Is.Not.Null, $"{ruleClass.Name}.{member} holds no list. Why it matters: this check reads the rule's own list, so without it the narrowing is unobserved. How to fix: keep the list under this name, or name the new member here.");

            return [.. ((System.Collections.IEnumerable)value!).Cast<string>()];
        }

        private static object? StaticValueOf(Type ruleClass, string member)
        {
            const BindingFlags anyStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            var field = ruleClass.GetField(member, anyStatic);
            if (field is not null)
            {
                return field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
            }

            var property = ruleClass.GetProperty(member, anyStatic);
            Assert.That(property, Is.Not.Null, $"{ruleClass.Name} has no member {member}. Why it matters: this check reads the rule's own target or list, so a renamed member leaves the narrowing unobserved. How to fix: name the member the rule uses here.");

            return property!.GetValue(null);
        }
    }
}
