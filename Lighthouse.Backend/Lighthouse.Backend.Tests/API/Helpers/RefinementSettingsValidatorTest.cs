using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    public class RefinementSettingsValidatorTest
    {
        private const string Backlog = "Backlog";

        private const string Refining = "Refining";

        private const string Implementation = "Implementation";

        private const string Analysing = "Analysing";

        private const string Done = "Done";

        private const string Icebox = "Icebox";

        private static TeamSettingDto SettingsChoosing(params string[] chosen)
        {
            return new TeamSettingDto
            {
                Name = "Team",
                ToDoStates = [Backlog],
                DoingStates = [Refining, Implementation],
                DoneStates = [Done],
                StateMappings = [new StateMappingDto { Name = Refining, States = [Analysing, "Grooming"] }],
                Refinement = new RefinementSettingsDto
                {
                    States = [.. chosen.Select(state => new RefinementStateSettingDto { State = state })],
                },
            };
        }

        [TestCase(Backlog)]
        [TestCase(Implementation)]
        [TestCase(Refining)]
        [TestCase("backlog")]
        [TestCase(" Implementation ")]
        public void A_To_Do_or_Doing_entry_is_accepted(string chosen)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(chosen));

            Assert.That(errors, Is.Empty);
        }

        [TestCase(Done)]
        [TestCase(Icebox)]
        [TestCase(Analysing)]
        public void A_state_outside_To_Do_and_Doing_is_refused_by_name(string chosen)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, chosen));

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{chosen}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
            }));
        }

        [Test]
        public void Every_refused_state_is_named()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Done, Backlog, Icebox));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errors, Has.Count.EqualTo(2));
                Assert.That(errors[0], Does.StartWith($"'{Done}'"));
                Assert.That(errors[1], Does.StartWith($"'{Icebox}'"));
            }
        }

        [Test]
        public void A_state_is_judged_against_the_states_in_the_same_save()
        {
            var settings = SettingsChoosing(Icebox);
            settings.ToDoStates = [Backlog, Icebox];

            var errors = RefinementSettingsValidator.ValidateSettings(settings);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Choosing_no_state_is_accepted()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing());

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void A_save_without_the_refinement_section_is_accepted()
        {
            var settings = SettingsChoosing();
            settings.Refinement = null;

            var errors = RefinementSettingsValidator.ValidateSettings(settings);

            Assert.That(errors, Is.Empty);
        }
    }
}
