using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class TeamReportsControllerTest
    {
        private const int TeamId = 42;

        private const string ReportsAddress = "/api/latest/teams/42/reports";

        private static readonly DateOnly EarliestStart = new(2025, 10, 2);

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(TeamId);

        private Mock<IReportQueries> queries = null!;
        private Mock<IReportCommands> commands = null!;
        private NewReport? requested;

        [SetUp]
        public void SetUp()
        {
            requested = null;
            queries = new Mock<IReportQueries>();
            queries.Setup(q => q.Templates(Lightspeed)).Returns(ReportOutcome<ReportTemplatesRead>.Done(
                new ReportTemplatesRead(new DateOnly(2026, 10, 2), EarliestStart, 14, [])));
            commands = new Mock<IReportCommands>();
            AnswerCreateWith(ReportOutcome<ReportRead>.Done(new ReportRead(17, "Engagement start", "then-and-now", ReportOwnerKind.Team, TeamId, new DateOnly(2026, 10, 2))));
        }

        [Test]
        public async Task A_created_report_answers_with_its_own_address()
        {
            var answer = await Subject().CreateReport(TeamId, AThenAndNowRequest("Engagement start", ["cycle-time"]));

            Assert.That(answer.Result, Is.InstanceOf<CreatedResult>().With.Property(nameof(CreatedResult.Location)).EqualTo($"{ReportsAddress}/17"));
        }

        [Test]
        public async Task The_name_and_shown_metrics_reach_the_report_as_sent()
        {
            await Subject().CreateReport(TeamId, AThenAndNowRequest("Engagement start", ["cycle-time"]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requested!.Name, Is.EqualTo("Engagement start"));
                Assert.That(requested.ShownMetricKeys, Is.EqualTo((string[])["cycle-time"]));
            }
        }

        [Test]
        public async Task A_request_naming_neither_a_name_nor_shown_metrics_asks_for_an_empty_name_and_no_metrics()
        {
            await Subject().CreateReport(TeamId, AThenAndNowRequest(null, null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requested!.Name, Is.Empty);
                Assert.That(requested.ShownMetricKeys, Is.Empty);
            }
        }

        [Test]
        public async Task A_refusal_carries_its_status_and_code_and_no_earliest_start_unless_it_is_about_the_cutoff()
        {
            AnswerCreateWith(ReportOutcome<ReportRead>.Refused(ReportRefusalCodes.NameMissing));

            var problem = ProblemOf(await Subject().CreateReport(TeamId, AThenAndNowRequest(" ", ["cycle-time"])));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
                Assert.That(problem.Extensions["code"], Is.EqualTo(ReportRefusalCodes.NameMissing));
                Assert.That(problem.Extensions, Does.Not.ContainKey("earliestStart"));
            }
        }

        [Test]
        public async Task A_refusal_about_the_cutoff_names_the_earliest_start()
        {
            AnswerCreateWith(ReportOutcome<ReportRead>.Refused(ReportRefusalCodes.ThenBeforeCutoff));

            var problem = ProblemOf(await Subject().CreateReport(TeamId, AThenAndNowRequest("Engagement start", ["cycle-time"])));

            Assert.That(problem.Extensions["earliestStart"], Is.EqualTo(EarliestStart));
        }

        private void AnswerCreateWith(ReportOutcome<ReportRead> outcome)
        {
            commands.Setup(c => c.Create(Lightspeed, It.IsAny<NewReport>()))
                .Callback<ReportOwner, NewReport>((_, request) => requested = request)
                .ReturnsAsync(outcome);
        }

        private static ProblemDetails ProblemOf(ActionResult<ReportRead> answer)
            => (ProblemDetails)((ObjectResult)answer.Result!).Value!;

        private static NewReportRequest AThenAndNowRequest(string? name, List<string>? shownMetricKeys)
            => new()
            {
                TemplateKey = "then-and-now",
                Name = name,
                ShownMetricKeys = shownMetricKeys,
                ThenAndNow = new NewThenAndNowRequest { ThenEndDate = new DateOnly(2026, 10, 2), ThenLengthDays = 90, NowLengthDays = 30 },
            };

        private TeamReportsController Subject()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = ReportsAddress;

            return new TeamReportsController(queries.Object, commands.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
            };
        }
    }
}
