using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5575, US-02 AC2 + AC3. The functional core of the team sync.
    //
    // Layer 1 (pure, no IO): the two rules that decide whether a flow coach's numbers are right or
    // merely plausible both live here, and both fail invisibly when broken —
    //
    //   * a record that was closed has to reach Throughput on the day it closed (ADR-117), and
    //   * a date has to be read from the universal-time form, never from the instance-local one.
    //
    // Every fixture below is shaped the way a real sysparm_display_value=all response is shaped:
    // { "display_value": ..., "value": ... } per field, with display_value in the instance timezone
    // and value in UTC.
    [TestFixture]
    public class ServiceNowWorkItemMapperTest
    {
        private const string Table = "incident";

        // A service desk that has told Lighthouse which of its labels mean what. "Resolved" is
        // mapped to Done because that is the mapping ADR-117's accepted cost is about: the team
        // calls the work finished, and without transition history the record carries no instant
        // saying when.
        private static Team ATeamThatCalls(string todo = "New", string doing = "In Progress", string done = "Resolved")
        {
            return new Team
            {
                Name = "Service Desk",
                ToDoStates = [todo],
                DoingStates = [doing],
                DoneStates = [done],
            };
        }

        // AC2. Without transition history the record itself is the only source, and closed_at is the
        // only instant on it that means "this work is over".
        [Test]
        public void WhenTheRecordWasClosed_IsWhenWorkFinished()
        {
            var record = AFinishedRecordWith(closed: "2026-07-30 08:00:00");

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc)));
        }

        // ADR-117 decision 1's accepted cost, amended 2026-07-31 and pinned here so it is a decision
        // rather than a surprise. closed_at is EMPTY on Resolved (state 6) — measured on the live
        // instance — and resolved_at is deliberately not read, because Resolved is a Doing state. A
        // team that nonetheless maps Resolved to Done, on an instance whose transition history
        // Lighthouse cannot read, gets work categorised Done with no day attached: it is missing from
        // Throughput while reading as finished everywhere else. The way out is the instance's own
        // transition history, which the connector prefers wherever it exists.
        [Test]
        public void WorkTheTeamCallsFinishedThatTheRecordDoesNotSayItFinished_CarriesNoDay()
        {
            var record = AFinishedRecordWith(closed: "");

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.StateCategory, Is.EqualTo(StateCategories.Done));
                Assert.That(workItem.ClosedDate, Is.Null,
                    "Set up your instance to measure state spans, or map Resolved to Doing — which is what it is.");
            }
        }

        [Test]
        public void WorkThatIsStillUnderway_HasNotFinished()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.ClosedField, "", ""));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.ClosedDate, Is.Null);
        }

        // H2. ServiceNow's reopen path does not reliably clear a closure instant, so a reopened
        // record arrives carrying one and a state the team maps to Doing. Setting both hides it from
        // every chart at once: Throughput counts Done only, and the WIP series drops anything closed
        // on or before the day being drawn. Actively-worked item, invisible.
        [Test]
        public void WorkThatWasReopened_IsNotCountedAsFinishedWhileItIsBeingWorkedOn()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.StateField, "In Progress", "2"),
                (ServiceNowWorkItemMapper.ClosedField, "2026-07-29 17:25:29", "2026-07-30 00:25:29"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(workItem.ClosedDate, Is.Null,
                    "A Doing item carrying a closure is counted in neither Throughput nor WIP, so it disappears from every chart while the team is still working on it.");
            }
        }

        // ADR-117: opened_at is a real, settable timestamp a customer backdates when importing
        // history, so it beats the row's creation time. The span this produces is
        // request-to-resolution rather than time-in-progress, which is the honest thing the record
        // can support without an itil-grade role.
        [Test]
        public void WhenWorkArrived_IsWhenTheRequestWasOpened()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.OpenedField, "2026-07-09 02:46:49", "2026-07-09 09:46:49"),
                (ServiceNowWorkItemMapper.CreatedField, "2026-07-29 06:46:49", "2026-07-29 13:46:49"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 9, 9, 46, 49, DateTimeKind.Utc)));
        }

        [Test]
        public void WorkThatCarriesNoRequestTime_ArrivedWhenItWasRecorded()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.OpenedField, "", ""),
                (ServiceNowWorkItemMapper.CreatedField, "2026-07-29 06:46:49", "2026-07-29 13:46:49"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 29, 13, 46, 49, DateTimeKind.Utc)));
        }

        // The date trap, and the reason this test picks instants that fall on DIFFERENT DAYS in the
        // two forms. Under sysparm_display_value=all, value is UTC and display_value is the
        // instance timezone — measured seven hours apart, with sys_created_on crossing midnight
        // between them. Lighthouse buckets Throughput by day, so reading the readable-looking form
        // files finished work under the wrong day, and only on instances far enough from UTC to
        // cross midnight. Nothing errors; the chart is simply wrong for some customers and right
        // for others. Bug #5567 spent a whole pass reclaiming exactly this ground.
        [Test]
        public void TheDayWorkFinished_IsTheDayTheInstanceRecordedInUniversalTime()
        {
            var record = ARecordWith(
                AFinishedState,
                (ServiceNowWorkItemMapper.ClosedField, "2026-07-29 21:25:29", "2026-07-30 04:25:29"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 4, 25, 29, DateTimeKind.Utc)),
                "The instance-local form of this timestamp falls on the 29th and the universal form on the 30th. Throughput buckets by day, so reading the wrong form moves finished work to the wrong day.");
        }

        [Test]
        public void TheDayWorkArrived_IsTheDayTheInstanceRecordedInUniversalTime()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.OpenedField, "2026-07-09 21:46:49", "2026-07-10 04:46:49"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 10, 4, 46, 49, DateTimeKind.Utc)));
        }

        [Test]
        public void TheDayWorkWasRecorded_IsTheDayTheInstanceRecordedInUniversalTime()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.CreatedField, "2026-07-28 23:46:48", "2026-07-29 06:46:48"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.CreatedDate, Is.EqualTo(new DateTime(2026, 7, 29, 6, 46, 48, DateTimeKind.Utc)));
        }

        // H1. A value carrying its own offset has to be CONVERTED to universal time, not relabelled
        // as universal. Relabelling reads the instant on the host machine's clock and then stamps
        // that wall-clock reading as UTC, which moves the instant by the machine's own offset — and
        // across a day boundary near midnight. The assertion is the instant rather than the Kind,
        // because a mapper that hardcodes DateTimeKind.Utc satisfies a Kind assertion by
        // construction and can never fail it.
        [TestCase("2026-07-30T02:25:29+02:00", TestName = "AnInstantCarryingItsOwnOffset_IsConvertedToUniversalTimeRatherThanRelabelled")]
        [TestCase("2026-07-30T00:25:29Z", TestName = "AnInstantCarryingAZuluMarker_IsReadAsTheInstantItNames")]
        [TestCase("2026-07-30 00:25:29", TestName = "AnInstantCarryingNoOffsetAtAll_IsReadAsUniversalTimeRatherThanLocal")]
        public void AnInstantTheInstanceReported_IsTheInstantLighthouseStores(string universalForm)
        {
            var record = ARecordWith(
                AFinishedState,
                (ServiceNowWorkItemMapper.ClosedField, "2026-07-29 17:25:29", universalForm));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 0, 25, 29, DateTimeKind.Utc)),
                "All three forms name the same instant, so all three have to map onto it whatever the host machine's own timezone is.");
        }

        // AC3, the other half of the display/value split. A flow coach maps the words their service
        // desk uses. The raw choice value is an integer nobody outside the platform team recognises,
        // and a state mapping screen offering "2" is a screen nobody can configure.
        [Test]
        public void TheStateAFlowCoachSees_IsTheLabelTheirServiceDeskUses()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.StateField, "In Progress", "2"));

            var label = ServiceNowWorkItemMapper.ReadStateLabel(record);

            Assert.That(label, Is.EqualTo("In Progress"),
                "The team maps the words on their own board. '2' is a platform-internal choice value.");
        }

        [Test]
        public void WorkInAStateTheTeamHasRenamed_IsReportedUnderTheTeamsOwnName()
        {
            var team = ATeamThatCalls(doing: "Doing");
            team.StateMappings = [new StateMapping { Name = "Doing", States = ["In Progress"] }];

            var record = ARecordWith((ServiceNowWorkItemMapper.StateField, "In Progress", "2"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, team, ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.State, Is.EqualTo("Doing"));
        }

        [TestCase("New", StateCategories.ToDo)]
        [TestCase("In Progress", StateCategories.Doing)]
        [TestCase("Resolved", StateCategories.Done)]
        [TestCase("Awaiting Vendor", StateCategories.Unknown)]
        public void WorkIsCategorised_ByTheLabelTheTeamMapped(string label, StateCategories expected)
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.StateField, label, "99"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.StateCategory, Is.EqualTo(expected));
        }

        [Test]
        public void WorkIsIdentified_ByTheNumberTheServiceDeskQuotes()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.RecordNumberField, "INC0010029", "INC0010029"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.ReferenceId, Is.EqualTo("INC0010029"));
        }

        [Test]
        public void WorkIsTitled_ByItsShortDescription()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.TitleField, "Printer on 3rd floor is offline", "Printer on 3rd floor is offline"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.Name, Is.EqualTo("Printer on 3rd floor is offline"));
        }

        // H3. Every guard in ReadForm was unreachable, because the fixture above can only ever emit
        // two well-formed strings. These are the shapes the live API actually returns — an explicit
        // JSON null for an unset date on change_request, a bare scalar where a field is not
        // display-valued, a number where a string was expected — and GetString() throws on the last
        // one, which would take the whole team sync down with it.
        [TestCase("""{"state": {"display_value": "In Progress", "value": 2}}""", TestName = "AFieldWhoseValueIsANumber_IsReadRatherThanThrown")]
        [TestCase("""{"state": null}""", TestName = "AFieldThatIsExplicitlyNull_IsReadAsNothing")]
        [TestCase("""{"state": "In Progress"}""", TestName = "AFieldThatIsABareScalar_IsReadAsNothing")]
        [TestCase("""{"state": {"display_value": null, "value": "2"}}""", TestName = "AFormThatIsExplicitlyNull_IsReadAsNothing")]
        [TestCase("""{"short_description": {"display_value": "x", "value": "x"}}""", TestName = "AFieldThatIsAbsentAltogether_IsReadAsNothing")]
        [TestCase("""[{"state": {"display_value": "In Progress", "value": "2"}}]""", TestName = "ARecordThatIsNotAnObject_IsReadAsNothing")]
        public void ARecordShapedInAWayTheMapperDidNotExpect_IsMappedRatherThanFatal(string json)
        {
            var record = ARecordFrom(json);

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem, Is.Not.Null,
                "A record Lighthouse cannot read a field off is one unmapped record, not a failed sync for the whole team.");
        }

        [Test]
        public void TheStateOfARecordThatCarriesANumberWhereALabelBelongs_IsReadAsThatNumber()
        {
            var record = ARecordFrom("""{"state": {"display_value": 2, "value": 2}}""");

            Assert.That(ServiceNowWorkItemMapper.ReadStateLabel(record), Is.EqualTo("2"));
        }

        [TestCase("""{"state": null}""", TestName = "TheStateOfARecordWhoseStateIsNull_IsEmpty")]
        [TestCase("""{"state": "In Progress"}""", TestName = "TheStateOfARecordWhoseStateIsABareScalar_IsEmpty")]
        [TestCase("""{"state": {"display_value": null, "value": "2"}}""", TestName = "TheStateOfARecordWhoseLabelIsNull_IsEmpty")]
        [TestCase("""{}""", TestName = "TheStateOfARecordWithNoStateAtAll_IsEmpty")]
        [TestCase("""[]""", TestName = "TheStateOfSomethingThatIsNotARecord_IsEmpty")]
        public void AStateTheMapperCannotRead_IsEmptyRatherThanAGuess(string json)
        {
            Assert.That(ServiceNowWorkItemMapper.ReadStateLabel(ARecordFrom(json)), Is.Empty);
        }

        // Story #5611 slice 01, AC-B2 / ADR-123 decision 8. A ServiceNow record class IS a Lighthouse
        // work item type, so a change request says "change_request" whatever hierarchy the team reads
        // it through. Today Type is the configured table, which for a team on the whole hierarchy is
        // the same lie repeated on every row.
        [Test]
        public void WorkThatSaysWhatKindItIs_IsLabelledWithItsOwnKind()
        {
            var record = ARecordWith((RecordClassField, "Change Request", "change_request"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading("change_request"), InstanceUrl);

            // Still the system name — because that is what THIS team named (ADR-128, amended). The
            // same record on a team that named `Change Request` reports Change Request. The rule is
            // not "class name" nor "label" but "the words this team used", which is what makes a
            // team's config and its work items agree by construction.
            Assert.That(workItem.Type, Is.EqualTo("change_request"),
                "The words this team named its work with. ServiceNowReadScope carries both forms and reports back the one that was typed.");
        }

        // AC-B2's other half, and the reason no shipped team's data moves: for a team reading a single
        // kind of work the record's own kind and the configured entry are the same string. ADR-128 did
        // not change that — it changed WHICH string, per team, and this team named the class.
        [Test]
        public void WorkOnATeamReadingOneKindOfWork_IsLabelledExactlyAsItWasBefore()
        {
            var record = ARecordWith((RecordClassField, "Incident", Table));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.Type, Is.EqualTo(Table));
        }

        // The other half of the amended ADR-128: the SAME record, on a team that named its work the
        // way ServiceNow does, reports back in those words. One record, two teams, two vocabularies —
        // and each team's config agrees with its own data, which is the property that matters.
        [Test]
        public void WorkOnATeamThatNamedItsWorkByLabel_IsLabelledThatWay()
        {
            var record = ARecordWith((RecordClassField, "Incident", Table));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading("Incident"), InstanceUrl);

            Assert.That(workItem.Type, Is.EqualTo("Incident"));
        }

        // A kind of work the team never named cannot reach here from a sync — the query filters to the
        // named ones — but AsTyped still has to answer sensibly rather than throw.
        [Test]
        public void WorkOfAKindTheTeamNeverNamed_KeepsTheClassTheRecordReports()
        {
            var record = ARecordWith((RecordClassField, "Problem", "problem"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.Type, Is.EqualTo("problem"));
        }

        private static ServiceNowReadScope ATeamReading(params string[] kindsOfWork)
        {
            return ServiceNowReadScope.For([.. kindsOfWork]);
        }

        // ---- Story #5612 item 1: the record is reachable from the chart. ----

        // The whole of the bucket's first item. Every other connector populates WorkItemBase.Url and
        // WorkItemsDialog already renders it as a link; ServiceNow was the only one of five leaving a
        // work item's id inert.
        [Test]
        public void AWorkItem_CarriesTheAddressOfItsRecordInServiceNow()
        {
            var record = ARecordWith(
                (RecordClassField, "Incident", "incident"),
                (ServiceNowWorkItemMapper.RecordIdField, "1234", "a1b2c3"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            Assert.That(workItem.Url, Is.EqualTo($"{InstanceUrl.TrimEnd('/')}/incident.do?sys_id=a1b2c3"));
        }

        // The reason the address is built from the RECORD's class and not from the team's: the .do
        // path is class-specific, so a team reading two kinds of work needs a different path per row.
        // Building it from the team's first kind, or from the hierarchy, 404s for every other row.
        [Test]
        public void WorkOfEachKindOnATeam_AddressesItsOwnKindsPage()
        {
            var change = ARecordWith(
                (RecordClassField, "Change Request", "change_request"),
                (ServiceNowWorkItemMapper.RecordIdField, "CHG1", "deadbeef"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(
                change, ATeamThatCalls(), ATeamReading("incident", "change_request"), InstanceUrl);

            Assert.That(workItem.Url, Does.Contain("/change_request.do?").And.Not.Contain("/incident.do?"));
        }

        // OC-3, settled the way Jira already settles it at JiraWorkTrackingConnector.cs:1297. The
        // Instance Url is user-entered and a trailing slash is the likeliest thing they leave on it.
        [TestCase("https://dev191338.service-now.com")]
        [TestCase("https://dev191338.service-now.com/")]
        public void AnInstanceAddressWithOrWithoutATrailingSlash_ProducesTheSameLink(string instanceUrl)
        {
            var record = ARecordWith(
                (RecordClassField, "Incident", "incident"),
                (ServiceNowWorkItemMapper.RecordIdField, "INC1", "abc"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), instanceUrl);

            Assert.That(workItem.Url, Is.EqualTo("https://dev191338.service-now.com/incident.do?sys_id=abc"));
        }

        // A link that 404s is worse than an absent one, and the dialog already renders a null url as
        // plain text. Both halves are required because a record missing either cannot be addressed.
        [Test]
        public void ARecordMissingEitherHalfOfItsAddress_GetsNoLinkRatherThanABrokenOne()
        {
            var noRecordId = ARecordWith((RecordClassField, "Incident", "incident"));
            var noClass = ARecordWith((ServiceNowWorkItemMapper.RecordIdField, "INC1", "abc"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    ServiceNowWorkItemMapper.MapRecord(noRecordId, ATeamThatCalls(), ATeamReading(Table), InstanceUrl).Url,
                    Is.Null);
                Assert.That(
                    ServiceNowWorkItemMapper.MapRecord(noClass, ATeamThatCalls(), ATeamReading(Table), InstanceUrl).Url,
                    Is.Null);
                Assert.That(
                    ServiceNowWorkItemMapper.MapRecord(noClass, ATeamThatCalls(), ATeamReading(Table), string.Empty).Url,
                    Is.Null,
                    "An unset instance address cannot produce one either.");
            }
        }

        private const string InstanceUrl = "https://dev191338.service-now.com/";

        // Not defensive padding: ReadForm answers string.Empty for a field that is not there, and an
        // empty Type on every row of a custom table would be a worse silent data change than the one
        // this rule fixes.
        [Test]
        public void WorkThatLeavesItsKindBlank_KeepsTheKindTheTeamReadsThrough()
        {
            var record = ARecordWith((RecordClassField, "", ""));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            // The hierarchy every ServiceNow read is rooted at, since #5611 removed the
            // connection-scope table. A record that does not say its own kind cannot be attributed to
            // one of the team's named kinds without guessing which.
            Assert.That(workItem.Type, Is.EqualTo(ServiceNowReadScope.RootTable));
        }

        [Test]
        public void WorkFromATableThatDoesNotRecordItsKind_KeepsTheKindTheTeamReadsThrough()
        {
            var workItem = ServiceNowWorkItemMapper.MapRecord(ARecordWith(), ATeamThatCalls(), ATeamReading(Table), InstanceUrl);

            // The hierarchy Lighthouse reads through, not the team's first named kind of work. Since
            // #5611 removed the connection-scope table every read is task-rooted, so this is the only
            // value the old `table` parameter could ever have carried at the real call site.
            Assert.That(workItem.Type, Is.EqualTo(ServiceNowReadScope.RootTable));
        }

        // The field is already in every record of the connector's sysparm_display_value=all read, so
        // reading it costs no extra request.
        private const string RecordClassField = ServiceNowWorkItemMapper.RecordClassField;

        private const string TheWholeHierarchy = "task";

        // The team above files "Resolved" under Done, and only finished work carries a finish date.
        private static (string Field, string DisplayValue, string Value) AFinishedState =>
            (ServiceNowWorkItemMapper.StateField, "Resolved", "6");

        private static JsonElement AFinishedRecordWith(string closed)
        {
            return ARecordWith(AFinishedState, (ServiceNowWorkItemMapper.ClosedField, closed, closed));
        }

        // Shapes a record the way sysparm_display_value=all shapes one. Unnamed fields get a
        // harmless default so each test states only the field it is about.
        private static JsonElement ARecordWith(params (string Field, string DisplayValue, string Value)[] fields)
        {
            var record = new Dictionary<string, (string DisplayValue, string Value)>
            {
                [ServiceNowWorkItemMapper.RecordNumberField] = ("INC0000001", "INC0000001"),
                [ServiceNowWorkItemMapper.TitleField] = ("Some request", "Some request"),
                [ServiceNowWorkItemMapper.StateField] = ("New", "1"),
                [ServiceNowWorkItemMapper.CreatedField] = ("2026-07-01 00:00:00", "2026-07-01 07:00:00"),
                [ServiceNowWorkItemMapper.OpenedField] = ("2026-07-01 00:00:00", "2026-07-01 07:00:00"),
                [ServiceNowWorkItemMapper.ClosedField] = ("", ""),
            };

            foreach (var (field, displayValue, value) in fields)
            {
                record[field] = (displayValue, value);
            }

            var body = string.Join(",", record.Select(entry =>
                $"{JsonSerializer.Serialize(entry.Key)}:{{\"display_value\":{JsonSerializer.Serialize(entry.Value.DisplayValue)},\"value\":{JsonSerializer.Serialize(entry.Value.Value)}}}"));

            return ARecordFrom($"{{{body}}}");
        }

        // The escape hatch from the well-formed fixture above, for the shapes it cannot express.
        private static JsonElement ARecordFrom(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}
