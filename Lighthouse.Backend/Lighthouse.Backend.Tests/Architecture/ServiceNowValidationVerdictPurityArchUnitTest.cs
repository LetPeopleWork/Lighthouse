using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // Story #5574 / ADR-114, widened by #5611. The verdict ladders are the only interesting logic in
    // the connection and team-settings slices and the only place the "denial wearing a success
    // costume" bug can be caught. Keeping them pure functions is what makes every rung reachable
    // without an HttpMessageHandler mock — which is in turn what makes the Stryker density of the
    // DoD affordable. The moment one acquires a client, a logger or a DbContext, the rungs stop
    // being unit-testable and the ladder rots.
    [TestFixture]
    public class ServiceNowValidationVerdictPurityArchUnitTest
    {
        private const string Namespace = "Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow";

        private const string Verdict = Namespace + ".ServiceNowValidationVerdict";

        // Story #5611 / ADR-124 decision 4. The team-settings ladder answers the same question in the
        // same vocabulary and was never covered here; the class rungs are what make that a gap worth
        // closing. ServiceNowReadScope joins it as the slice's new core.
        private const string TeamQueryVerdict = Namespace + ".ServiceNowTeamQueryVerdict";

        private const string ReadScope = Namespace + ".ServiceNowReadScope";

        // Story #5610 / ADR-125, ADR-126 decision 3. The board picker's two new cores: the one rung a
        // board list must NOT inherit (an empty list is not a failure) and the board-row-to-Board
        // translation. Both are decisions, so both stay reachable without a transport mock.
        private const string BoardVerdict = Namespace + ".ServiceNowBoardVerdict";

        private const string BoardMapper = Namespace + ".ServiceNowBoardMapper";

        private const string PersistencePattern = @"^Lighthouse\.Backend\.Data($|\..*)";
        private const string LoggingPattern = @"^Microsoft\.Extensions\.Logging($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        // The three rules below are stated by full name, so a core that does not exist yet passes
        // them by being absent rather than by being pure. This is the rung that says it has to exist.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public void TheBoardPickersDecisions_LiveInPureCoresOfTheirOwn()
        {
            var production = typeof(Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow.ServiceNowValidationVerdict).Assembly;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(production.GetType(BoardVerdict), Is.Not.Null,
                    "An empty board list is a decision — ADR-114's no_records_visible rung is intercepted rather than inherited — and decisions live in a pure core.");
                Assert.That(production.GetType(BoardMapper), Is.Not.Null,
                    "A board row becoming a Board is a translation with its own vocabulary, and it is the one class that must never learn readable_filter exists.");
            }
        }

        [Test]
        public void TheVerdictLadder_DoesNotSpeakHttp()
        {
            Types().That().HaveFullName(Verdict)
                .Or().HaveFullName(TeamQueryVerdict)
                .Or().HaveFullName(ReadScope)
                .Or().HaveFullName(BoardVerdict)
                .Or().HaveFullName(BoardMapper)
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
                .Or().HaveFullName(TeamQueryVerdict)
                .Or().HaveFullName(ReadScope)
                .Or().HaveFullName(BoardVerdict)
                .Or().HaveFullName(BoardMapper)
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
                .Or().HaveFullName(TeamQueryVerdict)
                .Or().HaveFullName(ReadScope)
                .Or().HaveFullName(BoardVerdict)
                .Or().HaveFullName(BoardMapper)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistencePattern))
                .Because(
                    "ADR-114: the verdict is a pure function of what the instance answered. A lookup against " +
                    "stored state would make the ladder depend on data no rung is defined in terms of.")
                .Check(Architecture);
        }
    }
}
