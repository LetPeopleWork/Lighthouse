namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Every report on every Team and Portfolio the caller can read, in one list, each row naming its owner. The
    /// server filters by the caller's read rights; a report on an owner the caller cannot read is never sent. The
    /// per-owner lists answer exactly as before. Listing is Community.
    ///
    /// Driving port: GET /api/latest/reports (and the unchanged GET /teams/{id}/reports).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("slice-01")]
    public class Slice01EveryReportInOnePlaceScenarios : BulkReportsAcceptanceTest
    {
        private static readonly string[] OnlyTheLaterType = ["signals"];

        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-6350-K1-reports-page-used @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task A_lead_reads_every_report_he_can_read_with_its_owner_on_a_Community_instance()
        {
            var gravity = ATeam("Gravity");
            var zenith = ATeam("Zenith");
            var oceanExplorer = APortfolio("Ocean Explorer");
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            await GivenTheOwnerHolds(zenith, TheQuartersComparison);
            await GivenTheOwnerHolds(oceanExplorer, TheMonthsComparison);
            TheCallerOnlyReadsEach(gravity, zenith, oceanExplorer);

            var every = ReportsIn(await EveryReportTheCallerCanRead());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(every.Select(report => (TextOf(report, "name"), TextOf(report, "ownerName"))),
                    Is.EquivalentTo(new[] { (TheQuarter, "Gravity"), (TheQuarter, "Zenith"), (TheMonth, "Ocean Explorer") }));
                Assert.That(OwnersIn(await EveryReportTheCallerCanRead()),
                    Is.EquivalentTo(new[] { OwnerKey(gravity), OwnerKey(zenith), OwnerKey(oceanExplorer) }));
                Assert.That(every.Select(report => TextOf(report, "createdOn")), Has.All.EqualTo(Iso(TodayInTheExamples)));
                Assert.That(every.Select(report => TextOf(report, "templateKey")), Has.All.EqualTo(ThenAndNow));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task A_report_on_a_Portfolio_he_cannot_read_is_never_sent()
        {
            var apollo = APortfolio("Apollo");
            var oceanExplorer = APortfolio("Ocean Explorer");
            await GivenTheOwnerHolds(apollo, TheQuartersComparison);
            await GivenTheOwnerHolds(oceanExplorer, TheQuartersComparison);
            TheCallerOnlyReadsEach(oceanExplorer);

            var owners = OwnersIn(await EveryReportTheCallerCanRead());

            Assert.That(owners, Is.EqualTo(new[] { OwnerKey(oceanExplorer) }));
        }

        // @driving_port @real-io @us-01 @slice-01 @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
        // The test RBAC service's batch Team read answers every id it is given; DELIVER scopes it to the caller's
        // grants, as its Portfolio twin already is, before this scenario can tell a filter from no filter.
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task A_report_on_a_Team_he_cannot_read_is_never_sent()
        {
            var pulsar = ATeam("Pulsar");
            var gravity = ATeam("Gravity");
            await GivenTheOwnerHolds(pulsar, TheQuartersComparison);
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            TheCallerOnlyReadsEach(gravity);

            var owners = OwnersIn(await EveryReportTheCallerCanRead());

            Assert.That(owners, Is.EqualTo(new[] { OwnerKey(gravity) }));
        }

        // @driving_port @real-io @us-01 @slice-01 @edge @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task An_instance_without_reports_answers_an_empty_list_not_a_failure()
        {
            ATeam("Gravity");

            var every = ReportsIn(await EveryReportTheCallerCanRead());

            Assert.That(every, Is.Empty);
        }

        // @driving_port @real-io @us-01 @slice-01 @edge @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task A_caller_who_can_read_no_owner_gets_an_empty_list()
        {
            var gravity = ATeam("Gravity");
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            TheCallerHasNoRoleOnIt();

            var every = ReportsIn(await EveryReportTheCallerCanRead());

            Assert.That(every, Is.Empty);
        }

        // @driving_port @real-io @us-01 @slice-01 @edge @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task A_report_of_a_type_this_version_cannot_show_is_still_listed()
        {
            var gravity = ATeam("Gravity");
            var reportId = await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            TheStoredReportBecomesOfType(reportId, "signals");

            var every = ReportsIn(await EveryReportTheCallerCanRead());

            Assert.That(every.Select(report => TextOf(report, "templateKey")), Is.EqualTo(OnlyTheLaterType));
        }

        // @driving_port @real-io @us-01 @us-05 @slice-01 @edge @contract-shape:pure-function
        // The removal confirm counts the Thens that can no longer be rebuilt from this list, without opening each report.
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task Each_row_says_whether_its_Then_can_still_be_rebuilt()
        {
            var equinox = ATeam("Equinox");
            var gravity = ATeam("Gravity");
            await GivenTheOwnerHolds(equinox, TheQuartersComparison);
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            TheKeptHistoryNowStartsOn(equinox, EquinoxKeepsFinishedWorkSince);

            var every = ReportsIn(await EveryReportTheCallerCanRead());
            var rebuildable = every.ToDictionary(report => TextOf(report, "ownerName") ?? "", ThenRebuildableIn);

            Assert.That(rebuildable, Is.EqualTo(new Dictionary<string, bool?> { ["Equinox"] = false, ["Gravity"] = true }));
        }

        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:unbounded-preservation
        // Older clients read the per-owner list; it must not gain the owner's name.
        [Test]
        public async Task The_per_owner_list_names_no_owner_as_before()
        {
            var gravity = ATeam("Gravity");
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);

            var list = ReportsIn(await TheReportsListOf(gravity));

            Assert.That(list.Select(report => report.TryGetProperty("ownerName", out _)), Has.All.False);
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:pure-function
        [Test]
        [Ignore(PendingEpic6350.Slice01)]
        public async Task Each_row_names_its_owners_kind_id_and_name_beside_the_report()
        {
            var gravity = ATeam("Gravity");
            var oceanExplorer = APortfolio("Ocean Explorer");
            await GivenTheOwnerHolds(gravity, TheQuartersComparison);
            await GivenTheOwnerHolds(oceanExplorer, TheQuartersComparison);

            var every = ReportsIn(await EveryReportTheCallerCanRead());
            var owners = every.Select(report => (
                TextOf(report, "ownerKind"),
                NumberOf(report, "ownerId"),
                TextOf(report, "ownerName"),
                TextOf(report, "name"),
                TextOf(report, "templateKey")));

            Assert.That(owners, Is.EquivalentTo(new[]
            {
                ("Team", (int?)gravity.Id, "Gravity", TheQuarter, "then-and-now"),
                ("Portfolio", (int?)oceanExplorer.Id, "Ocean Explorer", TheQuarter, "then-and-now"),
            }), "each row names its owner's kind, id and name beside the report's own name and type");
        }
    }
}
