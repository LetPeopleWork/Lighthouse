using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// Everything about a forecast that is the same in every simulated run, worked out once and then never
    /// written to again: which rows there are, which Team each belongs to, how much work each starts with,
    /// and which rows have to reach zero before another may be worked on.
    ///
    /// It is read-only for the whole forecast, which is what lets ten thousand simulated runs read it at the
    /// same time without a copy each and without anything to race over. What changes during a run lives in
    /// <see cref="TrialState"/>, which belongs to the run that made it.
    ///
    /// Rows are held in the order they were handed in, because that order decides which Feature a Team works
    /// on next and it is the order the product has always used.
    /// </summary>
    public sealed class ForecastRunPlan
    {
        private static readonly int[] NothingToWaitFor = [];

        private readonly SimulationResult[] rows;
        private readonly int[] teamOfRow;
        private readonly int[] initialRemainingOfRow;
        private readonly int[][] blockersOfRow;
        private readonly int[][] rowsOfTeam;
        private readonly Team[] teams;
        private readonly RunChartData[] throughputOfTeam;

        private ForecastRunPlan(
            SimulationResult[] rows,
            int[] teamOfRow,
            int[] initialRemainingOfRow,
            int[][] blockersOfRow,
            int[][] rowsOfTeam,
            Team[] teams,
            RunChartData[] throughputOfTeam)
        {
            this.rows = rows;
            this.teamOfRow = teamOfRow;
            this.initialRemainingOfRow = initialRemainingOfRow;
            this.blockersOfRow = blockersOfRow;
            this.rowsOfTeam = rowsOfTeam;
            this.teams = teams;
            this.throughputOfTeam = throughputOfTeam;
        }

        public int RowCount => rows.Length;

        public int TeamCount => teams.Length;

        /// <summary>
        /// Whether anything in this forecast waits on anything at all. Almost every forecast answers no, and
        /// the run asks whether it is stuck often enough that being able to skip the question matters.
        /// </summary>
        public bool NobodyWaitsForAnything { get; private init; }

        /// <summary>
        /// Only rows whose Team has measured delivery take part. A Team with nothing measured is left out of
        /// the run exactly as it always has been, and its Features fall back to what they reported before.
        /// </summary>
        public static ForecastRunPlan For(
            IReadOnlyList<SimulationResult> allRows,
            IReadOnlyDictionary<int, RunChartData> throughputByTeam,
            ForecastWaits waits)
        {
            var takingPart = allRows.Where(row => row.Team is not null && throughputByTeam.ContainsKey(row.Team.Id)).ToArray();

            var teams = takingPart.Select(row => row.Team).Distinct().ToArray();
            var placeOfTeam = teams
                .Select((team, index) => (team, index))
                .ToDictionary(entry => entry.team, entry => entry.index);

            var teamOfRow = takingPart.Select(row => placeOfTeam[row.Team]).ToArray();

            return new ForecastRunPlan(
                takingPart,
                teamOfRow,
                takingPart.Select(row => row.InitialRemainingItems).ToArray(),
                WhatEachRowWaitsFor(takingPart, waits),
                TheRowsOfEachTeam(teamOfRow, teams.Length),
                teams,
                teams.Select(team => throughputByTeam[team.Id]).ToArray())
            {
                NobodyWaitsForAnything = waits.NobodyWaitsForAnything,
            };
        }

        /// <summary>
        /// Kept in the order the rows were handed in, because that order decides which Feature a Team works
        /// on next and it is the order the product has always used.
        /// </summary>
        private static int[][] TheRowsOfEachTeam(int[] teamOfRow, int howManyTeams)
        {
            var rowsOfTeam = Enumerable.Range(0, howManyTeams).Select(_ => new List<int>()).ToArray();

            for (var row = 0; row < teamOfRow.Length; row++)
            {
                rowsOfTeam[teamOfRow[row]].Add(row);
            }

            return rowsOfTeam.Select(rows => rows.ToArray()).ToArray();
        }

        public SimulationResult RowAt(int rowIndex) => rows[rowIndex];

        public int TeamOf(int rowIndex) => teamOfRow[rowIndex];

        public int InitialRemainingOf(int rowIndex) => initialRemainingOfRow[rowIndex];

        public int[] MustFinishFirst(int rowIndex) => blockersOfRow[rowIndex];

        public int[] RowsOf(int teamIndex) => rowsOfTeam[teamIndex];

        public Team TeamAt(int teamIndex) => teams[teamIndex];

        public RunChartData ThroughputOf(int teamIndex) => throughputOfTeam[teamIndex];

        /// <summary>
        /// Which rows in the whole run have to reach zero before a row may be worked on. Every row of a
        /// Feature waited on counts, and it is looked up across all the Teams rather than within one of them:
        /// a Feature two Teams share is not finished when the first of them stops.
        ///
        /// A Feature waited on with no row here has already finished, or is not part of this run. Either way
        /// there is nothing to wait for and it holds nobody up.
        /// </summary>
        private static int[][] WhatEachRowWaitsFor(SimulationResult[] rows, ForecastWaits waits)
        {
            if (waits.NobodyWaitsForAnything)
            {
                return rows.Select(_ => NothingToWaitFor).ToArray();
            }

            var rowsOfFeature = Enumerable
                .Range(0, rows.Length)
                .Where(row => rows[row].Feature is not null)
                .GroupBy(row => rows[row].Feature.ReferenceId, StringComparer.Ordinal)
                .ToDictionary(byReferenceId => byReferenceId.Key, byReferenceId => byReferenceId.ToArray(), StringComparer.Ordinal);

            return Enumerable
                .Range(0, rows.Length)
                .Select(row => rows[row].Feature is null
                    ? NothingToWaitFor
                    : waits.Of(rows[row].Feature.ReferenceId)
                        .SelectMany(blocker => rowsOfFeature.GetValueOrDefault(blocker, NothingToWaitFor))
                        .ToArray())
                .ToArray();
        }
    }
}
