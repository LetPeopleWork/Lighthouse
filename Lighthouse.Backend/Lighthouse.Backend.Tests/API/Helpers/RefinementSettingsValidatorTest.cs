using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models.Refinement;

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

        private static readonly string[] NothingStored = [];

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
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(chosen), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(Done)]
        [TestCase(Icebox)]
        [TestCase(Analysing)]
        public void A_state_outside_To_Do_and_Doing_is_refused_by_name(string chosen)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, chosen), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{chosen}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
            }));
        }

        [Test]
        public void Every_refused_state_is_named()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Done, Backlog, Icebox), NothingStored);

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

            var errors = RefinementSettingsValidator.ValidateSettings(settings, NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Choosing_no_state_is_accepted()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void A_save_without_the_refinement_section_is_accepted()
        {
            var settings = SettingsChoosing();
            settings.Refinement = null;

            var errors = RefinementSettingsValidator.ValidateSettings(settings, NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(Icebox)]
        [TestCase("icebox")]
        [TestCase(" Icebox ")]
        public void A_stored_state_that_stopped_being_mapped_is_accepted_when_sent_again(string resent)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, resent), [Icebox]);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void A_newly_added_state_outside_To_Do_and_Doing_is_refused_next_to_a_kept_stored_one()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, Icebox, Done), [Backlog, Icebox]);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{Done}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
            }));
        }

        private static TeamSettingDto SettingsWithReadiness(int? minYes, int? minVoters, int? vetoThreshold = null)
        {
            var settings = SettingsChoosing(Backlog);
            settings.Refinement!.Readiness = new ReadinessSettingDto
            {
                MinYes = minYes,
                MinVoters = minVoters,
                Veto = vetoThreshold is null ? null : new VetoSettingDto { Threshold = vetoThreshold, Counts = VetoCounts.No },
            };
            return settings;
        }

        [TestCase(1, 1)]
        [TestCase(1, 5)]
        [TestCase(3, 3)]
        [TestCase(2, 3)]
        public void Readiness_with_at_least_one_Yes_and_as_many_voters_is_accepted(int minYes, int minVoters)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(minYes, minVoters), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Fewer_than_one_Yes_is_refused_by_value(int minYes)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(minYes, 3), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{minYes}' cannot be the Yes votes readiness needs: at least one Yes is needed.",
            }));
        }

        [TestCase(1, 0)]
        [TestCase(3, 2)]
        public void Fewer_voters_than_Yes_votes_is_refused_by_value(int minYes, int minVoters)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(minYes, minVoters), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{minVoters}' cannot be the voters readiness needs: never fewer than the {minYes} Yes votes.",
            }));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void A_veto_of_at_least_one_vote_is_accepted(int threshold)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(3, 3, threshold), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void A_veto_of_fewer_than_one_vote_is_refused_by_value(int threshold)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(3, 3, threshold), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{threshold}' cannot be the votes a veto needs: at least one vote is needed.",
            }));
        }

        [Test]
        public void A_save_without_readiness_is_not_judged_against_the_defaults()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(5, null)]
        [TestCase(null, 1)]
        [TestCase(null, null)]
        public void A_readiness_save_leaving_out_one_side_of_the_voter_rule_is_not_judged_on_it(int? minYes, int? minVoters)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(minYes, minVoters), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Every_broken_readiness_rule_is_named_after_a_refused_state()
        {
            var settings = SettingsWithReadiness(0, -1, 0);
            settings.Refinement!.States.Add(new RefinementStateSettingDto { State = Done });

            var errors = RefinementSettingsValidator.ValidateSettings(settings, NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{Done}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
                "'0' cannot be the Yes votes readiness needs: at least one Yes is needed.",
                "'-1' cannot be the voters readiness needs: never fewer than the 0 Yes votes.",
                "'0' cannot be the votes a veto needs: at least one vote is needed.",
            }));
        }
    }
}
