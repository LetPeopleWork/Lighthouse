using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // Story #5574 / ADR-114. The verdict ladder is the only interesting logic in the connection
    // slice and the only place the "denial wearing a success costume" bug can be caught. Keeping
    // it a pure function is what makes all seven rungs reachable without an HttpMessageHandler
    // mock — which is in turn what makes the Stryker density of the DoD affordable. The moment it
    // acquires a client, a logger or a DbContext, the rungs stop being unit-testable and the
    // ladder rots.
    [TestFixture]
    public class ServiceNowValidationVerdictPurityArchUnitTest
    {
        private const string Verdict =
            "Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow.ServiceNowValidationVerdict";

        private const string PersistencePattern = @"^Lighthouse\.Backend\.Data($|\..*)";
        private const string LoggingPattern = @"^Microsoft\.Extensions\.Logging($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TheVerdictLadder_DoesNotSpeakHttp()
        {
            Types().That().HaveFullName(Verdict)
                .Should().NotDependOnAny(Types().That().HaveFullName("System.Net.Http.HttpClient")
                    .Or().HaveFullName("System.Net.Http.HttpMessageHandler")
                    .Or().HaveFullName("System.Net.Http.HttpResponseMessage")
                    .Or().HaveFullName("System.Net.Http.HttpRequestMessage"))
                .Because(
                    "ADR-114 functional core / imperative shell. The connector performs the probe and hands the " +
                    "verdict three scalars (status, was-it-JSON, row count). A verdict that fetches its own input " +
                    "can only be tested through a transport mock.")
                .Check(Architecture);
        }

        [Test]
        public void TheVerdictLadder_DoesNotLog()
        {
            Types().That().HaveFullName(Verdict)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(LoggingPattern))
                .Because(
                    "ADR-114: the verdict is return-only. Anything worth saying about a rung belongs in the " +
                    "ConnectionValidationResult the administrator actually reads, not in a log line.")
                .Check(Architecture);
        }

        [Test]
        public void TheVerdictLadder_DoesNotReachForThePersistenceLayer()
        {
            Types().That().HaveFullName(Verdict)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistencePattern))
                .Because(
                    "ADR-114: the verdict is a pure function of what the instance answered. A lookup against " +
                    "stored state would make the ladder depend on data no rung is defined in terms of.")
                .Check(Architecture);
        }
    }
}
