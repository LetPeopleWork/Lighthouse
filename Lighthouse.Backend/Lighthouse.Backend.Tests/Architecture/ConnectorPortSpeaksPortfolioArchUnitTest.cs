using System.Reflection;
using System.Runtime.CompilerServices;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Microsoft.Extensions.Logging;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Lighthouse calls the thing a connector fetches Features for a Portfolio, while every tracker also has
    /// something it calls a project. When the connector port and its connectors name a Portfolio "project",
    /// a reader cannot tell which of the two a method means, and every interface split off the port copies
    /// the wrong word. So no member of the port, no member implementing it, and no Portfolio parameter of any
    /// method on the port or a connector may say Project. A method about a tracker's own project, with no
    /// Portfolio in its parameters, is fine.
    /// </summary>
    [TestFixture]
    public class ConnectorPortSpeaksPortfolioArchUnitTest
    {
        [Test]
        public void ThePortAndEveryConnectorNameAPortfolioAsAPortfolio()
        {
            var connectors = ConnectorPortNaming.ConnectorsIn(typeof(IWorkTrackingConnector).Assembly);

            var offences = ConnectorPortNaming.ProjectNamesForAPortfolio(connectors);

            using var _ = Assert.EnterMultipleScope();
            Assert.That(connectors, Has.Count.GreaterThanOrEqualTo(5),
                "WHAT: fewer than the five connectors (Jira, Azure DevOps, Linear, CSV, ServiceNow) were found implementing the connector port. " +
                "WHY: an empty or short population makes the naming check pass without looking. " +
                "HOW: the check reads every concrete type in Lighthouse.Backend that implements IWorkTrackingConnector; make sure the connectors still implement it.");
            Assert.That(offences, Is.Empty,
                "WHAT: the connector port or a connector still says Project for a Portfolio:\n  " + string.Join("\n  ", offences) + "\n" +
                "WHY: a Portfolio named project is read as a tracker's project, and the next interfaces split off the port would copy the wrong word. " +
                "HOW: rename GetFeaturesForProject to GetFeaturesForPortfolio on the port and on every connector, and name every Portfolio parameter portfolio.");
        }

        [Test]
        public void AConnectorAddedLaterThatNamesAPortfolioProject_IsNamedByTheCheck()
        {
            var offences = OffencesOnlyOf(typeof(ConnectorThatNamesAPortfolioProject));

            Assert.That(offences, Is.EquivalentTo(new[] { $"{typeof(ConnectorThatNamesAPortfolioProject).FullName}.ReadsTheFilterOf(project)" }),
                "WHAT: for a connector with a private method taking a Portfolio named project, the check did not name exactly that parameter. " +
                "WHY: then a connector added later, or a private helper of an existing one, could bring the wrong word back unseen. " +
                "HOW: the check must read every method declared on a connector, private and static ones included, not only the port's members.");
        }

        [Test]
        public void AConnectorThatSaysProjectOnlyForATrackerProject_PassesTheCheck()
        {
            var offences = OffencesOnlyOf(typeof(ConnectorThatReadsATrackerProject));

            Assert.That(offences, Is.Empty,
                "WHAT: a connector method about a tracker's own project, with no Portfolio parameter, was named by the check:\n  " + string.Join("\n  ", offences) + "\n" +
                "WHY: trackers have projects of their own (a Linear project becomes a Feature), and those names are right. " +
                "HOW: flag a member name only when it is a port member or implements one, and a parameter only when it is a Portfolio.");
        }

        // The port itself is part of every answer; these made-up connectors are judged on what they declare.
        private static List<string> OffencesOnlyOf(Type connector)
        {
            return ConnectorPortNaming.ProjectNamesForAPortfolio([connector])
                .Where(offence => offence.StartsWith(connector.FullName + ".", StringComparison.Ordinal))
                .ToList();
        }

        public class ConnectorThatNamesAPortfolioProject(ILogger<CsvWorkTrackingConnector> logger) : CsvWorkTrackingConnector(logger)
        {
            public static string Describe(Portfolio portfolio) => ReadsTheFilterOf(portfolio);

            private static string ReadsTheFilterOf(Portfolio project) => project.Name;
        }

        public class ConnectorThatReadsATrackerProject(ILogger<CsvWorkTrackingConnector> logger) : CsvWorkTrackingConnector(logger)
        {
            public static string NameOfTheTrackerProject(string project, Portfolio portfolio) => $"{project} for {portfolio.Name}";
        }
    }

    internal static class ConnectorPortNaming
    {
        private const BindingFlags DeclaredMethods =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static IReadOnlyList<Type> ConnectorsIn(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IWorkTrackingConnector).IsAssignableFrom(type))
                .ToList();
        }

        public static IReadOnlyList<string> ProjectNamesForAPortfolio(IEnumerable<Type> connectors)
        {
            var port = typeof(IWorkTrackingConnector);
            var offences = new List<string>();

            offences.AddRange(port.GetMethods()
                .Where(method => SaysProject(method.Name))
                .Select(method => $"{port.FullName}.{method.Name}"));
            offences.AddRange(PortfolioParametersNamedProject(port, port.GetMethods()));

            foreach (var connector in connectors)
            {
                var declared = connector.GetMethods(DeclaredMethods)
                    .Where(method => !method.IsDefined(typeof(CompilerGeneratedAttribute), false) && !method.Name.StartsWith('<'))
                    .ToList();

                var implementing = connector.GetInterfaceMap(port).TargetMethods
                    .Where(method => method.DeclaringType == connector);

                offences.AddRange(implementing
                    .Where(method => SaysProject(method.Name))
                    .Select(method => $"{connector.FullName}.{method.Name}"));
                offences.AddRange(PortfolioParametersNamedProject(connector, declared));
            }

            return offences.Distinct().ToList();
        }

        private static IEnumerable<string> PortfolioParametersNamedProject(Type owner, IEnumerable<MethodInfo> methods)
        {
            return methods.SelectMany(method => method.GetParameters()
                .Where(parameter => parameter.ParameterType == typeof(Portfolio)
                    && parameter.Name is not null
                    && parameter.Name.Contains("project", StringComparison.OrdinalIgnoreCase))
                .Select(parameter => $"{owner.FullName}.{method.Name}({parameter.Name})"));
        }

        private static bool SaysProject(string memberName) => memberName.Contains("Project", StringComparison.Ordinal);
    }
}
