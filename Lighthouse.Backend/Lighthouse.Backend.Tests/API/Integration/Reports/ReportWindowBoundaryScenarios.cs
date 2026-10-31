using System.Globalization;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// The edges of a report window, read from the one table the browser's wizard is checked against too: one
    /// day short of the shortest window, the last day today and tomorrow, the first day on the oldest kept day
    /// and the day before. The wizard refuses these before anything is sent, but the server is the rule's only
    /// authority, so a row answered differently here and in the browser fails one of the two suites.
    ///
    /// Every row is set on 2 Oct 2026 for a Team keeping finished Work Items for 365 days, the bounds the table
    /// itself names - except the rows marked as keeping every finished Work Item, which have no oldest kept day.
    ///
    /// Driving ports: GET /teams/{id}/reports/templates, POST /teams/{id}/reports.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    public class ReportWindowBoundaryScenarios : ReportsAcceptanceTest
    {
        private const string BoundaryTable = "Lighthouse.Frontend/src/tests/reportWindowBoundaries.json";

        [TestCaseSource(nameof(TheRowsOfTheBoundaryTable))]
        public async Task A_window_at_an_edge_is_answered_as_the_table_says(BoundaryRow row)
        {
            var lightspeed = ATeam(Lightspeed, keepsFinishedItemsForDays: row.KeepsEveryFinishedItem ? 0 : 365);

            TheCallerAdministers(lightspeed);
            var templates = await TheTypesOfReportFor(lightspeed);
            using var answer = await CreateAReport(lightspeed, AsNewReport(row));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(templates, "today"), Is.EqualTo(TheTable.Bounds.Today), "the table and the server agree on today");
                Assert.That(TextOf(templates, "earliestStart"), Is.EqualTo(row.KeepsEveryFinishedItem ? null : TheTable.Bounds.EarliestStart),
                    "and on the oldest kept day, which a Team keeping every finished Work Item does not have");
                Assert.That(templates.GetProperty("minimumLengthDays").GetInt32(), Is.EqualTo(TheTable.Bounds.MinimumLengthDays), "and on the shortest window");
            }

            if (row.Refusal is null)
            {
                await ThenItWasCreated(answer);
            }
            else
            {
                await ThenItIsRefusedFor(answer, row.Refusal);
            }
        }

        // --- The shared table ---

        // Declared before TheTable: static fields initialise in the order they are written.
        private static readonly JsonSerializerOptions TableReading = new() { PropertyNameCaseInsensitive = true };

        internal static readonly BoundaryTableFile TheTable = ReadTheTable();

        private static IEnumerable<TestCaseData> TheRowsOfTheBoundaryTable()
            => TheTable.Rows.Select(row => new TestCaseData(row).SetName($"A_window_at_an_edge_is_answered_as_the_table_says({row.Case})"));

        private static BoundaryTableFile ReadTheTable()
        {
            var path = Path.Combine(RepositoryRoot(), BoundaryTable.Replace('/', Path.DirectorySeparatorChar));
            return JsonSerializer.Deserialize<BoundaryTableFile>(File.ReadAllText(path), TableReading)
                ?? throw new InvalidOperationException($"{BoundaryTable} holds no table");
        }

        public sealed record BoundaryTableFile(BoundaryBounds Bounds, IReadOnlyList<BoundaryRow> Rows);

        public sealed record BoundaryBounds(string Today, string? EarliestStart, int MinimumLengthDays);

        public sealed record BoundaryRow(string Case, string Side, string? End, int LengthDays, string? Refusal, bool KeepsEveryFinishedItem = false)
        {
            public bool IsThen => Side == "then";

            public override string ToString() => Case;
        }

        /// <summary>A Then row moves Then and leaves Now at 90 days; a Now row keeps Then at the 90 days to today.</summary>
        private static NewReport AsNewReport(BoundaryRow row) => row.IsThen
            ? new NewReport
            {
                ThenEnd = DateOnly.ParseExact(row.End ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ThenLengthDays = row.LengthDays,
                NowLengthDays = 90,
            }
            : new NewReport { NowLengthDays = row.LengthDays };
    }
}
