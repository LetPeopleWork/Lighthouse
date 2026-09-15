using Lighthouse.Backend.Tests.TestHelpers;

using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// What one item's history costs. Azure DevOps has no endpoint that answers "when did this cross into
    /// Doing" - the only way to know is to download every revision the item has and read the state changes
    /// out of them. That download is the single most expensive thing a refresh does here, and the connector
    /// wants the same list for four different questions: when work started, when it closed, when it last
    /// re-entered To Do, and the full set of transitions to sync.
    ///
    /// Asking four times gets four identical answers at four times the price.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public class AzureDevOpsHistoryReadTest
    {
        private static readonly int[] ThreeItems = [1, 2, 3];

        [Test]
        public async Task GetWorkItemsForTeam_ReadsEachItemsHistoryOnce()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(ThreeItems);

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.RevisionReads, Has.Count.EqualTo(ThreeItems.Length),
                "One read per item is what the refresh needs. Every read beyond that re-downloads a list the "
                + "connector is already holding, on every item, on every cycle - and on Azure DevOps this is "
                + "the download that dominates the wall clock of the whole refresh.");
        }

        [Test]
        public async Task GetFeaturesForProject_ReadsEachFeaturesHistoryOnce()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(ThreeItems);

            await subject.GetFeaturesForProject(portfolio, CancellationToken.None);

            Assert.That(ado.RevisionReads, Has.Count.EqualTo(ThreeItems.Length),
                "The portfolio half converts through the same path as the team half, so it pays the same "
                + "multiple and has to be held to the same count.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_StillDatesTheWorkItFoundTheHistoryFor()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);

            var workItem = (await subject.GetWorkItemsForTeam(team, CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.StartedDate, Is.EqualTo(WhenTheTrackerSaysItLastChanged),
                    "Reading the history once has to answer the same question reading it four times did. A "
                    + "started date that goes null is an item that reads as never begun, and its age and cycle "
                    + "time go with it.");

                Assert.That(workItem.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "An instant has no time zone, and a date that comes back Unspecified is reduced to a "
                    + "calendar day in whatever zone the host happens to be in - which is how a cycle time "
                    + "lands a day out and nothing looks wrong.");

                Assert.That(workItem.SyncedTransitions, Has.Count.EqualTo(1),
                    "The item held two states, so it made exactly one move. Two would mean the walk started "
                    + "counting the state an item was created in as a transition it made; none would mean the "
                    + "second reader of the shared list got nothing.");

                Assert.That(workItem.SyncedTransitions[0].FromState, Is.EqualTo("New"),
                    "The move is read by pairing each state with the one before it. A blank origin is the "
                    + "created-in state leaking through as a move.");

                Assert.That(workItem.SyncedTransitions[0].ToState, Is.EqualTo("Active"),
                    "And where it landed - the half that says which category the item entered.");

                Assert.That(workItem.SyncedTransitions[0].TransitionedAt.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "The transitions carry their own normalisation, separate from the dates above. Sharing "
                    + "one revision list must not lose it.");
            }
        }

        private static readonly int[] TheOnlyItem = [1];
    }
}
