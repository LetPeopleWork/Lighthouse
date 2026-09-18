using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A refresh round keeps what it is holding to itself. The executions of one round run at the same
    /// time, and the round serialises every touch of that state behind its own lock - which only works
    /// for as long as the round is the one doing the touching. The moment another type can reach the
    /// staging area, it reaches it without the lock, and the loss it causes is silent: a field the
    /// operator asked to have written back to their tracker that simply never arrives, with nothing in
    /// any log saying so.
    ///
    /// The key the staging area is indexed by is the part that has to stay inside. Making it visible is
    /// the first step of every version of this mistake, so that is what is guarded.
    /// </summary>
    [TestFixture]
    public class WriteBackRoundStateOwnershipArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string Because =
            "Only the round itself may reach what the round is holding. Its executions run at the same " +
            "time and the round's lock is the only thing keeping their stagings from overwriting each " +
            "other; a second toucher is outside that lock by construction. If something else needs what " +
            "the round has, ask the round for it - Stage and TakeStaged are the whole surface, and both " +
            "are safe to call from anywhere.";

        [Test]
        public void NothingOutsideTheRound_ReachesWhatTheRoundIsHolding()
        {
            var stagingKey = Architecture.Types.Single(
                type => type.FullName.StartsWith(typeof(WriteBackRound).FullName!, StringComparison.Ordinal)
                    && type.Name.Contains("StagingKey", StringComparison.Ordinal));

            Types()
                .That().AreNot(typeof(WriteBackRound))
                .And().AreNot(stagingKey)
                .Should().NotDependOnAny(stagingKey)
                .Because(Because)
                .Check(Architecture);
        }
    }
}
