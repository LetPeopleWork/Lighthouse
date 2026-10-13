using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    [TestFixture]
    public class ReportRepositoryTest
    {
        private const int SharedId = 7;

        private static readonly DateTime CreatedAt = new(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc);

        private LighthouseAppContext context = null!;
        private ReportRepository subject = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
            subject = new ReportRepository(context);
        }

        [TearDown]
        public void TearDown()
        {
            context.Dispose();
        }

        [Test]
        public async Task An_owner_lists_its_reports_oldest_first()
        {
            var first = await Stored(ReportOwner.Team(SharedId), "First");
            var second = await Stored(ReportOwner.Team(SharedId), "Second");
            var third = await Stored(ReportOwner.Team(SharedId), "Third");

            int[] oldestFirst = [first.Id, second.Id, third.Id];

            var listed = subject.ListForOwner(ReportOwner.Team(SharedId)).Select(report => report.Id);

            Assert.That(listed, Is.EqualTo(oldestFirst));
        }

        [Test]
        public async Task A_Portfolio_lists_and_opens_its_own_reports_never_those_of_the_Team_sharing_its_id()
        {
            var teamReport = await Stored(ReportOwner.Team(SharedId), "The Team's");
            var portfolioReport = await Stored(ReportOwner.Portfolio(SharedId), "The Portfolio's");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ListForOwner(ReportOwner.Portfolio(SharedId)).Select(report => report.Id), Is.EqualTo((int[])[portfolioReport.Id]));
                Assert.That(subject.ListForOwner(ReportOwner.Team(SharedId)).Select(report => report.Id), Is.EqualTo((int[])[teamReport.Id]));
                Assert.That(subject.GetForOwner(ReportOwner.Portfolio(SharedId), portfolioReport.Id)?.Name, Is.EqualTo("The Portfolio's"));
                Assert.That(subject.GetForOwner(ReportOwner.Portfolio(SharedId), teamReport.Id), Is.Null);
            }
        }

        private async Task<Report> Stored(ReportOwner owner, string name)
        {
            var report = Report.For(owner, "then-and-now", name, CreatedAt);
            subject.Add(report);
            await subject.Save();
            return report;
        }
    }
}
