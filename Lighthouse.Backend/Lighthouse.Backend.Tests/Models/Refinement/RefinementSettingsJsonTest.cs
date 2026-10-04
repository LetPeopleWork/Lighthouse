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
        public void A_value_stored_before_readiness_existed_reads_three_Yes_from_three_voters_and_no_veto()
        {
            var settings = JsonSerializer.Deserialize<RefinementSettings>("""{"States":[{"State":"Backlog","Stage":0}]}""", StoredJson)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.States.Select(state => state.State), Is.EqualTo(OnlyBacklog));
                Assert.That(settings.Readiness.MinYes, Is.EqualTo(3));
                Assert.That(settings.Readiness.MinVoters, Is.EqualTo(3));
                Assert.That(settings.Readiness.Veto, Is.Null);
            }
        }

        [Test]
        public void A_readiness_with_a_veto_round_trips()
        {
            var stored = new RefinementSettings
            {
                Readiness = new ReadinessSetting { MinYes = 2, MinVoters = 4, Veto = new VetoSetting { Threshold = 2, Counts = VetoCounts.NoOrYesBut } },
            };

            var read = JsonSerializer.Deserialize<RefinementSettings>(JsonSerializer.Serialize(stored, StoredJson), StoredJson)!;

            Assert.That(
                (read.Readiness.MinYes, read.Readiness.MinVoters, read.Readiness.Veto?.Threshold, read.Readiness.Veto?.Counts),
                Is.EqualTo((2, 4, (int?)2, (VetoCounts?)VetoCounts.NoOrYesBut)));
        }
    }
}
