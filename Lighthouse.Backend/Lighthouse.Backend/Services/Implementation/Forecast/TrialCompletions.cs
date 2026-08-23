namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// How often each row finished on each day, counted up as the simulated runs go. One of these belongs to
    /// one worker, so nothing is written to from two places at once and there is no lock anywhere near the
    /// busiest loop in the product. They are added together once, after all the runs are done.
    ///
    /// Adding is what makes the total independent of how the runs were shared out; putting the days in order
    /// is the caller's job, once, rather than every worker's on the way in.
    /// </summary>
    public sealed class TrialCompletions
    {
        private readonly Dictionary<int, int>[] daysEachRowFinishedOn;

        public TrialCompletions(int rowCount)
        {
            daysEachRowFinishedOn = new Dictionary<int, int>[rowCount];

            for (var row = 0; row < rowCount; row++)
            {
                daysEachRowFinishedOn[row] = [];
            }
        }

        public void RecordThat(int rowIndex, int finishedOnDay)
        {
            var days = daysEachRowFinishedOn[rowIndex];
            days[finishedOnDay] = days.GetValueOrDefault(finishedOnDay) + 1;
        }

        public void AddInto(Dictionary<int, int>[] total)
        {
            for (var row = 0; row < daysEachRowFinishedOn.Length; row++)
            {
                foreach (var day in daysEachRowFinishedOn[row])
                {
                    total[row][day.Key] = total[row].GetValueOrDefault(day.Key) + day.Value;
                }
            }
        }
    }
}
