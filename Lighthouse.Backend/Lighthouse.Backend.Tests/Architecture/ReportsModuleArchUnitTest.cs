using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ReportsModuleArchUnitTest
    {
        private const string ApiLayerPattern = @"^Lighthouse\.Backend\.API($|\..*)";

        private const string CompositionRootPattern = @"^Lighthouse\.Backend\.Program($|\+.*)";

        private const string ReportModelsPattern = @"^Lighthouse\.Backend\.Models\.Reports($|\..*)";

        private const string RepositoryPortPattern = @"^Lighthouse\.Backend\.Services\.Interfaces\.Repositories\.IRepository`1$";

        private const string OwnersBaselineMember = "ProcessBehaviourChartBaseline";

        private static readonly string[] WritingMemberPrefixes = ["Add", "Insert", "Create", "Update", "Upsert", "Remove", "Delete", "Save"];

        private static readonly string[] ForbiddenReadMemberNames = ["then", "now"];

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void Nothing_but_the_API_and_the_composition_root_depends_on_the_Reports_module()
        {
            // The module's own records and its one repository sit outside its namespaces but belong to it.
            Types().That().DoNotResideInNamespaceMatching(ModuleBoundariesArchUnitTest.ReportsPattern)
                .And().DoNotResideInNamespaceMatching(ReportModelsPattern)
                .And().DoNotHaveFullNameMatching(NameAndNestedTypesOf(typeof(ReportRepository)))
                .And().DoNotResideInNamespaceMatching(ApiLayerPattern)
                .And().DoNotHaveFullNameMatching(CompositionRootPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.ReportsPattern))
                .Because("reports sit on top of Teams, Portfolios and their metrics; anything below reaching up into them would tie the metrics to a report screen.")
                .Check(Architecture);
        }

        // A report freezes what the Metrics tab showed; it never takes over the chart's own baseline or its
        // limits, so changing the chart's baseline cannot change a report and the other way round. A report
        // computes no limits of its own either, so nothing in it can count points beyond them.
        [Test]
        public void The_Reports_module_does_not_use_the_process_behaviour_chart_or_its_baseline()
        {
            Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.ReportsPattern)
                .Or().ResideInNamespaceMatching(ReportModelsPattern)
                .Should().NotDependOnAny(typeof(ProcessBehaviourChart), typeof(XmRResult), typeof(XmRCalculator), typeof(BaselineValidationService))
                .Because("a report keeps its own windows and its own rules; the chart's baseline answers a different question.")
                .Check(Architecture);
        }

        // Property reads compile to calls, so the owner's baseline dates show up as call dependencies.
        [Test]
        public void The_Reports_module_does_not_read_the_owners_chart_baseline()
        {
            var reads = ReportsModuleTypes()
                .SelectMany(type => type.Dependencies.OfType<MethodCallDependency>())
                .Where(call => call.TargetMember.Name.Contains(OwnersBaselineMember, StringComparison.Ordinal))
                .Select(call => $"{call.Origin.FullName} calls {call.TargetMember.DeclaringType.FullName}.{call.TargetMember.Name}")
                .Distinct()
                .ToList();

            Assert.That(reads, Is.Empty, "a report's Then window is its own; the chart's baseline dates must not leak into it");
        }

        [Test]
        public void Every_report_metric_reads_through_the_series_port()
        {
            Classes().That().ImplementInterface(typeof(IReportMetric))
                .Should().DependOnAny(typeof(IReportMetricSeries))
                .AndShould().NotDependOnAny(typeof(ITeamMetricsService), typeof(IPortfolioMetricsService))
                .Because("a metric that reads the metrics services directly could not be tested with a faked series, and would bypass the one place Team and Portfolio differ.")
                .Check(Architecture);
        }

        [Test]
        public void Only_the_series_adapters_reach_the_metrics_services()
        {
            var adapters = Implementations(typeof(IReportMetricSeries));
            var others = Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.ReportsPattern);
            foreach (var adapter in adapters)
            {
                others = others.And().DoNotHaveFullNameMatching(NameAndNestedTypesOf(adapter));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(adapters, Is.Not.Empty, "the rule exempts the series adapters; without one it guards nothing");
                others.Should().NotDependOnAny(typeof(ITeamMetricsService), typeof(IPortfolioMetricsService))
                    .Because("the series adapters are the one seam between reports and the metrics services.")
                    .Check(Architecture);
            }
        }

        [Test]
        public void Reading_reports_goes_through_the_read_port()
        {
            Types().That().HaveFullName(typeof(ReportQueries).FullName!)
                .Should().DependOnAny(typeof(IReportReader))
                .Because("reports are read through the read-only port and nothing else.")
                .Check(Architecture);
        }

        [Test]
        public void Reading_reports_and_the_templates_they_read_through_can_never_write()
        {
            var subjects = Types().That().HaveFullNameMatching(NameAndNestedTypesOf(typeof(ReportQueries)));
            foreach (var template in Implementations(typeof(IReportTemplate)))
            {
                subjects = subjects.Or().HaveFullNameMatching(NameAndNestedTypesOf(template));
            }

            subjects.Should().NotDependOnAny(typeof(IReportWriter), typeof(LighthouseAppContext))
                .AndShould().NotDependOnAny(Types().That().HaveFullNameMatching(RepositoryPortPattern))
                .Because("opening a report is a read; a writer, a repository or the database context on the read side would let it change what it shows.")
                .Check(Architecture);
        }

        [Test]
        public void A_template_does_not_read_stored_reports()
        {
            var templates = Implementations(typeof(IReportTemplate));
            var subjects = Types().That().HaveFullNameMatching(NameAndNestedTypesOf(templates[0]));
            foreach (var template in templates.Skip(1))
            {
                subjects = subjects.Or().HaveFullNameMatching(NameAndNestedTypesOf(template));
            }

            subjects.Should().NotDependOnAny(typeof(IReportReader))
                .Because("a template is handed the report it reads; fetching reports is the query side's job.")
                .Check(Architecture);
        }

        [TestCase(typeof(IReportReader))]
        [TestCase(typeof(IReportOwnerLookup))]
        public void A_read_port_offers_no_way_to_write(Type port)
        {
            var writing = port.GetMembers()
                .Select(member => member.Name)
                .Where(name => WritingMemberPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToList();

            Assert.That(writing, Is.Empty, $"{port.Name} is read-only; a member that adds, updates, removes or saves would let the read side write");
        }

        // "then" is a reserved name in JavaScript: an object carrying it is treated as a promise and never arrives.
        [Test]
        public void No_part_of_a_report_read_is_named_then_or_now()
        {
            var offending = ReadShapes()
                .SelectMany(shape => shape.GetProperties().Select(property => (shape, property)))
                .Where(pair => SerialisedNamesOf(pair.property).Any(name => ForbiddenReadMemberNames.Contains(name, StringComparer.OrdinalIgnoreCase)))
                .Select(pair => $"{pair.shape.Name}.{pair.property.Name}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadShapes().Select(shape => shape.Name), Does.Contain(nameof(ReportRead)),
                    "the rule finds the read shapes from the query port; without the report read it guards nothing");
                Assert.That(offending, Is.Empty);
            }
        }

        [Test]
        public void Only_the_composition_root_names_the_report_repository()
        {
            Types().That().DoNotHaveFullNameMatching(CompositionRootPattern)
                .And().DoNotHaveFullNameMatching(NameAndNestedTypesOf(typeof(ReportRepository)))
                .Should().NotDependOnAny(typeof(ReportRepository))
                .Because("callers must reach reports through the read or the write port, never past them to the repository's write members.")
                .Check(Architecture);
        }

        [TestCase(typeof(ReportWindowPolicy))]
        public void A_report_policy_is_handed_every_input(Type policy)
        {
            Types().That().HaveFullNameMatching(NameAndNestedTypesOf(policy))
                .Should().NotDependOnAny(typeof(ILighthouseClock), typeof(ILicenseService), typeof(IReportOwnerLookup),
                    typeof(IReportReader), typeof(IReportWriter), typeof(IReportMetricSeries))
                .AndShould().NotDependOnAny(Types().That().HaveFullNameMatching(RepositoryPortPattern))
                .Because("a policy that fetches its own clock, licence or data can no longer be tested by handing it values.")
                .Check(Architecture);
        }

        private static List<ArchUnitNET.Domain.IType> ReportsModuleTypes()
            => [.. Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.ReportsPattern)
                .Or().ResideInNamespaceMatching(ReportModelsPattern)
                .GetObjects(Architecture)];

        private static string NameAndNestedTypesOf(Type type) => $"^{Regex.Escape(type.FullName!)}($|\\+.*)";

        private static List<Type> Implementations(Type port)
            => [.. typeof(Program).Assembly.GetTypes().Where(type => port.IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false })];

        private static HashSet<Type> ReadShapes()
        {
            var shapes = new HashSet<Type>();
            var pending = new Queue<Type>(typeof(IReportQueries).GetMethods().Select(method => method.ReturnType));
            while (pending.TryDequeue(out var type))
            {
                foreach (var argument in type.GetGenericArguments())
                {
                    pending.Enqueue(argument);
                }

                var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (definition.Namespace?.StartsWith("Lighthouse.Backend", StringComparison.Ordinal) != true || definition.IsEnum || !shapes.Add(definition))
                {
                    continue;
                }

                foreach (var property in definition.GetProperties())
                {
                    pending.Enqueue(property.PropertyType);
                }
            }

            return shapes;
        }

        private static IEnumerable<string> SerialisedNamesOf(PropertyInfo property)
        {
            yield return property.Name;

            var renamed = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (renamed is not null)
            {
                yield return renamed.Name;
            }
        }
    }
}
