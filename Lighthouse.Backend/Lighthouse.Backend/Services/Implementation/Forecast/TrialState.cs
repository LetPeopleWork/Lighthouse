namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// What one simulated run knows: how much work each row still has, and therefore which rows may be
    /// worked on. It belongs to the run that made it and nothing else can see it.
    ///
    /// This used to live on the rows themselves, which meant every simulated run wrote to the same counters.
    /// That was safe only for as long as each Team's runs happened on their own; with the Teams on one clock
    /// and the runs carried out side by side it would be two runs sharing one count, which is wrong even
    /// when the writes do not tear. Removing the sharing is the fix - guarding it would not have been one.
    /// </summary>
    public sealed class TrialState
    {
        private readonly ForecastRunPlan plan;
        private readonly int[] remainingOfRow;
        private readonly int[] remainingOfTeam;
        private readonly int[] rowsReadyToBeWorkedOn;

        public TrialState(ForecastRunPlan plan)
        {
            this.plan = plan;
            remainingOfRow = new int[plan.RowCount];
            remainingOfTeam = new int[plan.TeamCount];
            rowsReadyToBeWorkedOn = new int[plan.RowCount];

            StartAgain();
        }

        public int RemainingEverywhere { get; private set; }

        public void StartAgain()
        {
            Array.Clear(remainingOfTeam);
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

        public bool ReadyToBeWorkedOn(int rowIndex)
        {
            if (remainingOfRow[rowIndex] <= 0)
            {
                return false;
            }

            foreach (var blocker in plan.MustFinishFirst(rowIndex))
            {
                if (remainingOfRow[blocker] > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether anything at all may be worked on. When the answer is no, no later day can change it -
        /// what a row waits for only clears when somebody finishes it, and nobody can start anything - so
        /// the run has ended rather than gone idle.
        /// </summary>
        public bool AnythingCanBeWorkedOn()
        {
            if (plan.NobodyWaitsForAnything)
            {
                return RemainingEverywhere > 0;
            }

            for (var row = 0; row < remainingOfRow.Length; row++)
            {
                if (ReadyToBeWorkedOn(row))
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
        public ReadOnlySpan<int> RowsReadyToBeWorkedOnBy(int teamIndex)
        {
            var rowsOfTeam = plan.RowsOf(teamIndex);
            var howMany = 0;

            for (var index = 0; index < rowsOfTeam.Length; index++)
            {
                if (ReadyToBeWorkedOn(rowsOfTeam[index]))
                {
                    rowsReadyToBeWorkedOn[howMany] = rowsOfTeam[index];
                    howMany++;
                }
            }

            return rowsReadyToBeWorkedOn.AsSpan(0, howMany);
        }

        /// <returns>True when that was the last item of the row, which is the day the row finished.</returns>
        public bool CloseOneItemOf(int rowIndex)
        {
            remainingOfRow[rowIndex] -= 1;
            remainingOfTeam[plan.TeamOf(rowIndex)] -= 1;
            RemainingEverywhere -= 1;

            return remainingOfRow[rowIndex] == 0;
        }
    }
}
