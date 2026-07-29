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
    //   * a record that was resolved but never closed still has to reach Throughput (ADR-117), and
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
        // deliberately mapped to Done: an ITSM shop treats a resolved incident as finished work,
        // and Lighthouse's out-of-the-box mapping files it under Doing.
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

        // AC2, and the headline defect ADR-117 exists to prevent. closed_at is empty on Resolved
        // (state 6) — measured on the live instance. A mapper that keys on closed_at alone drops
        // every resolved-but-not-closed record out of Throughput, and nothing anywhere reports a
        // failure: the chart simply reads lower than the work the team actually finished.
        [Test]
        public void WorkThatWasResolvedButNeverFormallyClosed_StillCountsAsFinished()
        {
            var record = ARecordWith(
                AResolvedState,
                (ServiceNowWorkItemMapper.ResolvedField, "2026-07-29 17:25:29", "2026-07-30 00:25:29"),
                (ServiceNowWorkItemMapper.ClosedField, "", ""));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 0, 25, 29, DateTimeKind.Utc)),
                "closed_at is empty on Resolved, so a mapper that keys on it alone silently drops this item from Throughput.");
        }

        // ADR-117's ladder, one case per rung. Resolution outranks closure, and closure is only a
        // fallback for the shops that do move records all the way to Closed.
        [Test]
        public void WhenBothAreRecorded_TheResolutionIsWhenWorkFinished()
        {
            var record = AFinishedRecordWith(resolved: "2026-07-29 07:25:29", closed: "2026-07-30 08:00:00");

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 29, 7, 25, 29, DateTimeKind.Utc)));
        }

        [Test]
        public void WhenOnlyTheClosureIsRecorded_TheClosureIsWhenWorkFinished()
        {
            var record = AFinishedRecordWith(resolved: "", closed: "2026-07-30 08:00:00");

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void WhenOnlyTheResolutionIsRecorded_TheResolutionIsWhenWorkFinished()
        {
            var record = AFinishedRecordWith(resolved: "2026-07-29 07:25:29", closed: "");

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 29, 7, 25, 29, DateTimeKind.Utc)));
        }

        [Test]
        public void WorkThatIsStillUnderway_HasNotFinished()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.ResolvedField, "", ""),
                (ServiceNowWorkItemMapper.ClosedField, "", ""));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.Null);
        }

        // H2. ServiceNow's reopen path does not reliably clear resolved_at, so a reopened incident
        // arrives carrying a resolution instant and a state the team maps to Doing. Setting both
        // hides it from every chart at once: Throughput counts Done only, and the WIP series drops
        // anything closed on or before the day being drawn. Actively-worked item, invisible.
        [Test]
        public void WorkThatWasReopened_IsNotCountedAsFinishedWhileItIsBeingWorkedOn()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.StateField, "In Progress", "2"),
                (ServiceNowWorkItemMapper.ResolvedField, "2026-07-29 17:25:29", "2026-07-30 00:25:29"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

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

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 9, 9, 46, 49, DateTimeKind.Utc)));
        }

        [Test]
        public void WorkThatCarriesNoRequestTime_ArrivedWhenItWasRecorded()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.OpenedField, "", ""),
                (ServiceNowWorkItemMapper.CreatedField, "2026-07-29 06:46:49", "2026-07-29 13:46:49"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

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
                AResolvedState,
                (ServiceNowWorkItemMapper.ResolvedField, "2026-07-29 21:25:29", "2026-07-30 04:25:29"),
                (ServiceNowWorkItemMapper.ClosedField, "", ""));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 4, 25, 29, DateTimeKind.Utc)),
                "The instance-local form of this timestamp falls on the 29th and the universal form on the 30th. Throughput buckets by day, so reading the wrong form moves finished work to the wrong day.");
        }

        [Test]
        public void TheDayWorkArrived_IsTheDayTheInstanceRecordedInUniversalTime()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.OpenedField, "2026-07-09 21:46:49", "2026-07-10 04:46:49"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 10, 4, 46, 49, DateTimeKind.Utc)));
        }

        [Test]
        public void TheDayWorkWasRecorded_IsTheDayTheInstanceRecordedInUniversalTime()
        {
            var record = ARecordWith(
                (ServiceNowWorkItemMapper.CreatedField, "2026-07-28 23:46:48", "2026-07-29 06:46:48"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

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
                AResolvedState,
                (ServiceNowWorkItemMapper.ResolvedField, "2026-07-29 17:25:29", universalForm));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

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

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, team, Table);

            Assert.That(workItem.State, Is.EqualTo("Doing"));
        }

        [TestCase("New", StateCategories.ToDo)]
        [TestCase("In Progress", StateCategories.Doing)]
        [TestCase("Resolved", StateCategories.Done)]
        [TestCase("Awaiting Vendor", StateCategories.Unknown)]
        public void WorkIsCategorised_ByTheLabelTheTeamMapped(string label, StateCategories expected)
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.StateField, label, "99"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.StateCategory, Is.EqualTo(expected));
        }

        [Test]
        public void WorkIsIdentified_ByTheNumberTheServiceDeskQuotes()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.RecordNumberField, "INC0010029", "INC0010029"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.ReferenceId, Is.EqualTo("INC0010029"));
        }

        [Test]
        public void WorkIsTitled_ByItsShortDescription()
        {
            var record = ARecordWith((ServiceNowWorkItemMapper.TitleField, "Printer on 3rd floor is offline", "Printer on 3rd floor is offline"));

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

            Assert.That(workItem.Name, Is.EqualTo("Printer on 3rd floor is offline"));
        }

        // ITSM records carry no work-item-type field — the table a record lives in is what kind of
        // work it is. That is why the team scope does not ask for a separate list of types.
        [Test]
        public void TheKindOfWork_IsTheTableItWasReadFrom()
        {
            var workItem = ServiceNowWorkItemMapper.MapRecord(ARecordWith(), ATeamThatCalls(), "change_request");

            Assert.That(workItem.Type, Is.EqualTo("change_request"));
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

            var workItem = ServiceNowWorkItemMapper.MapRecord(record, ATeamThatCalls(), Table);

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

        // The team above files "Resolved" under Done, and only finished work carries a finish date.
        private static (string Field, string DisplayValue, string Value) AResolvedState =>
            (ServiceNowWorkItemMapper.StateField, "Resolved", "6");

        private static JsonElement AFinishedRecordWith(string resolved, string closed)
        {
            return ARecordWith(
                AResolvedState,
                (ServiceNowWorkItemMapper.ResolvedField, resolved, resolved),
                (ServiceNowWorkItemMapper.ClosedField, closed, closed));
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
                [ServiceNowWorkItemMapper.ResolvedField] = ("", ""),
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
