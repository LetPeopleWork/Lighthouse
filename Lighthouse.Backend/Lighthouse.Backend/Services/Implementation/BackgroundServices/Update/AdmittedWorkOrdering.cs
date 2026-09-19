namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The order an operator is shown the work this instance has admitted.
    ///
    /// Neither store has an order to give. In process <c>GetAdmittedWork</c> hands back a dictionary's
    /// buckets and under Redis a hash's fields; neither is an order, and both can come back differently
    /// from one read to the next. A list drawn as a sequence makes a claim about that sequence whether it
    /// means to or not, so the claim is made true here rather than left to the substrate.
    /// </summary>
    public static class AdmittedWorkOrdering
    {
        /// <summary>
        /// Running first, then longest-waiting first, because that is the one its own queue reaches next.
        ///
        /// One list, but the work underneath it is queued by kind - team, portfolio and forecast work each
        /// wait only for their own kind. So a row's place in this list is a reading order and not a count
        /// of what stands in front of it, and what a waiting row is actually waiting for is the name shown
        /// beside it.
        ///
        /// A row whose moment nobody recorded goes to the end of its own group rather than the end of the
        /// list. Sending it to the very end would drop a refresh that is genuinely under way below work
        /// that has not begun, which is the opposite of what an operator opens this list to find out.
        ///
        /// Ties are settled on what the row is and which entity it names - the only thing about a row that
        /// cannot change between two glances. Two admissions inside one tick of the clock are ordinary, and
        /// without this last step a hash would be left to settle them differently each time it was read.
        /// </summary>
        public static List<UpdateStatus> InTheOrderTheQueueWillReachThem(IEnumerable<UpdateStatus> admitted)
        {
            return [.. admitted.OrderBy(WhereItSitsInTheQueue)];
        }

        /// <summary>
        /// The moment the work entered the state it is now in: when it started running, or when it was
        /// admitted to wait. How long it has been there and where it sits in the list are two answers about
        /// that same moment, so both read it from here.
        ///
        /// Work that has finished is neither running nor waiting, and it can still be listed - briefly as
        /// it ends, or for good if the replica running it died in that window. Time since admission under a
        /// "Completed" label reads as time since it completed, which is a different and usually much
        /// smaller number.
        ///
        /// Absent is an ordinary answer rather than a fault: a replica still on an older build admits work
        /// without recording anything.
        /// </summary>
        public static DateTimeOffset? WhenItsCurrentStateBegan(UpdateStatus work) => work.Status switch
        {
            UpdateProgress.InProgress => work.StartedAt,
            UpdateProgress.Queued => work.QueuedAt,
            _ => null,
        };

        /// <summary>
        /// Each element is a rank rather than a fact about the row, and they are read in order until one
        /// of them settles the pair. Zero sorts first, so each name reads as the rule it encodes.
        /// </summary>
        private static (int RunningBeforeWaiting, int RecordedBeforeUnrecorded, DateTimeOffset Moment, UpdateType UpdateType, int Id) WhereItSitsInTheQueue(UpdateStatus work)
        {
            var moment = WhenItsCurrentStateBegan(work);

            return (
                work.Status == UpdateProgress.InProgress ? 0 : 1,
                moment.HasValue ? 0 : 1,
                moment ?? DateTimeOffset.MinValue,
                work.UpdateType,
                work.Id);
        }
    }
}
