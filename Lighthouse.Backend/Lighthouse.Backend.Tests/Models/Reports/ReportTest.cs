using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Tests.Models.Reports
{
    [TestFixture]
    public class ReportTest
    {
        private const int SharedId = 7;

        private static readonly DateTime CreatedAt = new(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc);

        [Test]
        public void A_Team_report_sets_the_Team_column_alone_and_reads_back_as_that_Team()
        {
            var report = Report.For(ReportOwner.Team(SharedId), "then-and-now", "Engagement start", CreatedAt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.TeamId, Is.EqualTo(SharedId));
                Assert.That(report.PortfolioId, Is.Null);
                Assert.That(report.OwnedBy(), Is.EqualTo(ReportOwner.Team(SharedId)));
            }
        }

        [Test]
        public void A_Portfolio_report_sets_the_Portfolio_column_alone_and_reads_back_as_that_Portfolio()
        {
            var report = Report.For(ReportOwner.Portfolio(SharedId), "then-and-now", "Engagement start", CreatedAt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.TeamId, Is.Null);
                Assert.That(report.PortfolioId, Is.EqualTo(SharedId));
                Assert.That(report.OwnedBy(), Is.EqualTo(ReportOwner.Portfolio(SharedId)));
            }
        }
    }
}
