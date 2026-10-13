using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ReportWindowPolicyTest
    {
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
            var refusal = ReportWindowPolicy.ThenRefusal(Today.AddDays(endDaysFromToday), lengthDays, Today);

            Assert.That(refusal, Is.EqualTo(expected));
        }
    }
}
