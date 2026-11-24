using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Tests.Acceptance.BaselineReportCreation
{
    /// <summary>
    /// Reports that are already saved, and edits of them, at the Reports services with no web host. Every report
    /// compares equal lengths: its Now is as long as its Then and ends on the instance's today, which here is
    /// Sunday 15 Nov 2026, whatever Now length an older version stored. Edit changes the name and the shown metrics
    /// only, never checks Then against the kept history again, and refuses a request that still names another Now length.
    /// </summary>
    [TestFixture]
    public class EqualLengthReportsScenarios
    {
        private const string NowLengthDiffersFromThen = "now-length-differs-from-then";

        private const string StoredNowMember = "nowLengthDays";

        private static readonly TimeZoneInfo InstanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private static readonly DateTimeOffset NineInTheMorningOnSunday15Nov2026 = new(2026, 11, 15, 9, 0, 0, TimeSpan.FromHours(1));

        private static readonly DateOnly Today = new(2026, 11, 15);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        private static readonly string[] ThroughputShown = [ThroughputReportMetric.MetricKey];

        private FakeLighthouseClock clock = null!;

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabaseAndClock()
        {
            clock = new FakeLighthouseClock(NineInTheMorningOnSunday15Nov2026, InstanceZone);
            harness = new ServicePortHarness(services =>
            {
                foreach (var registration in ReportsAtThePortTest.WhatItRegisters)
                {
                    services.Add(registration);
                }

                services.AddSingleton<ILighthouseClock>(clock);
            });
        }

        [TearDown]
        public void RemoveTheScenariosDatabase() => harness.Dispose();

        [Test]
        public async Task A_saved_report_whose_Now_was_stored_45_days_long_reads_a_30_day_Now_ending_today_with_Then_and_its_stored_member_untouched()
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, new DateOnly(2026, 10, 16), 30);
            var readBefore = Read(lightspeed, reportId);
            TheOlderVersionStoredANowOf(lightspeed, reportId, 45);
            var storedBeforeReading = TheStoredPayload(lightspeed, reportId);

            var read = Read(lightspeed, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.NowWindow, Is.EqualTo(new ReportWindow(new DateOnly(2026, 10, 17), Today, 30)),
                    $"WHAT: Now read {read.NowWindow}. WHY: Now is always as long as Then and ends today, whatever an older version stored. HOW: derive Now from Then's length and never read the stored Now length.");
                Assert.That(read.ThenWindow, Is.EqualTo(readBefore.ThenWindow),
                    "WHAT: Then moved. WHY: only Now is derived; Then stays as it was saved. HOW: read Then from the payload unchanged.");
                Assert.That(read.Metrics.ToDictionary(pair => pair.Key, pair => pair.Value.AtThen), Is.EqualTo(readBefore.Metrics.ToDictionary(pair => pair.Key, pair => pair.Value.AtThen)),
                    "WHAT: a frozen Then value changed. WHY: past values never change when Now is derived. HOW: keep the captures as stored.");
                Assert.That(TheStoredPayload(lightspeed, reportId).ToJsonString(), Is.EqualTo(storedBeforeReading.ToJsonString()),
                    "WHAT: reading the report rewrote its stored payload. WHY: the legacy member stays as stored and unread. HOW: derive Now on read and write nothing.");
            }
        }

        [Test]
        public async Task A_saved_report_whose_Now_was_stored_longer_reads_and_lists_exactly_as_one_that_never_stored_it_so_no_notice_and_a_past_export_reads_the_same_30_day_Now()
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, new DateOnly(2026, 10, 16), 30);
            var readWithoutIt = TheWholeReadOf(lightspeed, reportId);
            var listedWithoutIt = TheWholeListOf(lightspeed);
            TheOlderVersionStoredANowOf(lightspeed, reportId, 45);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheWholeReadOf(lightspeed, reportId), Is.EqualTo(readWithoutIt),
                    "WHAT: the report reads differently because an older version stored a Now length. WHY: nothing about the update is shown to the reader, and a PDF export reads the same 30-day Now window. HOW: ignore the stored Now length and add nothing to the read.");
                Assert.That(TheWholeListOf(lightspeed), Is.EqualTo(listedWithoutIt),
                    "WHAT: the list entry differs because an older version stored a Now length. WHY: no notice is shown. HOW: ignore the stored Now length when listing.");
                Assert.That(Read(lightspeed, reportId).NowWindow, Is.EqualTo(new ReportWindow(new DateOnly(2026, 10, 17), Today, 30)),
                    "WHAT: the Now window an export would read is not 30 days ending today. WHY: past exports keep their files and new ones use the derived Now. HOW: derive Now from Then.");
            }
        }

        [Test]
        public async Task Editing_the_name_and_shown_metrics_of_a_saved_report_is_saved_and_leaves_its_window_members_as_stored()
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, new DateOnly(2026, 10, 16), 30);
            TheOlderVersionStoredANowOf(lightspeed, reportId, 45);
            var storedBefore = TheStoredPayload(lightspeed, reportId);

            var outcome = await Edit(lightspeed, reportId, "Renamed", ThroughputShown, requestedNowLengthDays: null);

            var read = outcome.Value;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                    $"WHAT: the edit was {outcome.Status} {outcome.Code}. WHY: the name and shown metrics may always be changed. HOW: save them.");
                Assert.That(read!.Name, Is.EqualTo("Renamed"), "WHAT: the new name was not saved. WHY: the editor changed it. HOW: store the name from the edit.");
                Assert.That(read.ShownMetricKeys, Is.EqualTo(ThroughputShown), "WHAT: the shown metrics were not saved. WHY: the editor changed them. HOW: store the shown keys from the edit.");
                Assert.That(read.ThenAndNow!.NowWindow, Is.EqualTo(new ReportWindow(new DateOnly(2026, 10, 17), Today, 30)),
                    "WHAT: the edited report shows another Now than 30 days ending today. WHY: Now is always as long as Then. HOW: derive Now from Then on every read.");
                Assert.That(TheStoredPayload(lightspeed, reportId).ToJsonString(), Is.EqualTo(storedBefore.ToJsonString()),
                    "WHAT: the edit changed the stored payload. WHY: an edit writes no window member and keeps what an older version stored. HOW: leave the payload as it is.");
            }
        }

        [TestCase(45, NowLengthDiffersFromThen, TestName = "Editing a 90-day Then report with a 45-day Now is refused because Now is always as long as Then, and nothing is written")]
        [TestCase(120, NowLengthDiffersFromThen, TestName = "Editing a 90-day Then report with a 120-day Now is refused because Now is always as long as Then, and nothing is written")]
        [TestCase(90, null, TestName = "Editing a 90-day Then report with a 90-day Now is saved and writes no Now length")]
        [TestCase(null, null, TestName = "Editing a 90-day Then report with no Now length is saved and writes no Now length")]
        public async Task An_edit_names_no_Now_length_or_the_length_of_Then_and_is_never_refused_for_another(int? requestedNowLengthDays, string? expectedRefusal)
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, Today, 90);
            var storedBefore = TheStoredPayload(lightspeed, reportId);
            ReportOutcome<ReportRead> outcome = null!;

            var writes = await harness.Writes.DuringAsync(async () =>
                outcome = await Edit(lightspeed, reportId, "Renamed", ThroughputShown, requestedNowLengthDays));

            using (Assert.EnterMultipleScope())
            {
                if (expectedRefusal is null)
                {
                    Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                        $"WHAT: an edit naming {requestedNowLengthDays?.ToString() ?? "no"} Now length was {outcome.Status} {outcome.Code}. WHY: an absent or equal Now length is accepted. HOW: save the edit.");
                    Assert.That(TheStoredPayload(lightspeed, reportId).ToJsonString(), Is.EqualTo(storedBefore.ToJsonString()),
                        "WHAT: the edit wrote a window member into the payload. WHY: Now is derived, so no Now length is stored. HOW: write only the name and shown metrics.");
                    return;
                }

                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused),
                    $"WHAT: an edit naming a {requestedNowLengthDays}-day Now for a 90-day Then was {outcome.Status}. WHY: Now is always as long as Then. HOW: refuse with {expectedRefusal}.");
                Assert.That(outcome.Code, Is.EqualTo(expectedRefusal),
                    $"WHAT: the refusal code was {outcome.Code}. WHY: clients word an unequal Now by this code. HOW: answer {expectedRefusal}.");
                Assert.That(writes.StatementsThatWrote, Is.Empty,
                    "WHAT: a refused edit wrote to the database. WHY: a refusal changes nothing. HOW: refuse before any save.");
                Assert.That(TheNameOf(lightspeed, reportId), Is.EqualTo("Engagement start"),
                    "WHAT: a refused edit renamed the report. WHY: a refusal changes nothing. HOW: refuse before the name is stored.");
            }
        }

        [TestCase(null, TestName = "A report whose Then starts before the oldest kept day is edited with no Now length and stays editable")]
        [TestCase(30, TestName = "A report whose Then starts before the oldest kept day is edited with a 30-day Now and stays editable")]
        public async Task A_report_whose_Then_lies_before_the_oldest_kept_day_stays_editable(int? requestedNowLengthDays)
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, new DateOnly(2026, 10, 16), 30);
            clock.SetInstant(NineInTheMorningOnSunday15Nov2026.AddDays(400));
            var laterToday = Today.AddDays(400);

            var outcome = await Edit(lightspeed, reportId, "Renamed", ThroughputShown, requestedNowLengthDays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                    $"WHAT: the edit was {outcome.Status} {outcome.Code} once Then lay before the oldest kept day. WHY: an edit never checks the kept history again. HOW: do not re-check Then or Now against the kept history on Edit.");
                Assert.That(outcome.Value!.ThenAndNow!.ThenRebuildable, Is.False,
                    "WHAT: Then still counts as rebuildable. WHY: the setup lets Then fall out of the kept year. HOW: keep the rebuildable flag as is.");
                Assert.That(outcome.Value.ThenAndNow.NowWindow, Is.EqualTo(new ReportWindow(laterToday.AddDays(-29), laterToday, 30)),
                    "WHAT: Now is not 30 days ending today. WHY: Now is always as long as Then. HOW: derive Now from Then's length.");
            }
        }

        [TestCase(0, 14, "2026-11-02", "2026-11-15", "2026-11-02", "2026-11-15",
            TestName = "A new report with a 14-day Then ending today, 2 Nov to 15 Nov 2026, reads a Now of the same 2 Nov to 15 Nov 2026")]
        [TestCase(45, 90, "2026-07-04", "2026-10-01", "2026-08-18", "2026-11-15",
            TestName = "A new report with a 90-day Then ending 45 days ago, 4 Jul to 1 Oct 2026, reads a 90-day Now of 18 Aug to 15 Nov 2026")]
        public async Task A_new_report_reads_a_Now_as_long_as_Then_ending_today(int thenEndedDaysAgo, int thenLengthDays, string thenStart, string thenEnd, string nowStart, string nowEnd)
        {
            var lightspeed = ATeam("Lightspeed");
            var reportId = await ASavedReport(lightspeed, Today.AddDays(-thenEndedDaysAgo), thenLengthDays);

            var read = Read(lightspeed, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.ThenWindow, Is.EqualTo(new ReportWindow(DateOnly.Parse(thenStart), DateOnly.Parse(thenEnd), thenLengthDays)),
                    "WHAT: Then reads another window than asked for. WHY: Then is frozen as asked. HOW: keep the requested Then.");
                Assert.That(read.NowWindow, Is.EqualTo(new ReportWindow(DateOnly.Parse(nowStart), DateOnly.Parse(nowEnd), thenLengthDays)),
                    "WHAT: Now is not as long as Then or does not end today. WHY: every report compares equal lengths. HOW: derive Now from Then's length.");
            }
        }

        private async Task<int> ASavedReport(ReportOwner owner, DateOnly thenEnd, int thenLengthDays)
        {
            using var scope = harness.NewScope();
            var created = await scope.ServiceProvider.GetRequiredService<IReportCommands>()
                .Create(owner, new NewReport(ThenAndNowTemplate.TemplateKey, "Engagement start", CycleTimeShown, new NewThenAndNow(thenEnd, thenLengthDays, null)));
            Assert.That(created.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                $"WHAT: the report to start from was {created.Status} {created.Code}. WHY: its Then lies in the kept history. HOW: create it.");
            return created.Value!.Id;
        }

        /// <summary>An older version kept the Now length on the report row; this stores it again, as that version did.</summary>
        private void TheOlderVersionStoredANowOf(ReportOwner owner, int reportId, int nowLengthDays)
        {
            using var scope = harness.NewScope();
            var writer = scope.ServiceProvider.GetRequiredService<IReportWriter>();
            var report = writer.FindForEdit(owner, reportId)!;
            var payload = JsonNode.Parse(report.TemplatePayloadJson)!.AsObject();
            payload[StoredNowMember] = nowLengthDays;
            report.TemplatePayloadJson = payload.ToJsonString();
            writer.Save().GetAwaiter().GetResult();
        }

        private JsonObject TheStoredPayload(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return JsonNode.Parse(scope.ServiceProvider.GetRequiredService<IReportReader>().GetForOwner(owner, reportId)!.TemplatePayloadJson)!.AsObject();
        }

        private ThenAndNowRead Read(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId).Value!.ThenAndNow!;
        }

        private string TheWholeReadOf(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return JsonSerializer.Serialize(scope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId).Value, ThenAndNowPayload.JsonOptions);
        }

        private string TheWholeListOf(ReportOwner owner)
        {
            using var scope = harness.NewScope();
            return JsonSerializer.Serialize(scope.ServiceProvider.GetRequiredService<IReportQueries>().List(owner).Value, ThenAndNowPayload.JsonOptions);
        }

        private string TheNameOf(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId).Value!.Name;
        }

        private async Task<ReportOutcome<ReportRead>> Edit(ReportOwner owner, int reportId, string name, IReadOnlyList<string> shown, int? requestedNowLengthDays)
        {
            Guid token;
            using (var readScope = harness.NewScope())
            {
                token = readScope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId).Value!.ConcurrencyToken;
            }

            using var scope = harness.NewScope();
            return await scope.ServiceProvider.GetRequiredService<IReportCommands>().Edit(owner, reportId, new ReportEdit(name, shown, token, requestedNowLengthDays));
        }

        /// <summary>A Team is stored that keeps its finished Work Items for a year.</summary>
        private ReportOwner ATeam(string name)
        {
            using var scope = harness.NewScope();
            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = LIGHTSPEED",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = 365,
            };
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();
            return ReportOwner.Team(team.Id);
        }
    }
}
