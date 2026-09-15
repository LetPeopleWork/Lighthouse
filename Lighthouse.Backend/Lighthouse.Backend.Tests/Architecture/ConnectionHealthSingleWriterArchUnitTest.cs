using ArchUnitNET.NUnit;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.ConnectionHealth;
using Lighthouse.Backend.Services.Implementation.ConnectionHealth;
using Lighthouse.Backend.Services.Implementation.Repositories;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ConnectionHealthSingleWriterArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string Because =
            "A verdict is recorded in two places and only two - a refresh for that connection ended, and " +
            "an administrator pressed Test connection - and both go through ConnectionHealthService. A " +
            "second writer is how two answers to 'is this credential working' start disagreeing, and an " +
            "administrator who has met two answers stops believing either. That is what the OAuth-only " +
            "aggregator this replaced was deleted for rather than generalised. The row itself, its " +
            "repository and the database context carry it; nothing else may reach for it. If a new path " +
            "genuinely needs to record a verdict, take IConnectionHealthService instead. Program is " +
            "exempt because naming a type is how a composition root registers it, which is the opposite " +
            "of a second writer.";

        [Test]
        public void NoProductionTypeButTheHealthServiceKnowsHowAVerdictIsRecorded()
        {
            Classes()
                .That().AreNot(typeof(ConnectionHealthService))
                .And().AreNot(typeof(ConnectionHealthVerdictRepository))
                .And().AreNot(typeof(LighthouseAppContext))
                .And().AreNot(typeof(Program))
                .Should().NotDependOnAny(typeof(ConnectionHealthVerdict))
                .Because(Because)
                .Check(Architecture);
        }
    }
}
