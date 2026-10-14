using System.Globalization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Tests.API.Integration.Reports;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ReportWindowPolicyTest
    {
        private const int KeepsEverything = 0;

        private static readonly DateOnly Today = new(2026, 10, 2);

        [TestCase(14, 0, null)]
        [TestCase(90, 0, null)]
        [TestCase(14, -400, null)]
        [TestCase(13, 0, ReportRefusalCodes.ThenTooShort)]
        [TestCase(1, -30, ReportRefusalCodes.ThenTooShort)]
        [TestCase(0, 0, ReportRefusalCodes.ThenTooShort)]
        [TestCase(-30, 0, ReportRefusalCodes.ThenTooShort)]
        [TestCase(13, 1, ReportRefusalCodes.ThenTooShort)]
        [TestCase(14, 1, ReportRefusalCodes.ThenEndsInTheFuture)]
        [TestCase(90, 365, ReportRefusalCodes.ThenEndsInTheFuture)]
        public void ThenRefusal_names_what_keeps_a_Then_from_being_frozen(int lengthDays, int endDaysFromToday, string? expected)
        {
            var refusal = ReportWindowPolicy.ThenRefusal(Today.AddDays(endDaysFromToday), lengthDays, Today, KeepsEverything);

            Assert.That(refusal, Is.EqualTo(expected));
        }

        // Today is 2 Oct 2026, so a Team keeping 365 days holds finished items from 2 Oct 2025 on.
        [TestCase(365, "2025-12-30", 90, null)]
        [TestCase(365, "2025-12-31", 90, null)]
        [TestCase(365, "2025-12-29", 90, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(365, "2025-11-15", 90, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(365, "2026-10-02", 366, null)]
        [TestCase(365, "2026-10-02", 367, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(365, "2026-10-02", int.MaxValue, ReportRefusalCodes.ThenTooLong)]
        [TestCase(180, "2026-04-18", 14, null)]
        [TestCase(180, "2026-04-17", 14, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(365, "2025-10-01", 13, ReportRefusalCodes.ThenTooShort)]
        [TestCase(365, "2026-10-03", 400, ReportRefusalCodes.ThenEndsInTheFuture)]
        [TestCase(KeepsEverything, "2024-06-30", 90, null)]
        [TestCase(KeepsEverything, "0001-12-31", 365, null)]
        [TestCase(KeepsEverything, "0001-12-31", 366, ReportRefusalCodes.ThenTooLong)]
        [TestCase(KeepsEverything, "2026-10-03", int.MaxValue, ReportRefusalCodes.ThenTooLong)]
        [TestCase(KeepsEverything, "2026-10-03", 13, ReportRefusalCodes.ThenTooShort)]
        public void ThenRefusal_refuses_a_Then_starting_before_the_oldest_kept_day(int doneItemsCutoffDays, string thenEnd, int lengthDays, string? expected)
        {
            var refusal = ReportWindowPolicy.ThenRefusal(Day(thenEnd), lengthDays, Today, doneItemsCutoffDays);

            Assert.That(refusal, Is.EqualTo(expected));
        }

        // The same Then rows the browser's wizard and the server's acceptance suite are checked against.
        [TestCaseSource(nameof(TheThenRowsOfTheBoundaryTable))]
        public void ThenRefusal_answers_every_Then_row_of_the_boundary_table_as_it_says(ReportWindowBoundaryScenarios.BoundaryRow row)
        {
            var bounds = ReportWindowBoundaryScenarios.TheTable.Bounds;
            var today = Day(bounds.Today);
            var doneItemsCutoffDays = row.KeepsEveryFinishedItem ? KeepsEverything : today.DayNumber - Day(bounds.EarliestStart!).DayNumber;

            var refusal = ReportWindowPolicy.ThenRefusal(Day(row.End!), row.LengthDays, today, doneItemsCutoffDays);

            Assert.That(refusal, Is.EqualTo(row.Refusal));
        }

        private static IEnumerable<TestCaseData> TheThenRowsOfTheBoundaryTable()
            => ReportWindowBoundaryScenarios.TheTable.Rows
                .Where(row => row.IsThen)
                .Select(row => new TestCaseData(row).SetName($"ThenRefusal_answers_every_Then_row_of_the_boundary_table_as_it_says({row.Case})"));

        // The same Now rows the browser's wizard and the server's acceptance suite are checked against.
        [TestCaseSource(nameof(TheNowRowsOfTheBoundaryTable))]
        public void NowRefusal_answers_every_Now_row_of_the_boundary_table_as_it_says(ReportWindowBoundaryScenarios.BoundaryRow row)
        {
            var bounds = ReportWindowBoundaryScenarios.TheTable.Bounds;
            var today = Day(bounds.Today);
            var doneItemsCutoffDays = row.KeepsEveryFinishedItem ? KeepsEverything : today.DayNumber - Day(bounds.EarliestStart!).DayNumber;

            var refusal = ReportWindowPolicy.NowRefusal(row.LengthDays, today, doneItemsCutoffDays);

            Assert.That(refusal, Is.EqualTo(row.Refusal));
        }

        private static IEnumerable<TestCaseData> TheNowRowsOfTheBoundaryTable()
            => ReportWindowBoundaryScenarios.TheTable.Rows
                .Where(row => !row.IsThen)
                .Select(row => new TestCaseData(row).SetName($"NowRefusal_answers_every_Now_row_of_the_boundary_table_as_it_says({row.Case})"));

        private static DateOnly Day(string yearMonthDay) => DateOnly.ParseExact(yearMonthDay, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        [TestCase(365, "2025-10-02")]
        [TestCase(180, "2026-04-05")]
        [TestCase(1, "2026-10-01")]
        [TestCase(KeepsEverything, null)]
        [TestCase(739_890, "0001-01-01")]
        [TestCase(739_891, "0001-01-01")]
        [TestCase(int.MaxValue, "0001-01-01")]
        public void EarliestStart_follows_how_long_the_owner_keeps_finished_items(int doneItemsCutoffDays, string? expected)
        {
            var earliest = ReportWindowPolicy.EarliestStart(Today, doneItemsCutoffDays);

            Assert.That(earliest?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Is.EqualTo(expected));
        }

        // Today is 2 Oct 2026, so a Team keeping 365 days holds finished items from 2 Oct 2025 on.
        [TestCase(365, "2025-10-02", true)]
        [TestCase(365, "2025-10-03", true)]
        [TestCase(365, "2025-10-01", false)]
        [TestCase(KeepsEverything, "0001-01-01", true)]
        public void ThenRebuildable_holds_while_Then_starts_on_or_after_the_oldest_kept_day(int doneItemsCutoffDays, string thenStart, bool expected)
        {
            Assert.That(ReportWindowPolicy.ThenRebuildable(Day(thenStart), Today, doneItemsCutoffDays), Is.EqualTo(expected));
        }

        // Today is 2 Oct 2026, so a Team keeping 365 days holds finished items from 2 Oct 2025 on.
        [TestCase(365, 14, null)]
        [TestCase(365, 90, null)]
        [TestCase(365, 366, null)]
        [TestCase(365, 13, ReportRefusalCodes.NowTooShort)]
        [TestCase(365, 0, ReportRefusalCodes.NowTooShort)]
        [TestCase(365, -30, ReportRefusalCodes.NowTooShort)]
        [TestCase(365, 367, ReportRefusalCodes.NowBeforeCutoff)]
        [TestCase(365, 500, ReportRefusalCodes.NowBeforeCutoff)]
        [TestCase(180, 181, null)]
        [TestCase(180, 182, ReportRefusalCodes.NowBeforeCutoff)]
        [TestCase(365, int.MaxValue, ReportRefusalCodes.NowTooLong)]
        [TestCase(KeepsEverything, 730, null)]
        [TestCase(KeepsEverything, 13, ReportRefusalCodes.NowTooShort)]
        [TestCase(KeepsEverything, int.MaxValue, ReportRefusalCodes.NowTooLong)]
        public void NowRefusal_refuses_a_Now_too_short_or_starting_before_the_oldest_kept_day(int doneItemsCutoffDays, int lengthDays, string? expected)
        {
            var refusal = ReportWindowPolicy.NowRefusal(lengthDays, Today, doneItemsCutoffDays);

            Assert.That(refusal, Is.EqualTo(expected));
        }

        // From 2 Oct 2026, 739 891 days reach back exactly to 1 Jan of the year 1, the first day the calendar can name.
        [TestCase(739_891, null)]
        [TestCase(739_892, ReportRefusalCodes.NowTooLong)]
        public void NowRefusal_refuses_a_Now_reaching_back_before_the_first_day_the_calendar_can_name(int lengthDays, string? expected)
        {
            var refusal = ReportWindowPolicy.NowRefusal(lengthDays, Today, KeepsEverything);

            Assert.That(refusal, Is.EqualTo(expected));
        }

        [Test]
        public void NowWindow_of_a_single_day_is_today_alone()
        {
            Assert.That(ReportWindowPolicy.NowWindow(Today, 1), Is.EqualTo(new ReportWindow(Today, Today, 1)));
        }
    }
}
