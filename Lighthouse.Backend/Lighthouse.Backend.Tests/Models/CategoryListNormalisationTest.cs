using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class CategoryListNormalisationTest
    {
        private static string[] Listed(params string[] states) => states;

        private static HashSet<string> Folded(IEnumerable<string> states) => states.Select(state => state.Trim().ToUpperInvariant()).ToHashSet();

        private const string Pending = "pending: category lists hold raw states";

        private static Team ATeamWithActiveGroup(params string[] doing)
        {
            var team = new Team { DoingStates = [.. doing] };
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });
            return team;
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_GroupNamedAfterItsOwnState_ListsTheOtherStatesRightAfterIt()
        {
            var team = ATeamWithActiveGroup("Analysis", "Active", "Blocked");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_ListThatAlreadyHoldsEveryState_PassesThroughInTheSameOrder()
        {
            var team = ATeamWithActiveGroup("Resolved", "Analysis", "Active", "Blocked");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Resolved", "Analysis", "Active", "Blocked")));
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_SameStateInDifferentCasing_KeepsTheFirstSpelling()
        {
            var team = ATeamWithActiveGroup("active", "Analysis", "ACTIVE");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("active", "Resolved", "Analysis")));
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_NoMappings_ReturnsTheListWithoutRepeats()
        {
            var team = new Team { DoingStates = ["Analysis", "Testing", "analysis"] };

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Testing")));
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_EmptyList_StaysEmpty()
        {
            var team = ATeamWithActiveGroup();

            Assert.That(team.NormalisedStatesFor([]), Is.Empty);
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_AnyGeneratedList_MeansTheSameStatesAsTheListBeforeIt()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var before = generated.Owner.GetRawStatesForCategory(generated.StoredDoing);
                var after = generated.Owner.GetRawStatesForCategory(generated.Owner.NormalisedStatesFor(generated.StoredDoing));

                Assert.That(Folded(after), Is.EquivalentTo(Folded(before)), generated.ToString());
            }
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_AnyGeneratedList_ChangesNothingTheSecondTime()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var once = generated.Owner.NormalisedStatesFor(generated.StoredDoing);
                var twice = generated.Owner.NormalisedStatesFor(once);

                Assert.That(twice, Is.EqualTo(once), generated.ToString());
            }
        }

        [Test]
        [Ignore(Pending)]
        public void NormalisedStatesFor_AnyGeneratedList_NeverNamesAGroupThatIsNotOneOfItsOwnStates()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var once = generated.Owner.NormalisedStatesFor(generated.StoredDoing);
                var groupNamesThatAreNoState = generated.Owner.StateMappings
                    .Where(mapping => !mapping.States.Contains(mapping.Name, StringComparer.OrdinalIgnoreCase))
                    .Select(mapping => mapping.Name);

                Assert.That(once.Select(state => state.Trim()), Has.None.Matches<string>(state => groupNamesThatAreNoState.Contains(state, StringComparer.OrdinalIgnoreCase)), generated.ToString());
            }
        }

        [Test]
        [Ignore(Pending)]
        public void AnOlderBackend_ReadingANormalisedList_FindsTheSameStatesAsBeforeTheUpgrade()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var stored = generated.Owner.GetRawStatesForCategory(generated.StoredDoing);
                var normalised = generated.Owner.NormalisedStatesFor(generated.StoredDoing);

                var olderBackendReads = generated.Owner.GetRawStatesForCategory(normalised);

                Assert.That(Folded(olderBackendReads), Is.EquivalentTo(Folded(stored)), generated.ToString());
            }
        }
    }
}
