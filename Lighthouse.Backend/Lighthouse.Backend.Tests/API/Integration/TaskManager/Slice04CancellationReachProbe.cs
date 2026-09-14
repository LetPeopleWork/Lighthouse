using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.Diagnostics;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// Epic #5511 slice 04's pre-slice probe (P5), run 2026-09-14 during slice 02.
    ///
    /// The question DESIGN could not answer on paper: where does a Team refresh actually spend its
    /// wall-clock, and which of those points can observe a cancellation token <em>without</em> changing
    /// <c>IWorkTrackingConnector</c>'s public signatures. S9 says none of the port's methods takes a
    /// token; S10 says nearly all the time is inside those calls. If both hold in the way DESIGN feared,
    /// a checkpoint between phases cancels nothing an operator would notice.
    ///
    /// What survives here is the structural half: an ambient value set by the queue is readable inside a
    /// connector call. That is load bearing - slice 04 is designed on it - so it stays in the ordinary
    /// suite and keeps being true.
    ///
    /// The probe's other half measured how a refresh's wall-clock splits between the connector and
    /// everything Lighthouse does itself. That number is recorded in the slice brief rather than asserted
    /// here: it is a ratio between two durations, and under a loaded suite the second one grows until the
    /// assertion fails for reasons that have nothing to do with the claim. A measurement is evidence for a
    /// decision already taken, not an invariant to re-check on every run.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-04-probe")]
    public class Slice04CancellationReachProbe : TaskManagerAcceptanceTest
    {
        [Test]
        public async Task TheAmbientContextTheQueueAlreadySets_IsReadableInsideAConnectorCall()
        {
            // Not a stand-in: this is the production WriteBackRoundContext, which UpdateQueueService sets
            // immediately before it invokes an update task, exactly where slice 04 would set a token. If
            // that value is visible inside the connector then an AsyncLocal reaches through the whole call
            // chain - the queue, the updater, the data service, the work-item service - and into a
            // connector method whose signature mentions nothing of the kind.
            var team = SeedTeam(SeedConnection(), $"Team {Guid.NewGuid():N}");
            var roundContext = Factory.Services.GetRequiredService<WriteBackRoundContext>();

            WriteBackRound? seenInsideTheConnector = null;
            var connectorWasCalled = false;

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    connectorWasCalled = true;
                    seenInsideTheConnector = roundContext.Current;
                    return Task.FromResult<IEnumerable<WorkItem>>([]);
                });

            await RunUpdate(sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connectorWasCalled, Is.True, "The refresh never reached the connector at all.");
                Assert.That(seenInsideTheConnector, Is.Not.Null,
                    "The refresh round the queue opened is readable from inside the connector call. Cancellation can "
                    + "reach the same way, without any of IWorkTrackingConnector's signatures changing - which is the "
                    + "conclusion S9 was read as ruling out.");
            }
        }

    }
}
