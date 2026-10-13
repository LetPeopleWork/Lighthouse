using System.Globalization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;

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
        [TestCase(365, "2026-10-02", int.MaxValue, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(180, "2026-04-18", 14, null)]
        [TestCase(180, "2026-04-17", 14, ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(365, "2025-10-01", 13, ReportRefusalCodes.ThenTooShort)]
        [TestCase(365, "2026-10-03", 400, ReportRefusalCodes.ThenEndsInTheFuture)]
        [TestCase(KeepsEverything, "2024-06-30", 90, null)]
        [TestCase(KeepsEverything, "0001-12-31", 365, null)]
        public void ThenRefusal_refuses_a_Then_starting_before_the_oldest_kept_day(int doneItemsCutoffDays, string thenEnd, int lengthDays, string? expected)
        {
            var refusal = ReportWindowPolicy.ThenRefusal(DateOnly.Parse(thenEnd, CultureInfo.InvariantCulture), lengthDays, Today, doneItemsCutoffDays);

            Assert.That(refusal, Is.EqualTo(expected));
        }

        [TestCase(365, "2025-10-02")]
        [TestCase(180, "2026-04-05")]
        [TestCase(1, "2026-10-01")]
        [TestCase(KeepsEverything, null)]
        public void EarliestStart_follows_how_long_the_owner_keeps_finished_items(int doneItemsCutoffDays, string? expected)
        {
            var earliest = ReportWindowPolicy.EarliestStart(Today, doneItemsCutoffDays);

            Assert.That(earliest?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Is.EqualTo(expected));
        }
    }
}
