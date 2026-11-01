using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemOptionsOwnerCategoryListsTest
    {
        private static string[] Listed(params string[] states) => states;

        private const string Pending = "pending: category lists hold raw states";

        private static Team ATeamWithReviewMapping(params string[] doing)
        {
            var team = new Team { DoingStates = [.. doing], ToDoStates = ["Backlog"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_MembersNextToEachOther_ShowsOneGroupInTheirPlace()
        {
            var team = ATeamWithReviewMapping("Analysis", "Code Review", "Testing", "Waiting");

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_MembersApart_ShowsTheGroupWhereItsFirstMemberIs()
        {
            var team = ATeamWithReviewMapping("Code Review", "Analysis", "Testing", "Waiting");

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Review", "Analysis", "Waiting")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_LaterMemberBeforeEarlierOne_ShowsTheGroupAtTheFirstListedMember()
        {
            var team = ATeamWithReviewMapping("Waiting", "Testing", "Analysis", "Code Review");

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Waiting", "Review", "Analysis")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_SameStateListedTwiceInDifferentCasingAndSpacing_ShowsItOnce()
        {
            var team = ATeamWithReviewMapping("Analysis", "analysis ", "  CODE REVIEW", "testing ");

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Analysis", "Review")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_GroupNamedAfterOneOfItsOwnStates_ShowsTheGroupOnce()
        {
            var team = new Team { DoingStates = ["Active", "Resolved", "Blocked"] };
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Active", "Blocked")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_GroupWhoseStatesAreListedNowhere_LeavesTheListAlone()
        {
            var team = ATeamWithReviewMapping("Analysis", "Waiting");

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Analysis", "Waiting")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_NoMappings_ReturnsTheListAsItIs()
        {
            var team = new Team { DoingStates = ["Analysis", "Testing"] };

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Analysis", "Testing")));
        }

        [Test]
        [TestCase(StateCategories.ToDo, new[] { "Intake", "Review" })]
        [TestCase(StateCategories.Doing, new[] { "Analysis" })]
        [TestCase(StateCategories.Done, new[] { "Released", "Review" })]
        [Ignore(Pending)]
        public void DisplayStatesFor_AnyCategory_GroupsThatCategorysListOnly(StateCategories category, string[] expected)
        {
            var team = new Team
            {
                ToDoStates = ["Intake", "Code Review", "Testing"],
                DoingStates = ["Analysis"],
                DoneStates = ["Released", "Testing", "Code Review"],
            };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.DisplayStatesFor(category), Is.EqualTo(expected));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_AfterTheMappingThatGroupedAStateIsDeleted_LeavesNoGhostName()
        {
            var team = new Team { DoingStates = ["Y", "X"] };
            team.StateMappings.Add(new StateMapping { Name = "B", States = ["X"] });

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Y", "B")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_AnyGeneratedList_ShowsEachGroupOnceAtItsFirstMemberAndLosesNoState()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var normalised = generated.Owner.NormalisedStatesFor(generated.StoredDoing);
                generated.Owner.DoingStates = normalised;

                var expected = normalised
                    .Select(generated.Owner.MapRawStateToMappedName)
                    .DistinctBy(name => name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Assert.That(generated.Owner.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(expected), generated.ToString());
            }
        }

        [Test]
        [TestCase("Analysis", StateCategories.Doing)]
        [TestCase("analysis ", StateCategories.Doing)]
        [TestCase("Backlog", StateCategories.ToDo)]
        [TestCase("Done", StateCategories.Done)]
        [TestCase("Testing", StateCategories.Doing)]
        [TestCase("Jellybean", StateCategories.Unknown)]
        [Ignore(Pending)]
        public void CategoryOfRawState_ReadsTheListsAsTheyAre(string rawState, StateCategories expected)
        {
            var team = ATeamWithReviewMapping("Analysis", "Code Review", "Testing");

            Assert.That(team.CategoryOfRawState(rawState), Is.EqualTo(expected));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfRawState_MappingNameThatIsNoStateOfTheTracker_IsUnknown()
        {
            var team = ATeamWithReviewMapping("Analysis", "Code Review", "Testing");

            Assert.That(team.CategoryOfRawState("Review"), Is.EqualTo(StateCategories.Unknown));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfRawState_RawStateThatSharesAMappingsName_KeepsItsOwnCategory()
        {
            var team = new Team { DoingStates = ["In Arbeit"], ToDoStates = ["Backlog"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });

            Assert.That(team.CategoryOfRawState("In Arbeit"), Is.EqualTo(StateCategories.Doing));
        }

        [Test]
        [TestCase("Review", StateCategories.Doing)]
        [TestCase(" review ", StateCategories.Doing)]
        [TestCase("Analysis", StateCategories.Doing)]
        [TestCase("Backlog", StateCategories.ToDo)]
        [TestCase("Jellybean", StateCategories.Unknown)]
        [Ignore(Pending)]
        public void CategoryOfDisplayName_MappingNameOrRawState_ReadsTheCategoryOfWhatItStandsFor(string name, StateCategories expected)
        {
            var team = ATeamWithReviewMapping("Analysis", "Code Review", "Testing");

            Assert.That(team.CategoryOfDisplayName(name), Is.EqualTo(expected));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfDisplayName_MappingWhoseStatesSitInDifferentCategories_IsUnknown()
        {
            var team = new Team { ToDoStates = ["Code Review"], DoingStates = ["Testing"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.CategoryOfDisplayName("Review"), Is.EqualTo(StateCategories.Unknown));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfDisplayName_MappingWithOneStateListedNowhere_FollowsTheStatesThatAreListed()
        {
            var team = new Team { DoingStates = ["Code Review"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.CategoryOfDisplayName("Review"), Is.EqualTo(StateCategories.Doing));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfDisplayName_MappingWhoseStatesAreAllListedNowhere_IsUnknown()
        {
            var team = new Team { DoingStates = ["Analysis"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.CategoryOfDisplayName("Review"), Is.EqualTo(StateCategories.Unknown));
        }

        [Test]
        [Ignore(Pending)]
        public void CategoryOfDisplayName_GroupNamedAfterOneOfItsOwnStates_IsTheCategoryOfItsStates()
        {
            var team = new Team { DoingStates = ["Active", "Resolved"] };
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });

            Assert.That(team.CategoryOfDisplayName("Active"), Is.EqualTo(StateCategories.Doing));
        }

        [Test]
        [Ignore(Pending)]
        public void AllStates_ListHoldsAStateNamedLikeAMapping_KeepsThatStateAndDoesNotAddTheMappingsStates()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["In Arbeit"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });

            Assert.That(team.AllStates, Is.EquivalentTo(Listed("Backlog", "In Arbeit", "Done")));
        }

        [Test]
        [Ignore(Pending)]
        public void OpenStates_ListHoldsAStateNamedLikeAMapping_ReadsToDoAndDoingAsTheyAre()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["In Arbeit"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });

            Assert.That(team.OpenStates, Is.EquivalentTo(Listed("Backlog", "In Arbeit")));
        }

        [Test]
        [Ignore(Pending)]
        public void AllStates_RawListWithAMappingOverIt_HoldsEveryRawStateOnce()
        {
            var team = ATeamWithReviewMapping("Analysis", "Code Review", "Testing");

            Assert.That(team.AllStates, Is.EquivalentTo(Listed("Backlog", "Analysis", "Code Review", "Testing", "Done")));
        }
    }
}
