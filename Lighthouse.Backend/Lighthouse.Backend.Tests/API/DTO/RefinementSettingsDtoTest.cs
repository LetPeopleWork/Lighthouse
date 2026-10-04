using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementSettingsDtoTest
    {
        private static readonly DiscussionRules StoredRules = new() { No = 2, YesIf = 3 };

        [Test]
        public void The_readiness_a_Team_has_is_read_out_whole()
        {
            var settings = new RefinementSettings
            {
                Readiness = new ReadinessSetting { MinYes = 4, MinVoters = 6, DiscussWhen = new DiscussionRules { No = 2, YesIf = null } },
            };

            var readiness = new RefinementSettingsDto(settings).Readiness;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readiness?.MinYes, Is.EqualTo(4));
                Assert.That(readiness?.MinVoters, Is.EqualTo(6));
                Assert.That(readiness?.DiscussWhen?.No, Is.EqualTo(2));
                Assert.That(readiness?.DiscussWhen?.YesIf, Is.Null);
            }
        }

        [Test]
        public void Discussion_rules_read_out_and_sent_back_replace_the_stored_ones_including_a_rule_that_is_off()
        {
            var readOut = new DiscussionRulesDto(new DiscussionRules { No = 5, YesIf = null });

            var applied = readOut.AppliedTo(StoredRules);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(applied.No, Is.EqualTo(5));
                Assert.That(applied.YesIf, Is.Null);
            }
        }

        [Test]
        public void A_save_that_sends_only_the_No_rule_keeps_the_stored_Yes_if_rule()
        {
            var applied = new DiscussionRulesDto { No = 4 }.AppliedTo(StoredRules);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(applied.No, Is.EqualTo(4));
                Assert.That(applied.YesIf, Is.EqualTo(3));
            }
        }

        [Test]
        public void A_save_that_sends_only_the_Yes_if_rule_keeps_the_stored_No_rule()
        {
            var applied = new DiscussionRulesDto { YesIf = null }.AppliedTo(StoredRules);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(applied.No, Is.EqualTo(2));
                Assert.That(applied.YesIf, Is.Null);
            }
        }
    }
}
