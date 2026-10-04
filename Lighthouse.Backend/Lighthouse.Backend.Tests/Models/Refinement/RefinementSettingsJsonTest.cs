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

        private static (int MinYes, int MinVoters, int? No, int? YesIf) ReadinessOf(RefinementSettings settings)
            => (settings.Readiness.MinYes, settings.Readiness.MinVoters, settings.Readiness.DiscussWhen.No, settings.Readiness.DiscussWhen.YesIf);
    }
}
