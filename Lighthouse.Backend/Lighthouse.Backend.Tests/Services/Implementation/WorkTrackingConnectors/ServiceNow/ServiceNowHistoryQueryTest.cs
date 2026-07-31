using System.Text.Json;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5577, ADR-118 decisions 2 and 4. Layer 1 (pure, no IO).
    //
    // What gets asked of metric_instance, and what is kept of the answer. Both halves fail quietly
    // when wrong: an over-long batch is a 414 that fails the whole sync, and an under-filtered answer
    // invents state transitions out of rows that never described a state.
    [TestFixture]
    public class ServiceNowHistoryQueryTest
    {
        private const string Table = "incident";
        private const string StateSpanDefinition = "35f2b283c0a808ae000b7132cd0a4f55";
        private const string ScriptCalculationDefinition = "35edf981c0a808ae009895af7c843ace";

        private static IReadOnlyList<string> Records(int count)
        {
            return [.. Enumerable.Range(1, count).Select(index => index.ToString("x32"))];
        }

        // The measured cliff: 245 ids answered 200 at 8182 bytes, 250 answered 414 at 8347. A batch
        // sized by row count rather than by URL length walks straight into it.
        [Test]
        public void ATeamLargerThanOneBatch_IsSplit()
        {
            var batches = ServiceNowHistoryQuery.IntoBatches(Records(500));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(batches, Has.Count.EqualTo(3));
                Assert.That(batches.Sum(batch => batch.Count), Is.EqualTo(500), "Splitting must not lose records.");
                Assert.That(batches.Select(batch => batch.Count), Has.All.LessThanOrEqualTo(ServiceNowHistoryQuery.RecordsPerBatch));
            }
        }

        [Test]
        public void ATeamThatFitsInOneBatch_IsNotSplit()
        {
            var batches = ServiceNowHistoryQuery.IntoBatches(Records(96));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(batches, Has.Count.EqualTo(1));
                Assert.That(batches[0], Has.Count.EqualTo(96));
            }
        }

        // Every record appears once and in one batch. A duplicate would double a record's spans and
        // manufacture transitions between a state and itself.
        [Test]
        public void SplittingATeam_KeepsEveryRecordExactlyOnce()
        {
            var records = Records(450);

            var batched = ServiceNowHistoryQuery.IntoBatches(records).SelectMany(batch => batch).ToList();

            Assert.That(batched, Is.EquivalentTo(records));
        }

        [Test]
        public void ATeamWithNoWork_AsksForNothing()
        {
            var batches = ServiceNowHistoryQuery.IntoBatches([]);

            Assert.That(batches.SelectMany(batch => batch), Is.Empty,
                "A team whose query matched nothing must not produce a request for every span in the instance.");
        }

        // The batch is bounded by URL bytes, so the assembled query has to stay under the limit that
        // was actually measured — not merely under a record count that happened to work once.
        [Test]
        public void AFullBatchsQuery_StaysUnderTheMeasuredUrlLimit()
        {
            var query = ServiceNowHistoryQuery.SpanQueryFor(Records(ServiceNowHistoryQuery.RecordsPerBatch), [StateSpanDefinition]);

            Assert.That(query, Has.Length.LessThan(7000),
                "8192 bytes is the cliff and the rest of the URL has to fit too — instance address, path and the other sysparm_* parameters.");
        }

        // ADR-118 D2, corrected by measurement against the live PDI: `sysparm_query` matches the
        // STORED value of a choice field, never its label. `type=Field value duration` answered 200
        // with an empty result; `type=field_value_duration` returned the incident table's four span
        // definitions. Filtering by the `field` name stays the rejected alternative — it hardcodes
        // which field counts as state per table and is blind to a customer's own definitions.
        [Test]
        public void TheDefinitionQuery_AsksForTheTypeByTheValueTheInstanceStores()
        {
            var query = ServiceNowHistoryQuery.DefinitionQueryFor([Table]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(query, Does.Contain(Table));
                Assert.That(query, Does.Contain("type=field_value_duration"),
                    "The display label matches nothing at all — the instance filters on the stored value.");
            }
        }

        [Test]
        public void TheSpanQuery_RestrictsToBothTheRecordsAndTheDefinitions()
        {
            var query = ServiceNowHistoryQuery.SpanQueryFor([Records(1)[0]], [StateSpanDefinition]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(query, Does.Contain(Records(1)[0]), "Without the record filter this reads every span in the instance.");
                Assert.That(query, Does.Contain(StateSpanDefinition), "Without the definition filter it reads rows that are not spans.");
            }
        }

        // The row shape measured on the live instance: the LABEL is in `value`, the choice number is
        // in `field_value`. Reading field_value would map states by a number whose meaning differs
        // per table — 3 is On Hold on incident and Closed Complete on task (SPIKE Q10).
        [Test]
        public void ASpanRow_IsReadForItsLabelRatherThanItsChoiceNumber()
        {
            var rows = Rows(ARow(record: "abc", definition: StateSpanDefinition, label: "In Progress", number: "2", start: "2026-07-29 09:00:00"));

            var spans = ServiceNowHistoryQuery.SpansFrom(rows, [StateSpanDefinition]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spans, Has.Count.EqualTo(1));
                Assert.That(spans[0].Label, Is.EqualTo("In Progress"));
                Assert.That(spans[0].RecordId, Is.EqualTo("abc"));
                Assert.That(spans[0].Start, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)));
            }
        }

        // ADR-118 D2, and the reason the definition read exists at all. On the live instance
        // field=incident_state carries "Incident State Duration" spans alongside "Create to Resolve
        // Duration" rows, which are not spans. Keeping the latter invents transitions.
        [Test]
        public void RowsFromADefinitionThatDoesNotMeasureState_AreDiscarded()
        {
            var rows = Rows(
                ARow(record: "abc", definition: StateSpanDefinition, label: "In Progress", number: "2", start: "2026-07-29 09:00:00"),
                ARow(record: "abc", definition: ScriptCalculationDefinition, label: "", number: "", start: "2026-07-29 06:00:00"));

            var spans = ServiceNowHistoryQuery.SpansFrom(rows, [StateSpanDefinition]);

            Assert.That(spans, Has.Count.EqualTo(1),
                "A script-calculation row is not a state span, and pairing it with one produces a move that never happened.");
        }

        // Dates come from the universal form. The instance-local form can fall on a different
        // calendar day — seven hours apart on the measured instance — and everything downstream
        // buckets by day. Bug #5567 is the ledger entry for what relabelling costs.
        [Test]
        public void ASpansStart_IsReadInUniversalTime()
        {
            var rows = Rows(ARow(
                record: "abc",
                definition: StateSpanDefinition,
                label: "New",
                number: "1",
                start: "2026-07-29 06:46:43",
                localStart: "2026-07-28 23:46:43"));

            var spans = ServiceNowHistoryQuery.SpansFrom(rows, [StateSpanDefinition]);

            Assert.That(spans[0].Start, Is.EqualTo(new DateTime(2026, 7, 29, 6, 46, 43, DateTimeKind.Utc)),
                "The local form falls on the previous day here, and a state-time chart would file the whole span wrong.");
        }

        [Test]
        public void AnEmptyAnswer_ProducesNoSpans()
        {
            var spans = ServiceNowHistoryQuery.SpansFrom([], [StateSpanDefinition]);

            Assert.That(spans, Is.Empty);
        }

        // A row whose start cannot be read is not a span. Defaulting it to anything — epoch, now,
        // the record's creation — puts a fabricated instant into a chart that reads as measurement.
        [Test]
        public void ARowWithNoReadableStart_IsNotASpan()
        {
            var rows = Rows(ARow(record: "abc", definition: StateSpanDefinition, label: "New", number: "1", start: ""));

            var spans = ServiceNowHistoryQuery.SpansFrom(rows, [StateSpanDefinition]);

            Assert.That(spans, Is.Empty);
        }

        private static IReadOnlyList<JsonElement> Rows(params string[] rows)
        {
            using var document = JsonDocument.Parse($"[{string.Join(",", rows)}]");

            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        // The sysparm_display_value=all shape, as measured: every field arrives as
        // { display_value, value }, and `definition` and `id` additionally carry a link.
        private static string ARow(string record, string definition, string label, string number, string start, string? localStart = null)
        {
            return $$"""
                {
                  "id": { "display_value": "Incident: INC0010014", "value": "{{record}}" },
                  "definition": { "display_value": "A Metric", "value": "{{definition}}" },
                  "field": { "display_value": "incident_state", "value": "incident_state" },
                  "value": { "display_value": "{{label}}", "value": "{{label}}" },
                  "field_value": { "display_value": "{{number}}", "value": "{{number}}" },
                  "start": { "display_value": "{{localStart ?? start}}", "value": "{{start}}" },
                  "end": { "display_value": "", "value": "" }
                }
                """;
        }
    }
}
