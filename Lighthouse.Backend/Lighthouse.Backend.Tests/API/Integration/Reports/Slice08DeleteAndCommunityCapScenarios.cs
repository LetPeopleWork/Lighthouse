using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// An admin deletes a report, and it is gone for everybody. Without a Premium licence each Team or Portfolio
    /// holds one report of each type; the list says when the cap is reached so the page does not have to restate
    /// the rule, and the server refuses a second of the same type. A licence that lapses takes nothing away:
    /// every report stays readable and deletable, and creating waits until none of that type is left. A report
    /// whose Then starts before the oldest finished Work Item the owner still keeps says that it can no longer
    /// be rebuilt, so deleting it can warn first.
    ///
    /// Driving ports: GET/POST /teams/{id}/reports, GET/DELETE /teams/{id}/reports/{reportId}, and the same
    /// under /portfolios/{id}.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-08")]
    public class Slice08DeleteAndCommunityCapScenarios : ReportsAcceptanceTest
    {
        private const string HandOver = "Then & Now — hand-over";

        private const string SecondExperiment = "Then & Now — second experiment";

        // @driving_port @real-io @us-08 @slice-08 @kpi-OUT-5878-guardrail-quick-deletes @contract-shape:bounded-change
        [Test]
        public async Task An_admin_deletes_a_report_and_it_is_gone_for_everybody()
        {
            TheInstanceHasAPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            var engagementStart = await GivenTheAdminCreated(lightspeed, new NewReport());
            await GivenTheAdminCreated(lightspeed, new NewReport { Name = HandOver });

            using var deleted = await DeleteTheReport(lightspeed, engagementStart);

            TheCallerOnlyReads(lightspeed);
            var names = ReportNamesIn(await TheReportsListOf(lightspeed));
            using var opened = await AskForTheReport(lightspeed, engagementStart);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(names, Is.EqualTo(TheHandOverAlone));
                Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_reader_cannot_delete_a_report()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerOnlyReads(lightspeed);
            using var refused = await DeleteTheReport(lightspeed, reportId);

            await ThenItIsForbidden(refused);
            Assert.That(ReportNamesIn(await TheReportsListOf(lightspeed)), Is.EqualTo(TheEngagementStartAlone));
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Deleting_through_another_Teams_address_deletes_nothing()
        {
            var lightspeed = ATeam(Lightspeed);
            var zenith = ATeam(Zenith);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerAdministersTheInstance();
            using var refused = await DeleteTheReport(zenith, reportId);

            await ThenItIsNotFound(refused);
            Assert.That(ReportNamesIn(await TheReportsListOf(lightspeed)), Is.EqualTo(TheEngagementStartAlone));
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:bounded-change
        [Test]
        public async Task Deleting_a_report_twice_finds_nothing_the_second_time()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var first = await DeleteTheReport(lightspeed, reportId);
            using var second = await DeleteTheReport(lightspeed, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @kpi-OUT-5878-K4-community-cap-reached @contract-shape:unbounded-preservation
        [Test]
        public async Task Community_keeps_one_Then_and_Now_report_per_Team()
        {
            TheInstanceHasNoPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            TheCallerAdministers(lightspeed);
            var belowTheCap = CreationBlockedByCapIn(await TheReportsListOf(lightspeed));
            await GivenTheAdminCreated(lightspeed, new NewReport());

            var atTheCap = await TheReportsListOf(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport { Name = HandOver });
            var body = await refused.Content.ReadAsStringAsync();
            var stillListed = ReportNamesIn(await TheReportsListOf(lightspeed));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(belowTheCap, Is.False);
                Assert.That(CreationBlockedByCapIn(atTheCap), Is.True);
                Assert.That(atTheCap.GetProperty("communityCap").GetInt32(), Is.EqualTo(1));
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
                Assert.That(body, Does.Contain(Reason.ReportCapReached));
                Assert.That(stillListed, Is.EqualTo(TheEngagementStartAlone));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @boundary @contract-shape:bounded-change
        // Each type has its own place, so a Team holding a report of a type a later version added can still
        // create its one Then & Now report.
        [Test]
        public async Task A_report_of_another_type_does_not_take_the_Then_and_Now_place()
        {
            TheInstanceHasNoPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            var later = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredReportBecomesOfType(later, "signal");

            var blocked = CreationBlockedByCapIn(await TheReportsListOf(lightspeed));
            using var created = await CreateAReport(lightspeed, new NewReport { Name = HandOver });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blocked, Is.False);
                Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created), await created.Content.ReadAsStringAsync());
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @boundary @contract-shape:bounded-change
        [Test]
        public async Task The_cap_is_per_Team()
        {
            TheInstanceHasNoPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            var zenith = ATeam(Zenith);
            await GivenTheAdminCreated(lightspeed, new NewReport());

            TheCallerAdministers(zenith);
            using var created = await CreateAReport(zenith, new NewReport());

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created), await created.Content.ReadAsStringAsync());
        }

        // @driving_port @real-io @us-08 @slice-08 @contract-shape:bounded-change
        [Test]
        public async Task Premium_creates_as_many_reports_as_it_needs()
        {
            TheInstanceHasAPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            await GivenFourReports(lightspeed);

            TheCallerAdministers(lightspeed);
            using var fifth = await CreateAReport(lightspeed, new NewReport { Name = "Fifth" });

            var body = await fifth.Content.ReadAsStringAsync();
            var blocked = CreationBlockedByCapIn(await TheReportsListOf(lightspeed));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fifth.StatusCode, Is.EqualTo(HttpStatusCode.Created), body);
                Assert.That(blocked, Is.False);
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:bounded-change
        [Test]
        public async Task A_lapsed_licence_keeps_every_report_readable_and_deletable_but_stops_creating()
        {
            TheInstanceHasAPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            var reports = await GivenFourReports(lightspeed);

            ThePremiumLicenceLapses();
            TheCallerOnlyReads(lightspeed);
            var opened = new List<HttpStatusCode>();
            foreach (var reportId in reports)
            {
                using var response = await AskForTheReport(lightspeed, reportId);
                opened.Add(response.StatusCode);
            }

            TheCallerAdministers(lightspeed);
            using var deleted = await DeleteTheReport(lightspeed, reports[0]);
            var afterOneDelete = await TheReportsListOf(lightspeed);
            using var refused = await CreateAReport(lightspeed, new NewReport());
            var refusal = await refused.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(opened, Is.All.EqualTo(HttpStatusCode.OK));
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), refusal);
                Assert.That(refusal, Does.Contain(Reason.ReportCapReached));
                Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(ReportsIn(afterOneDelete), Has.Count.EqualTo(3));
                Assert.That(CreationBlockedByCapIn(afterOneDelete), Is.True, "three reports are still above Community's one");
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Deleting_the_report_frees_its_place_for_a_new_one()
        {
            TheInstanceHasNoPremiumLicence();
            var lightspeed = ATeam(Lightspeed);
            var first = await GivenTheAdminCreated(lightspeed, new NewReport());

            using var deleted = await DeleteTheReport(lightspeed, first);
            var afterDelete = CreationBlockedByCapIn(await TheReportsListOf(lightspeed));
            using var created = await CreateAReport(lightspeed, new NewReport { Name = HandOver });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(afterDelete, Is.False);
                Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @boundary @contract-shape:pure-function
        // Then starts on 2 Oct 2025, the oldest day kept on 2 Oct 2026; a day later it is one day too old.
        [Test]
        public async Task A_Then_older_than_what_the_Team_keeps_says_it_can_no_longer_be_rebuilt()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { ThenEnd = new DateOnly(2025, 12, 30) });
            var onTheDayItWasCreated = ThenRebuildableIn(await TheReport(lightspeed, reportId));

            TodayIs(TodayOnTheInstance.AddDays(1));

            var report = await TheReport(lightspeed, reportId);
            var listed = ReportsIn(await TheReportsListOf(lightspeed)).Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(onTheDayItWasCreated, Is.True);
                Assert.That(ThenRebuildableIn(report), Is.False);
                Assert.That(ThenRebuildableIn(listed), Is.False,
                    "the list's delete button warns as well, so the list has to know");
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:bounded-change
        [Test]
        public async Task A_report_of_a_type_this_version_does_not_know_can_still_be_deleted()
        {
            var lightspeed = ATeam(Lightspeed);
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport());
            TheStoredReportBecomesOfType(reportId, "signal");

            using var deleted = await DeleteTheReport(lightspeed, reportId);

            Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_Portfolio_reader_cannot_delete_its_report()
        {
            var apollo = APortfolio(Apollo);
            var reportId = await GivenTheAdminCreated(apollo, new NewReport());

            TheCallerOnlyReads(apollo);
            using var refused = await DeleteTheReport(apollo, reportId);

            await ThenItIsForbidden(refused);
            Assert.That(ReportNamesIn(await TheReportsListOf(apollo)), Is.EqualTo(TheEngagementStartAlone));
        }

        // @driving_port @real-io @us-08 @slice-08 @kpi-OUT-5878-guardrail-quick-deletes @contract-shape:bounded-change
        [Test]
        public async Task A_Portfolio_admin_deletes_its_report()
        {
            var apollo = APortfolio(Apollo);
            var reportId = await GivenTheAdminCreated(apollo, new NewReport());

            using var deleted = await DeleteTheReport(apollo, reportId);

            Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await ThenTheOwnerHoldsNoReport(apollo);
        }

        // @driving_port @real-io @us-08 @slice-08 @kpi-OUT-5878-K4-community-cap-reached @contract-shape:unbounded-preservation
        [Test]
        public async Task Community_keeps_one_Then_and_Now_report_per_Portfolio()
        {
            TheInstanceHasNoPremiumLicence();
            var apollo = APortfolio(Apollo);
            await GivenTheAdminCreated(apollo, new NewReport());

            using var refused = await CreateAReport(apollo, new NewReport { Name = HandOver });
            var body = await refused.Content.ReadAsStringAsync();
            var stillListed = await TheReportsListOf(apollo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
                Assert.That(body, Does.Contain(Reason.ReportCapReached));
                Assert.That(CreationBlockedByCapIn(stillListed), Is.True);
                Assert.That(ReportNamesIn(stillListed), Is.EqualTo(TheEngagementStartAlone));
            }
        }

        // --- Steps ---

        private static readonly string?[] TheHandOverAlone = [HandOver];

        private static readonly string?[] TheEngagementStartAlone = [EngagementStart];

        private async Task<List<int>> GivenFourReports(OwnerUnderTest owner)
        {
            var ids = new List<int>();
            foreach (var name in new[] { EngagementStart, HandOver, SecondExperiment, "Fourth" })
            {
                ids.Add(await GivenTheAdminCreated(owner, new NewReport { Name = name }));
            }

            return ids;
        }
    }
}
