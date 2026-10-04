using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementReadinessSyncTest
    {
        [Test]
        public void A_saved_readiness_is_stored_with_what_it_leaves_out_kept()
        {
            var team = ATeamWithReadiness();

            team.SyncTeamWithTeamSettings(ASave(new ReadinessSettingDto { MinYes = 2 }));

            var readiness = team.RefinementSettings?.Readiness;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readiness?.MinYes, Is.EqualTo(2));
                Assert.That(readiness?.MinVoters, Is.EqualTo(5));
                Assert.That(readiness?.DiscussWhen.No, Is.EqualTo(2));
                Assert.That(readiness?.DiscussWhen.YesIf, Is.EqualTo(3));
            }
        }

        [Test]
        public void A_save_that_says_nothing_about_readiness_leaves_it_as_it_was()
        {
            var team = ATeamWithReadiness();

            team.SyncTeamWithTeamSettings(ASave(null));

            var readiness = team.RefinementSettings?.Readiness;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readiness?.MinYes, Is.EqualTo(4));
                Assert.That(readiness?.MinVoters, Is.EqualTo(5));
                Assert.That(readiness?.DiscussWhen.No, Is.EqualTo(2));
                Assert.That(readiness?.DiscussWhen.YesIf, Is.EqualTo(3));
            }
        }

        private static Team ATeamWithReadiness() => new()
        {
            ToDoStates = ["Backlog"],
            RefinementSettings = new RefinementSettings
            {
                States = [new RefinementStateSetting { State = "Backlog" }],
                Readiness = new ReadinessSetting { MinYes = 4, MinVoters = 5, DiscussWhen = new DiscussionRules { No = 2, YesIf = 3 } },
            },
        };

        private static TeamSettingDto ASave(ReadinessSettingDto? readiness) => new()
        {
            ToDoStates = ["Backlog"],
            Refinement = new RefinementSettingsDto
            {
                States = [new RefinementStateSettingDto { State = "Backlog" }],
                Readiness = readiness,
            },
        };
    }
}
