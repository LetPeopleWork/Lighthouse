using System.Text.Json;
using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Tests.Models.Reports
{
    public class ThenAndNowPayloadRoundTripTest
    {
        // The first version froze only the 85th percentile; later ones keep the 70th and 50th beside it.
        private const string FirstVersionDocument =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90},"nowLengthDays":30,"captures":{"cycle-time":{"p85":{"value":21,"sampleSize":10,"absentReason":null}}}}""";

        private const string DocumentWithoutLaterMembers =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90}}""";

        private const string DocumentALaterVersionWrote =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90},"nowLengthDays":30,"captures":{"cycle-time":{"p85":{"value":21,"sampleSize":10,"absentReason":null,"trend":"down"}}},"authorNotes":{"cycle-time":"a note"},"schemaVersion":7}""";

        private const string DocumentThatFrozeThe95th =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90},"nowLengthDays":90,"captures":{"cycle-time":{"p95":{"value":34,"sampleSize":20,"absentReason":null},"p85":{"value":21,"sampleSize":20,"absentReason":null}}}}""";

        private static readonly ReportWindow Then = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly CapturedValue Frozen85th = CapturedValue.Shown(21, 10);

        [Test]
        public void A_first_version_document_reads_into_the_current_payload()
        {
            var payload = ThenAndNowPayload.FromJson(FirstVersionDocument)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.ThenWindow, Is.EqualTo(Then));
                Assert.That(payload.NowLengthDays, Is.EqualTo(30));
                Assert.That(The85thIn(payload), Is.EqualTo(Frozen85th));
            }
        }

        [Test]
        public void Members_a_document_predates_read_as_null()
        {
            var payload = ThenAndNowPayload.FromJson(DocumentWithoutLaterMembers)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.ThenWindow, Is.EqualTo(Then));
                Assert.That(payload.NowLengthDays, Is.Null);
                Assert.That(payload.Captures, Is.Null);
            }
        }

        [Test]
        public void Members_this_version_does_not_know_are_ignored()
        {
            var payload = ThenAndNowPayload.FromJson(DocumentALaterVersionWrote)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.ThenWindow, Is.EqualTo(Then));
                Assert.That(payload.NowLengthDays, Is.EqualTo(30));
                Assert.That(The85thIn(payload), Is.EqualTo(Frozen85th));
            }
        }

        [Test]
        public void A_frozen_95th_is_one_more_member_of_the_capture_and_reads_back_with_its_sample_size()
        {
            var payload = ThenAndNowPayload.FromJson(DocumentThatFrozeThe95th)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.Captures!["cycle-time"]["p95"].Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions), Is.EqualTo(CapturedValue.Shown(34, 20)));
                Assert.That(The85thIn(payload), Is.EqualTo(CapturedValue.Shown(21, 20)));
            }
        }

        [Test]
        public void A_payload_reads_back_as_it_was_written()
        {
            var written = new ThenAndNowPayload { ThenWindow = Then, NowLengthDays = 30 };

            Assert.That(ThenAndNowPayload.FromJson(written.ToJson()), Is.EqualTo(written));
        }

        private static CapturedValue? The85thIn(ThenAndNowPayload payload)
            => payload.Captures!["cycle-time"]["p85"].Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions);
    }
}
