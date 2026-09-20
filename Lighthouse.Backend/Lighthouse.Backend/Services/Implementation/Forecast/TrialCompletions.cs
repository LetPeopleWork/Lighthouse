namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// How often each row finished on each day, and how often work first began on each row and on each
    /// Feature, counted up as the simulated runs go. One of these belongs to one worker, so nothing is
    /// written to from two places at once and there is no lock anywhere near the busiest loop in the
    /// product. They are added together once, after all the runs are done.
    ///
    /// Adding is what makes the total independent of how the runs were shared out; putting the days in order
    /// is the caller's job, once, rather than every worker's on the way in.
    /// </summary>
    public sealed class TrialCompletions
    {
        private readonly Dictionary<int, int>[] daysEachRowFinishedOn;
        private readonly Dictionary<int, int>[] daysEachRowWasFirstWorkedOn;
        private readonly Dictionary<int, int>[] daysEachFeatureWasFirstWorkedOn;

        public TrialCompletions(int rowCount, int featureCount)
        {
            daysEachRowFinishedOn = ADayCountPer(rowCount);
            daysEachRowWasFirstWorkedOn = ADayCountPer(rowCount);
            daysEachFeatureWasFirstWorkedOn = ADayCountPer(featureCount);
        }

        public void RecordThat(int rowIndex, int finishedOnDay)
            => CountOneMore(daysEachRowFinishedOn[rowIndex], finishedOnDay);

        /// <summary>
        /// The first day this run worked the row, whether or not that item finished it. Called at most
        /// once per row per run - a Feature is started once, and the days after that are not starts.
        /// </summary>
        public void RecordThatWorkBeganOnRow(int rowIndex, int beganOnDay)
            => CountOneMore(daysEachRowWasFirstWorkedOn[rowIndex], beganOnDay);

        /// <summary>
        /// The first day this run worked any row of the Feature, which is the earliest of its rows within
        /// this one run. Taken here rather than from the finished per-team distributions afterwards:
        /// combining those would have to assume the teams move independently, and a Feature whose teams
        /// both wait on the same upstream work is exactly the case where they do not.
        /// </summary>
        public void RecordThatWorkBeganOnFeature(int featureIndex, int beganOnDay)
            => CountOneMore(daysEachFeatureWasFirstWorkedOn[featureIndex], beganOnDay);

        public void AddInto(Dictionary<int, int>[] total) => AddAllOf(daysEachRowFinishedOn, total);

        public void AddRowStartsInto(Dictionary<int, int>[] total) => AddAllOf(daysEachRowWasFirstWorkedOn, total);

        public void AddFeatureStartsInto(Dictionary<int, int>[] total) => AddAllOf(daysEachFeatureWasFirstWorkedOn, total);

        private static Dictionary<int, int>[] ADayCountPer(int howMany)
        {
            var counts = new Dictionary<int, int>[howMany];

            for (var index = 0; index < howMany; index++)
            {
                counts[index] = [];
            }

            return counts;
        }

        private static void CountOneMore(Dictionary<int, int> days, int day)
            => days[day] = days.GetValueOrDefault(day) + 1;

        private static void AddAllOf(Dictionary<int, int>[] share, Dictionary<int, int>[] total)
        {
            for (var index = 0; index < share.Length; index++)
            {
                foreach (var day in share[index])
                {
                    total[index][day.Key] = total[index].GetValueOrDefault(day.Key) + day.Value;
                }
            }
        }
    }
}
