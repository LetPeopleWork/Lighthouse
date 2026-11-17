using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Ports;
using Mono.Cecil;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The connector port with its capabilities, the connector and system factories with their interfaces, the authentication
    /// schema and the board types live in the Tracker integration module's Application, and none is left under Services or
    /// Factories. The types are named as strings and read from the compiled production assembly, so a type that is missing,
    /// renamed or left behind is a failure that names it. Two leaks are closed by placement: a factory interface and its class
    /// share one namespace, and the board port names no implementation namespace in its signatures. Both rules also run on
    /// made-up types: an offender must be reported and its compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationApplicationLayoutArchUnitTest
    {
        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.TrackerIntegration.Application";

        private const string WhereTheTypesLivedBefore = @"^Lighthouse\.Backend\.(Services|Factories)(\.|$)";

        private const string TheWholeAssembly = @"^Lighthouse\.Backend\.";

        // The board port sits in the old connector-port namespace until it moves, and in the Application after.
        private const string WhereTheBoardPortLives = @"^Lighthouse\.Backend\.(Services\.Interfaces\.WorkTrackingConnectors|Modules\.TrackerIntegration\.Application)$";

        private const string AnImplementationNamespace = @"^Lighthouse\.Backend\.(Services\.Implementation|Modules\.TrackerIntegration\.Adapters)(\.|$)";

        private const string FixturePorts = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.ApplicationLayout\.Ports$";

        private const string FixtureImplementation = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.ApplicationLayout\.Implementation(\.|$)";

        private const string FixtureApplicationNamespace = "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Application";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.ApplicationLayout\.Factories(\.|$)";

        private const string TheBoardPort = "IBoardInformationProvider";

        private static readonly string[] ConnectorPortTypes =
        [
            "IWorkTrackingConnector",
            "IAzureDevOpsWorkTrackingConnector",
            "IJiraWorkTrackingConnector",
            "ILinearWorkTrackingConnector",
            "IServiceNowWorkTrackingConnector",
            "IBoardInformationProvider",
            "IDeliveryForecastPublisher",
            "IDeliverySourceProvider",
            "IFeatureSource",
            "IIncrementalSource",
            "IWriteBackTarget",
            "IWorkTrackingAuthStrategy",
            "IWorkTrackingAuthStrategyFactory",
            "WorkTrackingReadException",
            "WorkTrackingRefusedException",
        ];

        private static readonly string[] FactoryTypes =
        [
            "IWorkTrackingConnectorFactory",
            "WorkTrackingConnectorFactory",
            "IWorkTrackingSystemFactory",
            "WorkTrackingSystemFactory",
        ];

        private static readonly string[] AuthenticationSchemaTypes =
        [
            "AuthenticationMethodKeys",
            "AuthenticationMethodSchema",
            "AuthenticationMethod",
            "AuthenticationMethodOption",
        ];

        private static readonly string[] BoardTypes =
        [
            "Board",
            "BoardInformation",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationApplicationLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheConnectorPortAndItsCapabilities_LiveInTheTrackerIntegrationApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, ConnectorPortTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheConnectorAndSystemFactoriesAndTheirInterfaces_LiveInTheTrackerIntegrationApplication_AndNoneIsLeftUnderFactories()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, FactoryTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheAuthenticationSchema_LivesInTheTrackerIntegrationApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, AuthenticationSchemaTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheBoardTypes_LiveInTheTrackerIntegrationApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, BoardTypes, WhereTheTypesLivedBefore).Check();
        }

        [TestCase("IWorkTrackingConnectorFactory", "WorkTrackingConnectorFactory")]
        [TestCase("IWorkTrackingSystemFactory", "WorkTrackingSystemFactory")]
        public void AFactoryInterfaceAndItsClass_ShareOneNamespace(string factoryInterface, string factory)
        {
            var namespaces = new[] { factoryInterface, factory }
                .Select(name => $"{name} in [{string.Join(", ", NamespacesHolding(ProductionModule.Value, name))}]")
                .ToList();
            var shared = NamespacesHolding(ProductionModule.Value, factoryInterface)
                .Intersect(NamespacesHolding(ProductionModule.Value, factory), StringComparer.Ordinal)
                .ToList();

            Assert.That(
                shared,
                Has.Count.EqualTo(1),
                $"The factory interface and its class do not share one namespace: {string.Join("; ", namespaces)}. " +
                "Why it matters: a caller that has to import two namespaces for one factory shows that the interface and the class were never placed together, " +
                $"and the interface name then hides which layer owns the factory. How to fix: move both files into the module's Application folder so both say {ApplicationNamespace}.");
        }

        [Test]
        public void TheBoardPort_NamesNoImplementationNamespaceInItsSignatures()
        {
            var naming = TypesNamingAnImplementationNamespace(ProductionModule.Value, WhereTheBoardPortLives, AnImplementationNamespace, TheBoardPort);

            Assert.That(
                naming,
                Is.Empty,
                $"The board port {TheBoardPort} names [{string.Join(", ", naming.SelectMany(dependent => dependent.Targets))}] in its signatures. " +
                "Why it matters: a port that names a type from an implementation namespace makes every caller of the port depend on the connectors, " +
                "so the layer that should hide them shows them. How to fix: move the board types the port returns into the module's Application beside the port.");
        }

        [Test]
        public void AFactoryTypeLeftUnderItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplicationNamespace, ["AWellPlacedFactory"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplicationNamespace, ["ALeftBehindFactory"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in the Application and has no copy in its former home was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left in its former home passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindFactory"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void APortThatNamesAnImplementationNamespace_IsReported_AndItsCompliantTwinIsNot()
        {
            var offender = typeof(ALeakyBoardPort).Name;
            var twin = typeof(ACleanBoardPort).Name;
            var implementation = typeof(global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Implementation.AnImplementationBoard).FullName!;

            var leaking = TypesNamingAnImplementationNamespace(FixtureModule.Value, FixturePorts, FixtureImplementation, offender);
            var clean = TypesNamingAnImplementationNamespace(FixtureModule.Value, FixturePorts, FixtureImplementation, twin);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    leaking.SelectMany(dependent => dependent.Targets),
                    Does.Contain(implementation),
                    $"The rule did not report {offender} for naming {implementation}. Why it matters: a board port that leaks an implementation type must be caught. How to fix: look at every type a port's signature names, generic arguments included.");

                Assert.That(
                    clean,
                    Is.Empty,
                    $"The rule reported {twin}, which names only its own namespace. Why it matters: a rule that flags a clean port flags everything. How to fix: report only types in the implementation namespaces.");
            }
        }

        private static List<string> NamespacesHolding(ModuleDefinition module, string typeName)
            => IlDependencies.TypesIn(module, TheWholeAssembly)
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal)
                .Where(fullName => SimpleNameOf(fullName) == typeName)
                .Select(fullName => fullName[..^(typeName.Length + 1)])
                .ToList();

        // The port is looked up by name in the namespaces it may live in, so a port that is missing fails instead of passing unseen.
        private static List<IlDependent> TypesNamingAnImplementationNamespace(ModuleDefinition module, string whereThePortLives, string implementationNamespace, string portName)
        {
            var found = IlDependencies.TypesIn(module, whereThePortLives).Select(SimpleNameOf).Contains(portName);
            Assert.That(
                found,
                Is.True,
                $"The port {portName} was not found in a namespace matching \"{whereThePortLives}\". Why it matters: a rule that looked at no port passes unseen. How to fix: keep the port named {portName} in the module's Application.");

            return IlDependencies.DependentsIn(module, whereThePortLives, target => IlDependencies.ResidesInNamespaceMatching(target, implementationNamespace))
                .Where(dependent => SimpleNameOf(IlDependencies.DeclaringTypeOf(dependent.TypeName)) == portName)
                .ToList();
        }

        private static string SimpleNameOf(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Application
{
    internal sealed class AWellPlacedFactory
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Factories
{
    internal sealed class ALeftBehindFactory
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Implementation
{
    internal sealed class AnImplementationBoard
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Ports
{
    internal sealed class ABoard
    {
        public int Answer => 42;
    }

    internal sealed class ALeakyBoardPort
    {
        public Task<IEnumerable<global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Implementation.AnImplementationBoard>> GetBoards()
            => Task.FromResult(Enumerable.Empty<global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ApplicationLayout.Implementation.AnImplementationBoard>());
    }

    internal sealed class ACleanBoardPort
    {
        public Task<IEnumerable<ABoard>> GetBoards() => Task.FromResult(Enumerable.Empty<ABoard>());
    }
}
