using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // The three history cores stay pure for the same reason the validation verdict ladder does: the
    // interesting decisions — how spans become moves, when work started, which of two causes stopped
    // the history — all have to be reachable without an HttpMessageHandler mock, or mutation testing them stops being affordable.
    //
    // There is a second reason here. The history never reads `end` and `duration`,
    // and the Glide duration is an epoch offset that reads as a plausible timestamp when parsed
    // wrongly. `ServiceNowStateSpan` not carrying those fields is what keeps that structural.
    [TestFixture]
    public class ServiceNowHistoryPurityArchUnitTest
    {
        // Read from the type, so the rule follows the ServiceNow connector wherever its namespace moves.
        private static readonly string Namespace = typeof(ServiceNowHistoryVerdict).Namespace!;

        private static readonly string SpanMapper = Namespace + ".ServiceNowStateSpanMapper";
        private static readonly string HistoryQuery = Namespace + ".ServiceNowHistoryQuery";
        private static readonly string HistoryVerdict = Namespace + ".ServiceNowHistoryVerdict";

        private const string PersistencePattern = @"^Lighthouse\.Backend\.Data($|\..*)";
        private const string LoggingPattern = @"^Microsoft\.Extensions\.Logging($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TheHistoryCores_DoNotSpeakHttp()
        {
            Types().That().HaveFullName(SpanMapper).Or().HaveFullName(HistoryQuery).Or().HaveFullName(HistoryVerdict)
                .Should().NotDependOnAny(Types().That().HaveFullName("System.Net.Http.HttpClient")
                    .Or().HaveFullName("System.Net.Http.HttpMessageHandler")
                    .Or().HaveFullName("System.Net.Http.HttpResponseMessage")
                    .Or().HaveFullName("System.Net.Http.HttpRequestMessage"))
                .Because(
                    "ADR-118 keeps the connector as the only thing that talks to the instance. A core that fetched " +
                    "its own spans could only be tested through a transport mock, and the span-pairing rules are " +
                    "exactly what has to be cheap to test.")
                .Check(Architecture);
        }

        [Test]
        public void TheHistoryCores_DoNotLog()
        {
            Types().That().HaveFullName(SpanMapper).Or().HaveFullName(HistoryQuery).Or().HaveFullName(HistoryVerdict)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(LoggingPattern))
                .Because(
                    "Return-only, like the verdict ladder. What an administrator needs to know about a missing " +
                    "capability belongs in the advisory they read, not in a log line nobody opens.")
                .Check(Architecture);
        }

        [Test]
        public void TheHistoryCores_DoNotReachForThePersistenceLayer()
        {
            Types().That().HaveFullName(SpanMapper).Or().HaveFullName(HistoryQuery).Or().HaveFullName(HistoryVerdict)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistencePattern))
                .Because(
                    "Each core is a pure function of what the instance answered. A lookup against stored state " +
                    "would make the result depend on data none of the rules are defined in terms of.")
                .Check(Architecture);
        }
    }
}
