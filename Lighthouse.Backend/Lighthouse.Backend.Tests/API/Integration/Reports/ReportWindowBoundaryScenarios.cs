using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models.Reports;

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
    /// Driving ports: the types of report a Team may make, and making one. The status and the JSON text of the
    /// route are pinned by the hosted Reports scenarios.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    public class ReportWindowBoundaryScenarios : ReportsAtThePortTest
    {
        private const string BoundaryTable = "Lighthouse.Frontend/src/tests/reportWindowBoundaries.json";

        private const string Lightspeed = "Team Lightspeed";

        private const string ThenAndNowTemplate = "then-and-now";

        private const string EngagementStart = "Then & Now — engagement start";

        private const string CycleTime = "cycle-time";

        [TestCaseSource(nameof(TheRowsOfTheBoundaryTable))]
        public async Task A_window_at_an_edge_is_answered_as_the_table_says(BoundaryRow row)
        {
            var lightspeed = ATeam(Lightspeed, keepsFinishedItemsForDays: row.KeepsEveryFinishedItem ? 0 : 365);

            var templates = TheTypesOfReportFor(lightspeed);
            var answer = await CreateAReport(lightspeed, AsNewReport(row));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(templates.Status, Is.EqualTo(ReportOutcomeStatus.Done), "the Team's types of report are on offer");
                Assert.That(Day(templates.Value?.Today), Is.EqualTo(TheTable.Bounds.Today), "the table and the server agree on today");
                Assert.That(Day(templates.Value?.EarliestStart), Is.EqualTo(row.KeepsEveryFinishedItem ? null : TheTable.Bounds.EarliestStart),
                    "and on the oldest kept day, which a Team keeping every finished Work Item does not have");
                Assert.That(templates.Value?.MinimumLengthDays, Is.EqualTo(TheTable.Bounds.MinimumLengthDays), "and on the shortest window");
            }

            if (row.Refusal is null)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answer.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report was not created: {answer.Code}");
                    Assert.That(answer.Value?.Id, Is.GreaterThan(0), "a created report is handed back with its id");
                }
            }
            else
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answer.Status, Is.EqualTo(ReportOutcomeStatus.Refused), "a window outside the table's bounds is refused");
                    Assert.That(answer.Code, Is.EqualTo(row.Refusal), "the refusal must name its reason from the closed list");
                }
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

        /// <summary>The checkout's root, where the tables shared with the browser's tests live.</summary>
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return Directory.GetParent(directory?.FullName ?? throw new InvalidOperationException("Lighthouse.sln could not be found to anchor the tables shared with the browser."))!.FullName;
        }

        private static string? Day(DateOnly? day) => day?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public sealed record BoundaryTableFile(BoundaryBounds Bounds, IReadOnlyList<BoundaryRow> Rows);

        public sealed record BoundaryBounds(string Today, string? EarliestStart, int MinimumLengthDays);

        public sealed record BoundaryRow(string Case, string Side, string? End, int LengthDays, string? Refusal, bool KeepsEveryFinishedItem = false)
        {
            public bool IsThen => Side == "then";

            public override string ToString() => Case;
        }

        /// <summary>A Then row moves Then and leaves Now at 90 days; a Now row keeps Then at the 90 days to today.</summary>
        private static NewReport AsNewReport(BoundaryRow row) => row.IsThen
            ? new NewReport(ThenAndNowTemplate, EngagementStart, [CycleTime],
                new NewThenAndNow(DateOnly.ParseExact(row.End ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture), row.LengthDays, 90))
            : new NewReport(ThenAndNowTemplate, EngagementStart, [CycleTime],
                new NewThenAndNow(new DateOnly(2026, 10, 2), 90, row.LengthDays));
    }
}
