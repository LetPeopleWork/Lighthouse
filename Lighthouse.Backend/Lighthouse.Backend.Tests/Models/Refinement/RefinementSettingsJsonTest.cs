using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Tests.Models.Refinement
{
    /// <summary>
    /// The Team's refinement settings are one JSON value read with these options. A value stored before a
    /// member existed must read back as that member's default, or every Team saved earlier loses it.
    /// </summary>
    public class RefinementSettingsJsonTest
    {
        private static readonly JsonSerializerOptions StoredJson = new(JsonSerializerDefaults.General);

        private static readonly string[] OnlyBacklog = ["Backlog"];

        private static readonly DayOfWeek[] TuesdayAndThursday = [DayOfWeek.Tuesday, DayOfWeek.Thursday];

        [Test]
        public void A_value_stored_before_readiness_existed_reads_three_Yes_from_three_voters_and_both_discussion_rules_on()
        {
            var settings = JsonSerializer.Deserialize<RefinementSettings>("""{"States":[{"State":"Backlog","Stage":0}]}""", StoredJson)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.States.Select(state => state.State), Is.EqualTo(OnlyBacklog));
                Assert.That(ReadinessOf(settings), Is.EqualTo((3, 3, (int?)1, (int?)2)));
            }
        }

        [Test]
        public void A_readiness_stored_before_the_discussion_rules_existed_reads_both_rules_on()
        {
            var settings = JsonSerializer.Deserialize<RefinementSettings>(
                """{"States":[],"Readiness":{"MinYes":2,"MinVoters":4,"Veto":{"Threshold":3,"Counts":1}}}""", StoredJson)!;

            Assert.That(ReadinessOf(settings), Is.EqualTo((2, 4, (int?)1, (int?)2)));
        }

        [TestCase(2, 3)]
        [TestCase(null, 1)]
        [TestCase(4, null)]
        [TestCase(null, null)]
        public void A_readiness_with_its_discussion_rules_round_trips(int? no, int? yesIf)
        {
            var stored = new RefinementSettings
            {
                Readiness = new ReadinessSetting { MinYes = 2, MinVoters = 4, DiscussWhen = new DiscussionRules { No = no, YesIf = yesIf } },
            };

            var read = JsonSerializer.Deserialize<RefinementSettings>(JsonSerializer.Serialize(stored, StoredJson), StoredJson)!;

            Assert.That(ReadinessOf(read), Is.EqualTo((2, 4, no, yesIf)));
        }

        [Test]
        public void A_value_stored_before_the_cadence_existed_reads_as_a_Team_without_a_cadence()
        {
            var settings = JsonSerializer.Deserialize<RefinementSettings>("""{"States":[{"State":"Backlog","Stage":0}]}""", StoredJson)!;

            Assert.That(settings.Cadence, Is.Null);
        }

        [TestCase(null)]
        [TestCase("2026-10-05")]
        public void A_cadence_round_trips(string? anchorWeek)
        {
            var anchor = anchorWeek is null ? (DateOnly?)null : DateOnly.Parse(anchorWeek, CultureInfo.InvariantCulture);
            var stored = new RefinementSettings { Cadence = RefinementCadence.Of(TuesdayAndThursday, 2, anchor) };

            var read = JsonSerializer.Deserialize<RefinementSettings>(JsonSerializer.Serialize(stored, StoredJson), StoredJson)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Cadence?.Weekdays, Is.EqualTo(TuesdayAndThursday));
                Assert.That(read.Cadence?.IntervalWeeks, Is.EqualTo(2));
                Assert.That(read.Cadence?.AnchorWeek, Is.EqualTo(anchor));
            }
        }

        private static (int MinYes, int MinVoters, int? No, int? YesIf) ReadinessOf(RefinementSettings settings)
            => (settings.Readiness.MinYes, settings.Readiness.MinVoters, settings.Readiness.DiscussWhen.No, settings.Readiness.DiscussWhen.YesIf);
    }
}
