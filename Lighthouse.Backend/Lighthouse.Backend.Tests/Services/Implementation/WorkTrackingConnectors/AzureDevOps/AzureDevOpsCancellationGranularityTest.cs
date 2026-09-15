using Lighthouse.Backend.Tests.TestHelpers;

using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// Epic #5511 slice 04, AC-04.2 - the number, for Azure DevOps. The acceptance scenarios cancel a
    /// refresh and watch it stop, but they stop a <em>mock</em> written to honour the token; that proves the
    /// ask arrives, not that a real connector acts on it. These count the round trips a real
    /// <c>AzureDevOpsWorkTrackingConnector</c> makes against an organisation holding far more records than
    /// the test lets it read.
    ///
    /// Azure DevOps reaches its records in two round trips of different shapes - a WIQL that answers with
    /// ids alone, then one batched payload read per two hundred of them - so the checkpoint that matters is
    /// the batch loop, and a cancel before the WIQL must cost nothing at all.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public class AzureDevOpsCancellationGranularityTest
    {
        /// <summary>
        /// Ten batches at the connector's two hundred per read - far more than any test here lets it walk,
        /// so "it stopped" cannot be satisfied by an organisation that ran out of records to give.
        /// </summary>
        private static readonly int[] FarMoreRecordsThanTheTestWillRead = [.. Enumerable.Range(1, 2000)];

        /// <summary>Which batch read triggers the cancel - the third of ten, so seven remain unread.</summary>
        private const int TheBatchTheCancelArrivesOn = 3;

        /// <summary>
        /// Enough items that reading every one's history is unmistakably more work than stopping, and few
        /// enough that the batch reads are out of the way before the first revision read.
        /// </summary>
        private static readonly int[] ATeamWorthOfItems = [.. Enumerable.Range(1, 50)];

        /// <summary>
        /// Two per item: one to date the move into Doing, one to rebuild the synced transitions. An item
        /// that is done costs four, so this is the floor rather than the figure.
        /// </summary>
        private const int WhatReadingEveryItemsHistoryCosts = 100;

        private const int TheRevisionReadTheCancelArrivesOn = 3;

        [Test]
        public async Task GetWorkItemsForTeam_CancelledBeforeItStarts_NeverAsksTheTrackerAnything()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(FarMoreRecordsThanTheTestWillRead);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(team, alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(ado.EveryRequestMade, Is.Empty,
                    "A refresh cancelled before it began must not spend the rate limit an operator cancelled "
                    + "to protect. The WIQL is already too much.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_CancelledWhileItReadsBatches_StopsWithinOneBatchRoundTrip()
        {
            using var stopAfterTheThirdBatch = new CancellationTokenSource();

            var (subject, team, ado) = AnAzureDevOpsThatHolds(FarMoreRecordsThanTheTestWillRead);
            ado.OnRequest = () => StopOnceTheThirdBatchIsRead(ado, stopAfterTheThirdBatch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(team, stopAfterTheThirdBatch.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(ado.PayloadReads, Has.Count.EqualTo(3),
                    "This is the number AC-04.2 asserts against: a refresh told to stop mid-walk stops after "
                    + "the round trip it is already in, not after the whole result set. A fourth batch means "
                    + "the token never reached the read, where the organisation has seven batches left to give.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_CancelledBeforeItStarts_NeverAsksTheTrackerAnything()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(FarMoreRecordsThanTheTestWillRead);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.SweepWorkItemsForTeam(team, alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(ado.EveryRequestMade, Is.Empty,
                    "The sweep runs on the same cycle as the download and against the same organisation. A "
                    + "cancel it ignores spends the quota twice over.");
            }
        }

        [Test]
        public void SweepWorkItemsForTeam_CancelledWhileItReadsBatches_StopsWithinOneBatchRoundTrip()
        {
            using var stopAfterTheThirdBatch = new CancellationTokenSource();

            var (subject, team, ado) = AnAzureDevOpsThatHolds(FarMoreRecordsThanTheTestWillRead);
            ado.OnRequest = () => StopOnceTheThirdBatchIsRead(ado, stopAfterTheThirdBatch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.SweepWorkItemsForTeam(team, stopAfterTheThirdBatch.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(ado.PayloadReads, Has.Count.EqualTo(3),
                    "The sweep walks the same two hundred ids at a time as the download, and carries the same "
                    + "promise: the batch it is in, and no further.");
            }
        }

        /// <summary>
        /// The batch loop is not where an Azure DevOps refresh spends its time. Rebuilding one item's state
        /// transitions costs a revision read per item - up to four for an item that is done - so a team of
        /// any size pays far more round trips here than in the ten batch reads that fetched it. A cancel
        /// the conversion ignores is a Cancel button that stops almost none of the work.
        /// </summary>
        [Test]
        public void GetWorkItemsForTeam_CancelledWhileItRebuildsHistory_StopsReadingRevisions()
        {
            using var stopOnceHistoryIsBeingRead = new CancellationTokenSource();

            var (subject, team, ado) = AnAzureDevOpsThatHolds(ATeamWorthOfItems);
            ado.OnRequest = () =>
            {
                if (ado.RevisionReads.Count == TheRevisionReadTheCancelArrivesOn)
                {
                    stopOnceHistoryIsBeingRead.Cancel();
                }
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(team, stopOnceHistoryIsBeingRead.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(
                    ado.RevisionReads,
                    Has.Count.GreaterThan(0)
                        .And.Count.LessThan(WhatReadingEveryItemsHistoryCosts / 2),
                    "Both ends carry a claim. Above: the walk reached the history phase before it stopped, so "
                    + "a refresh that died in the batch reads cannot pass this wearing the same number. Below: "
                    + "a conversion that ignores the token reads every item's history anyway, which is the bulk "
                    + "of the refresh an operator just cancelled. The upper bound is loose on purpose - the walk "
                    + "is eight items wide, so reads already in flight when the cancel lands still finish.");
            }
        }

        private static void StopOnceTheThirdBatchIsRead(
            AzureDevOpsOrganisation ado, CancellationTokenSource stopping)
        {
            if (ado.PayloadReads.Count == TheBatchTheCancelArrivesOn)
            {
                stopping.Cancel();
            }
        }
    }
}
