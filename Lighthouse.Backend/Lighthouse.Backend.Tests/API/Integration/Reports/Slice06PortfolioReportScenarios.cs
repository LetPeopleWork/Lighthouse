using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// A Portfolio has Reports too: the same catalog and the same panels, over its Features rather than a Team's
    /// Work Items. Its admin creates them, its readers read them, and nobody reaches one through another owner's
    /// address - another Portfolio's, or a Team's that happens to carry the same number. A report goes when its
    /// Portfolio goes.
    ///
    /// Driving ports: GET/POST /portfolios/{id}/reports, GET /portfolios/{id}/reports/templates,
    /// GET /portfolios/{id}/reports/{reportId}, DELETE /portfolios/{id}.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-06")]
    public class Slice06PortfolioReportScenarios : ReportsAcceptanceTest
    {
        [Test]
        public async Task A_Portfolio_admin_creates_a_Then_and_Now_report_about_its_Features()
        {
            var apollo = GivenApolloFinishedThreeFeaturesInThen();

            var reportId = await GivenTheAdminCreated(apollo, AllFourOverNinetyDays());

            var report = await TheReport(apollo, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(report, "ownerKind"), Is.EqualTo("portfolio").IgnoreCase);
                Assert.That(ThenWindowIn(report), Is.EqualTo(NinetyDaysToToday));
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(34, 3)));
                Assert.That(MetricIn(report, Throughput), Is.Not.Null);
                Assert.That(MetricIn(report, Wip), Is.Not.Null);
                Assert.That(MetricIn(report, WorkItemAge), Is.Not.Null);
            }
        }

        [Test]
        public async Task A_Portfolio_can_be_reported_on_all_four_metrics()
        {
            var apollo = APortfolio(Apollo);

            TheCallerAdministers(apollo);
            var templates = await TheTypesOfReportFor(apollo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(MetricKeysOfThenAndNowIn(templates), Is.EquivalentTo(AllFourMetrics));
                Assert.That(TextOf(templates, "earliestStart"), Is.EqualTo("2025-10-02"));
            }
        }

        [Test]
        public async Task A_Portfolio_with_no_finished_Features_shows_a_dash_and_the_reason()
        {
            var orion = APortfolio("Project Orion");

            var reportId = await GivenTheAdminCreated(orion, AllFourOverNinetyDays());

            ThenItIsADashFor(ThenValueIn(await TheReport(orion, reportId), CycleTime, P85), Reason.NoFinishedItems);
        }

        [Test]
        public async Task A_Portfolio_reader_finds_the_report_and_cannot_create_one()
        {
            var apollo = GivenApolloFinishedThreeFeaturesInThen();
            await GivenTheAdminCreated(apollo, AllFourOverNinetyDays());

            TheCallerOnlyReads(apollo);
            var names = ReportNamesIn(await TheReportsListOf(apollo));
            using var refused = await CreateAReport(apollo, AllFourOverNinetyDays());

            Assert.That(names, Is.EqualTo(TheEngagementStartAlone));
            await ThenItIsForbidden(refused);
        }

        [Test]
        public async Task A_report_reached_through_another_Portfolios_address_is_not_found()
        {
            var apollo = GivenApolloFinishedThreeFeaturesInThen();
            var orion = APortfolio("Project Orion");
            var reportId = await GivenTheAdminCreated(apollo, AllFourOverNinetyDays());

            TheCallerAdministersTheInstance();
            using var throughApollo = await AskForTheReport(apollo, reportId);
            using var throughOrion = await AskForTheReport(orion, reportId);

            Assert.That(throughApollo.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the same caller opens it through Apollo");
            await ThenItIsNotFound(throughOrion);
        }

        [Test]
        public async Task A_Teams_report_cannot_be_reached_through_a_Portfolios_address()
        {
            var lightspeed = ATeam(Lightspeed);
            var apollo = APortfolio(Apollo);
            var reportId = await GivenTheAdminCreated(lightspeed, AllFourOverNinetyDays());

            TheCallerAdministersTheInstance();
            using var throughTheTeam = await AskForTheReport(lightspeed, reportId);
            using var throughThePortfolio = await AskForTheReport(apollo, reportId);

            Assert.That(throughTheTeam.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await ThenItIsNotFound(throughThePortfolio);
        }

        [Test]
        public async Task Deleting_a_Portfolio_deletes_its_reports()
        {
            var apollo = GivenApolloFinishedThreeFeaturesInThen();
            await GivenTheAdminCreated(apollo, AllFourOverNinetyDays());
            var storedBefore = StoredReports();

            TheCallerAdministersTheInstance();
            using var deleted = await DeleteTheOwner(apollo);
            var answer = await deleted.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted.IsSuccessStatusCode, Is.True, answer);
                Assert.That(storedBefore, Is.EqualTo(1));
                Assert.That(StoredReports(), Is.Zero);
            }
        }

        // --- Steps ---

        private static readonly string[] AllFourMetrics = [CycleTime, Throughput, Wip, WorkItemAge];

        private static readonly string?[] TheEngagementStartAlone = [EngagementStart];

        private static NewReport AllFourOverNinetyDays() => new() { Shown = AllFourMetrics };

        /// <summary>Three Features finished between 5 Jul and 2 Oct 2026, taking 20, 34 and 51 days: the 85th percentile is 34.</summary>
        private OwnerUnderTest GivenApolloFinishedThreeFeaturesInThen()
        {
            var apollo = APortfolio(Apollo);
            ThePortfolioFinished(apollo,
                new Finished(new DateOnly(2026, 7, 20), 20),
                new Finished(new DateOnly(2026, 8, 25), 34),
                new Finished(new DateOnly(2026, 9, 28), 51));
            return apollo;
        }
    }
}
