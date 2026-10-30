using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class ReportsControllerTest
    {
        private Mock<IReportBulkCommands> bulkCommands = null!;
        private Mock<IReportBulkRemover> bulkRemover = null!;
        private IReadOnlyList<ReportOwner>? askedFor;

        [SetUp]
        public void SetUp()
        {
            askedFor = null;
            bulkRemover = new Mock<IReportBulkRemover>();
            bulkCommands = new Mock<IReportBulkCommands>();
            bulkCommands
                .Setup(commands => commands.CreateForEach(It.IsAny<IReadOnlyList<ReportOwner>>(), It.IsAny<NewReport>(), It.IsAny<IReportOwnerWriteAccess>()))
                .Callback<IReadOnlyList<ReportOwner>, NewReport, IReportOwnerWriteAccess>((owners, _, _) => askedFor = owners)
                .ReturnsAsync((IReadOnlyList<ReportOwner> owners, NewReport _, IReportOwnerWriteAccess _) => [.. owners.Select(owner => ReportOwnerOutcome.Created(owner, 1))]);
        }

        [Test]
        public async Task A_run_naming_one_Team_twice_counts_it_once_against_the_ceiling_and_keeps_its_first_place()
        {
            var distinct = Enumerable.Range(1, IReportBulkCommands.MostOwnersPerRun).Select(ReportOwner.Team).ToList();
            var named = new List<ReportOwner>([.. distinct, distinct[0]]);

            var answer = await Subject().CreateForEach(ARunFor(named.Select(owner => (ReportOwnerKind?)owner.Kind), named.Select(owner => owner.Id)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Result, Is.InstanceOf<OkObjectResult>());
                Assert.That(askedFor, Is.EqualTo(distinct));
            }
        }

        [Test]
        public async Task An_owner_kind_sent_as_an_undefined_number_refuses_the_whole_run()
        {
            var answer = await Subject().CreateForEach(ARunFor([ReportOwnerKind.Team, (ReportOwnerKind)99], [1, 2]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Result, Is.InstanceOf<ObjectResult>().With.Property(nameof(ObjectResult.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
                bulkCommands.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task A_run_naming_no_owners_at_all_is_refused_saying_a_run_needs_one()
        {
            var noOwners = ARunFor([], []);
            noOwners.Owners = null;

            var answer = await Subject().CreateForEach(noOwners);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DetailOf(answer), Does.StartWith("A run names at least one owner"));
                bulkCommands.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task A_run_over_the_ceiling_is_refused_naming_the_ceiling()
        {
            var tooMany = Enumerable.Range(1, IReportBulkCommands.MostOwnersPerRun + 1).ToList();

            var answer = await Subject().CreateForEach(ARunFor(tooMany.Select(_ => (ReportOwnerKind?)ReportOwnerKind.Team), tooMany));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DetailOf(answer), Is.EqualTo($"A run holds at most {IReportBulkCommands.MostOwnersPerRun} Teams and Portfolios."));
                bulkCommands.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task A_removal_naming_no_report_is_refused_saying_a_removal_needs_one()
        {
            var answer = await Subject().RemoveEach(new ReportRemovalRequest { ReportIds = [] });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DetailOf(answer), Is.EqualTo("A removal names at least one report."));
                bulkRemover.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task A_removal_over_the_ceiling_is_refused_naming_the_ceiling()
        {
            var tooMany = Enumerable.Range(1, IReportBulkCommands.MostOwnersPerRun + 1).ToList();

            var answer = await Subject().RemoveEach(new ReportRemovalRequest { ReportIds = tooMany });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DetailOf(answer), Is.EqualTo($"A removal holds at most {IReportBulkCommands.MostOwnersPerRun} reports."));
                bulkRemover.VerifyNoOtherCalls();
            }
        }

        private static string? DetailOf<T>(ActionResult<T> answer)
            => ((answer.Result as ObjectResult)?.Value as ProblemDetails)?.Detail;

        private static NewReportsForOwnersRequest ARunFor(IEnumerable<ReportOwnerKind?> kinds, IEnumerable<int> ids)
            => new()
            {
                TemplateKey = "then-and-now",
                Name = "The quarter",
                ShownMetricKeys = ["cycle-time"],
                ThenAndNow = new NewThenAndNowRequest { ThenEndDate = new DateOnly(2026, 6, 30), ThenLengthDays = 90 },
                Owners = [.. kinds.Zip(ids, (kind, id) => new ReportOwnerRequest { OwnerKind = kind, OwnerId = id })],
            };

        private ReportsController Subject()
            => new(Mock.Of<IReportQueries>(), Mock.Of<IReportOwnerLookup>(), bulkCommands.Object, bulkRemover.Object, Mock.Of<IRbacAdministrationService>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().AddMvcCore().Services.BuildServiceProvider() },
                },
            };
    }
}
