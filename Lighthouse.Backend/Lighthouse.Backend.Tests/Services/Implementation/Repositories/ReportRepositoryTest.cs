using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
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

        [Test]
        public async Task Several_owners_of_both_kinds_list_their_reports_in_one_read_leaving_other_owners_out()
        {
            var team = await Stored(ReportOwner.Team(SharedId), "The Team's");
            var portfolio = await Stored(ReportOwner.Portfolio(SharedId + 1), "The Portfolio's");
            await Stored(ReportOwner.Team(SharedId + 1), "Another Team's");
            await Stored(ReportOwner.Portfolio(SharedId), "Another Portfolio's");

            var listed = subject.ListForOwners([ReportOwner.Team(SharedId), ReportOwner.Portfolio(SharedId + 1)]);

            Assert.That(listed.Select(report => report.Id), Is.EqualTo((int[])[team.Id, portfolio.Id]));
        }

        [Test]
        public async Task No_owners_list_no_reports()
        {
            await Stored(ReportOwner.Team(SharedId), "The Team's");

            Assert.That(subject.ListForOwners([]), Is.Empty);
        }

        [Test]
        public async Task A_report_names_its_own_owner_and_an_unknown_report_names_none()
        {
            var teamReport = await Stored(ReportOwner.Team(SharedId), "The Team's");
            var portfolioReport = await Stored(ReportOwner.Portfolio(SharedId), "The Portfolio's");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.OwnerOf(teamReport.Id), Is.EqualTo(ReportOwner.Team(SharedId)));
                Assert.That(subject.OwnerOf(portfolioReport.Id), Is.EqualTo(ReportOwner.Portfolio(SharedId)));
                Assert.That(subject.OwnerOf(portfolioReport.Id + 1), Is.Null);
            }
        }

        [Test]
        public async Task Discarding_pending_changes_forgets_what_was_not_saved_and_keeps_what_was()
        {
            var stored = await Stored(ReportOwner.Team(SharedId), "Stored");
            stored.Name = "Renamed but never saved";
            subject.Add(Report.For(ReportOwner.Team(SharedId), "then-and-now", "Never saved", CreatedAt));

            subject.DiscardPendingChanges();
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ListForOwner(ReportOwner.Team(SharedId)).Select(report => report.Name), Is.EqualTo((string[])["Stored"]));
                Assert.That(context.ChangeTracker.Entries<Report>().Select(entry => (entry.Entity.Id, entry.State)), Is.EqualTo(new[] { (stored.Id, EntityState.Unchanged) }));
            }
        }

        [Test]
        public async Task Discarding_pending_changes_leaves_a_report_marked_for_removal_in_place()
        {
            var stored = await Stored(ReportOwner.Team(SharedId), "Stored");
            subject.Remove(ReportOwner.Team(SharedId), stored.Id);

            subject.DiscardPendingChanges();
            await subject.Save();

            Assert.That(subject.ListForOwner(ReportOwner.Team(SharedId)).Select(report => report.Id), Is.EqualTo(new[] { stored.Id }));
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
