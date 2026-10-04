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

        private static readonly RefinementSettings? NothingStored = null;

        private static RefinementSettings StoredStates(params string[] states)
            => new() { States = [.. states.Select(state => new RefinementStateSetting { State = state })] };

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
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, resent), StoredStates(Icebox));

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void A_newly_added_state_outside_To_Do_and_Doing_is_refused_next_to_a_kept_stored_one()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog, Icebox, Done), StoredStates(Backlog, Icebox));

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{Done}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
            }));
        }

        private static TeamSettingDto SettingsWithReadiness(int? minYes, int? minVoters, DiscussionRulesDto? discussWhen = null)
        {
            var settings = SettingsChoosing(Backlog);
            settings.Refinement!.Readiness = new ReadinessSettingDto
            {
                MinYes = minYes,
                MinVoters = minVoters,
                DiscussWhen = discussWhen,
            };
            return settings;
        }

        private static RefinementSettings StoredReadiness(int minYes, int minVoters)
            => new() { States = [new RefinementStateSetting { State = Backlog }], Readiness = new ReadinessSetting { MinYes = minYes, MinVoters = minVoters } };

        private static string NoRuleRefused(int threshold)
            => $"'{threshold}' cannot be the No votes that send a Work Item to discussion: a discussion rule needs at least 1 vote.";

        private static string YesIfRuleRefused(int threshold)
            => $"'{threshold}' cannot be the \"Yes, if…\" votes that send a Work Item to discussion: a discussion rule needs at least 1 vote.";

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

        [TestCase(1, 2)]
        [TestCase(5, 1)]
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(null, null)]
        public void Discussion_rules_of_at_least_one_vote_or_turned_off_are_accepted(int? no, int? yesIf)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithReadiness(3, 3, new DiscussionRulesDto { No = no, YesIf = yesIf }), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void A_No_rule_of_fewer_than_one_vote_is_refused_by_value(int threshold)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithReadiness(3, 3, new DiscussionRulesDto { No = threshold, YesIf = 2 }), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string> { NoRuleRefused(threshold) }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void A_Yes_if_rule_of_fewer_than_one_vote_is_refused_by_value(int threshold)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithReadiness(3, 3, new DiscussionRulesDto { No = 1, YesIf = threshold }), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string> { YesIfRuleRefused(threshold) }));
        }

        [Test]
        public void A_save_without_readiness_is_not_judged_against_the_defaults()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsChoosing(Backlog), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(5, null, 3, 3, "'3' cannot be the voters readiness needs: never fewer than the 5 Yes votes.")]
        [TestCase(null, 1, 3, 3, "'1' cannot be the voters readiness needs: never fewer than the 3 Yes votes.")]
        [TestCase(null, 4, 5, 6, "'4' cannot be the voters readiness needs: never fewer than the 5 Yes votes.")]
        public void A_readiness_save_leaving_out_one_side_of_the_voter_rule_is_judged_with_the_stored_side(
            int? minYes, int? minVoters, int storedMinYes, int storedMinVoters, string refusal)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithReadiness(minYes, minVoters), StoredReadiness(storedMinYes, storedMinVoters));

            Assert.That(errors, Is.EqualTo(new List<string> { refusal }));
        }

        [TestCase(2, null, 3, 3)]
        [TestCase(null, 6, 5, 5)]
        [TestCase(null, null, 2, 2)]
        public void A_readiness_save_leaving_out_one_side_of_the_voter_rule_is_accepted_when_the_stored_side_agrees(
            int? minYes, int? minVoters, int storedMinYes, int storedMinVoters)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithReadiness(minYes, minVoters), StoredReadiness(storedMinYes, storedMinVoters));

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void A_readiness_save_for_a_Team_without_refinement_is_judged_with_the_default_side()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithReadiness(5, null), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                "'3' cannot be the voters readiness needs: never fewer than the 5 Yes votes.",
            }));
        }

        private static readonly DateOnly StartingWeek = new(2026, 10, 5);

        private static TeamSettingDto SettingsWithCadence(int intervalWeeks, DateOnly? anchorWeek, params string[] weekdays)
        {
            var settings = SettingsChoosing(Backlog);
            settings.Refinement!.Cadence = new RefinementCadenceDto
            {
                Weekdays = [.. weekdays],
                IntervalWeeks = intervalWeeks,
                AnchorWeek = anchorWeek,
            };
            return settings;
        }

        private static string WeekdayRefused(string day)
            => $"'{day}' cannot be a Refinement day: only a weekday such as Monday can be chosen.";

        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(5, true)]
        public void A_cadence_of_a_week_or_more_with_a_starting_week_when_it_skips_weeks_is_accepted(int intervalWeeks, bool withStartingWeek)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithCadence(intervalWeeks, withStartingWeek ? StartingWeek : null, "Tuesday"), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void Skipping_weeks_without_a_starting_week_is_refused(int intervalWeeks)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithCadence(intervalWeeks, null, "Tuesday"), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string> { $"Refining every {intervalWeeks} weeks needs a starting week." }));
        }

        [TestCase(0, true)]
        [TestCase(0, false)]
        [TestCase(-1, true)]
        public void Fewer_than_one_week_between_Refinements_is_refused_by_value(int intervalWeeks, bool withStartingWeek)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(
                SettingsWithCadence(intervalWeeks, withStartingWeek ? StartingWeek : null, "Thursday"), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{intervalWeeks}' cannot be the weeks between Refinements: at least one week is needed.",
            }));
        }

        [TestCase("Monday")]
        [TestCase("Sunday")]
        [TestCase("saturday")]
        public void An_English_weekday_name_is_accepted_as_a_Refinement_day(string day)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithCadence(1, null, day), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [TestCase("Someday")]
        [TestCase("Thu 8 Oct")]
        [TestCase("Thu")]
        [TestCase("4")]
        [TestCase("Monday,Tuesday")]
        [TestCase("")]
        public void A_Refinement_day_that_is_not_a_weekday_name_is_refused_by_name(string day)
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithCadence(1, null, "Tuesday", day), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string> { WeekdayRefused(day) }));
        }

        [Test]
        public void A_cadence_without_weekdays_is_accepted()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithCadence(1, null), NothingStored);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Every_broken_cadence_rule_is_named()
        {
            var errors = RefinementSettingsValidator.ValidateSettings(SettingsWithCadence(-2, null, "Someday", "Thu"), NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                WeekdayRefused("Someday"),
                WeekdayRefused("Thu"),
                "'-2' cannot be the weeks between Refinements: at least one week is needed.",
            }));
        }

        [Test]
        public void Every_broken_readiness_rule_is_named_after_a_refused_state()
        {
            var settings = SettingsWithReadiness(0, -1, new DiscussionRulesDto { No = 0, YesIf = -2 });
            settings.Refinement!.States.Add(new RefinementStateSettingDto { State = Done });

            var errors = RefinementSettingsValidator.ValidateSettings(settings, NothingStored);

            Assert.That(errors, Is.EqualTo(new List<string>
            {
                $"'{Done}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.",
                "'0' cannot be the Yes votes readiness needs: at least one Yes is needed.",
                "'-1' cannot be the voters readiness needs: never fewer than the 0 Yes votes.",
                NoRuleRefused(0),
                YesIfRuleRefused(-2),
            }));
        }
    }
}
