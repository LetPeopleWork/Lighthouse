namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// What one simulated run knows: how much work each row still has, which day each row finished on, and
    /// therefore which rows may be worked on. It belongs to the run that made it and nothing else can see it.
    ///
    /// This used to live on the rows themselves, which meant every simulated run wrote to the same counters.
    /// That was safe only for as long as each Team's runs happened on their own; with the Teams on one clock
    /// and the runs carried out side by side it would be two runs sharing one count, which is wrong even
    /// when the writes do not tear. Removing the sharing is the fix - guarding it would not have been one.
    /// </summary>
    public sealed class TrialState
    {
        private const int StillHasWorkLeft = -1;

        private readonly ForecastRunPlan plan;
        private readonly int[] remainingOfRow;
        private readonly int[] remainingOfTeam;
        private readonly int[] dayEachRowFinished;
        private readonly int[] rowsReadyToBeWorkedOn;

        public TrialState(ForecastRunPlan plan)
        {
            this.plan = plan;
            remainingOfRow = new int[plan.RowCount];
            remainingOfTeam = new int[plan.TeamCount];
            dayEachRowFinished = new int[plan.RowCount];
            rowsReadyToBeWorkedOn = new int[plan.RowCount];

            StartAgain();
        }

        public int RemainingEverywhere { get; private set; }

        public void StartAgain()
        {
            Array.Clear(remainingOfTeam);
            Array.Fill(dayEachRowFinished, StillHasWorkLeft);
            RemainingEverywhere = 0;

            for (var row = 0; row < remainingOfRow.Length; row++)
            {
                var remaining = plan.InitialRemainingOf(row);

                remainingOfRow[row] = remaining;
                remainingOfTeam[plan.TeamOf(row)] += remaining;
                RemainingEverywhere += remaining;
            }
        }

        public int RemainingOf(int teamIndex) => remainingOfTeam[teamIndex];

        /// <summary>
        /// A Team learns that its own work is finished the moment it finishes it, and learns that another
        /// Team's work is finished the following day.
        ///
        /// Within a day a Team really does work its items one after another, so finishing what it was
        /// waiting for and starting the next thing on the same day is what a Team does. Two Teams share no
        /// such order: they both simply work day N, and asking which of them got there first has no answer.
        /// Answering it anyway with whatever order the Features happened to arrive from the store would
        /// hand a whole day of the wait back, silently, depending on nothing a user could see - and in a
        /// chain of waits across three Teams it would hand back one day per link.
        /// </summary>
        public bool ReadyToBeWorkedOn(int rowIndex, int today)
        {
            if (remainingOfRow[rowIndex] <= 0)
            {
                return false;
            }

            var itsOwnTeam = plan.TeamOf(rowIndex);

            foreach (var blocker in plan.MustFinishFirst(rowIndex))
            {
                var finished = plan.TeamOf(blocker) == itsOwnTeam
                    ? remainingOfRow[blocker] <= 0
                    : dayEachRowFinished[blocker] >= 0 && dayEachRowFinished[blocker] < today;

                if (!finished)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether anything at all may be worked on today. When the answer is no and no other Team's work
        /// finished yesterday either, no later day can change it - what a row waits for only clears when
        /// somebody finishes it, and nobody can start anything - so the run has ended rather than gone idle.
        /// </summary>
        public bool AnythingCanBeWorkedOn(int today)
        {
            if (plan.NobodyWaitsForAnything)
            {
                return RemainingEverywhere > 0;
            }

            for (var row = 0; row < remainingOfRow.Length; row++)
            {
                if (ReadyToBeWorkedOn(row, today))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The rows a Team may work on right now, in the order the run was handed them. That order is what
        /// decides which Feature the next item comes from, so it is the order the product has always used.
        ///
        /// Written into a buffer this run already owns rather than a new list, because it is asked for once
        /// per delivered item, which is the busiest thing a forecast does.
        /// </summary>
        public ReadOnlySpan<int> RowsReadyToBeWorkedOnBy(int teamIndex, int today)
        {
            var rowsOfTeam = plan.RowsOf(teamIndex);
            var howMany = 0;

            for (var index = 0; index < rowsOfTeam.Length; index++)
            {
                if (ReadyToBeWorkedOn(rowsOfTeam[index], today))
                {
                    rowsReadyToBeWorkedOn[howMany] = rowsOfTeam[index];
                    howMany++;
                }
            }

            return rowsReadyToBeWorkedOn.AsSpan(0, howMany);
        }

        /// <returns>True when that was the last item of the row, which is the day the row finished.</returns>
        public bool CloseOneItemOf(int rowIndex, int today)
        {
            remainingOfRow[rowIndex] -= 1;
            remainingOfTeam[plan.TeamOf(rowIndex)] -= 1;
            RemainingEverywhere -= 1;

            if (remainingOfRow[rowIndex] > 0)
            {
                return false;
            }

            dayEachRowFinished[rowIndex] = today;

            return true;
        }
    }
}
