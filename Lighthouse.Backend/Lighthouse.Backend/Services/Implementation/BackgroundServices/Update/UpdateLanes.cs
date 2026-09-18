namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using System.Threading.Channels;

    /// <summary>
    /// The queues updates actually wait in: one per lane, each drained by a reader of its own.
    ///
    /// With a single queue, work ran strictly in the order it was asked for, so a portfolio refresh that
    /// took hours held every team refresh behind it - three teams once sat for 3h38m and the operator
    /// read a working instance as a dead one. A lane each means a slow refresh can only ever hold up
    /// work of its own kind, which is the part that has to stay serial: two portfolio refreshes at once
    /// would double what one work tracking system is asked for at a time.
    ///
    /// This is a queue, and it deliberately knows nothing about what it is running. It is handed a key
    /// and a piece of work, it decides which lane the key belongs to, and it runs the work in turn.
    /// </summary>
    public sealed class UpdateLanes : IDisposable
    {
        private readonly Dictionary<UpdateLane, Channel<LaneWork>> lanes;
        private readonly Task[] readers;
        private readonly ILogger logger;

        public UpdateLanes(ILogger logger)
        {
            this.logger = logger;

            lanes = Enum.GetValues<UpdateLane>().ToDictionary(lane => lane, _ => Channel.CreateUnbounded<LaneWork>());
            readers = [.. lanes.Select(lane => StartReading(lane.Key, lane.Value.Reader))];
        }

        /// <summary>
        /// Puts work in the lane its key belongs to, and says whether the lane took it. A lane refuses
        /// only once it has been closed, which happens at shutdown - the caller then has to hand back
        /// everything queuing that work claimed, because nothing will ever run it.
        /// </summary>
        public bool WriteTo(UpdateKey key, Func<Task> work)
        {
            return lanes[UpdateLaneMapping.LaneOf(key.UpdateType)].Writer.TryWrite(new LaneWork(key, work));
        }

        /// <summary>
        /// Closes every lane and waits for all of them together. Waiting for them one after another would
        /// still be correct but would spend the shutdown budget in sequence; waiting for fewer than all
        /// of them would return while work is still running against a database the host is about to take
        /// away, and would leave that work's key admitted where another replica can see it.
        /// </summary>
        public async Task DrainAsync(CancellationToken cancellationToken)
        {
            CloseEveryLane();

            await Task.WhenAll(readers).WaitAsync(cancellationToken);
        }

        public void Dispose()
        {
            // Closing the lanes is what lets the reader loops end. Without it they stay parked on an
            // empty channel for the lifetime of the process.
            CloseEveryLane();
        }

        private void CloseEveryLane()
        {
            foreach (var lane in lanes.Values)
            {
                lane.Writer.TryComplete();
            }
        }

        private Task StartReading(UpdateLane lane, ChannelReader<LaneWork> reader)
        {
            // The loop below is the lane itself, not an update: it has to outlive every token any update
            // carries, or cancelling one refresh would stop this lane processing any others.
            return Task.Run(async () =>
            {
                // The reader outlives any one update, so it takes no update's token. It ends when the
                // lane is closed, which is what shutdown does.
                await foreach (var queued in reader.ReadAllAsync(CancellationToken.None))
                {
                    try
                    {
                        await queued.Work();
                    }
                    catch (Exception ex)
                    {
                        // Caught inside the loop, so one piece of work failing cannot stop the lane. It
                        // names what failed because a lane can now die on its own while the other two
                        // carry on - an unnamed error would leave nobody able to tell which kind of work
                        // stopped moving.
                        logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id} in the {Lane} lane", queued.Key.UpdateType, queued.Key.Id, lane);
                    }
                }
            }, CancellationToken.None);
        }

        private sealed record LaneWork(UpdateKey Key, Func<Task> Work);
    }
}
