using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemOptionsOwnerCategoryListsTest
    {
        private const string Pending = "pending: category lists hold raw states";

        private static string[] Listed(params string[] states) => states;

        private static string Folded(string state) => state.Trim().ToUpperInvariant();

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
        public void DisplayStatesFor_StatesPaddedWithTabsAndNonBreakingSpaces_AreRecognisedAsTheirGroup()
        {
            var team = ATeamWithReviewMapping("Analysis", "\tCode Review", " Testing ");

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
        public void DisplayStatesFor_AStateWhoseGroupWasDeleted_IsLeftExactlyAsStored()
        {
            var team = new Team { DoingStates = ["Y", "X"] };

            Assert.That(team.DisplayStatesFor(StateCategories.Doing), Is.EqualTo(Listed("Y", "X")));
        }

        [Test]
        [Ignore(Pending)]
        public void DisplayStatesFor_AnyGeneratedList_ShowsEachGroupOnceInTheOrderOfItsFirstStateAndLosesNoState()
        {
            foreach (var generated in StateListCases.Generate(300).Where(c => !c.HasShadowing))
            {
                // What the stored list stands for, worked out from the input alone and in the order it is read.
                var states = StatesTheStoredListStandsFor(generated);
                var expectedNames = states.Select(state => Folded(generated.Owner.MapRawStateToMappedName(state.Trim()))).Distinct().ToList();

                var shownKeys = generated.Owner.DisplayStatesFor(StateCategories.Doing).Select(Folded).ToList();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(shownKeys, Is.Unique, $"a name shown twice. {generated}");
                    Assert.That(shownKeys, Is.EqualTo(expectedNames), $"a state lost, invented or out of place. {generated}");
                }
            }
        }

        private static List<string> StatesTheStoredListStandsFor(StateListCase generated)
        {
            var states = new List<string>();
            foreach (var entry in generated.StoredDoing.Where(listed => !string.IsNullOrWhiteSpace(listed)))
            {
                var name = entry.Trim();
                var group = generated.Owner.StateMappings.FirstOrDefault(mapping => mapping.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (group is null || generated.NamedEntries[group.Name] == ListEntryKind.StateOfItsOwnMapping)
                {
                    states.Add(name);
                }

                if (group is not null)
                {
                    states.AddRange(group.States);
                }
            }

            return states;
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
        public void CategoryOfRawState_ListStillHoldsTheGroupNameInPlaceOfItsStates_FindsTheStatesInThatList()
        {
            var team = ATeamWithReviewMapping("Analysis", "Review", "Waiting");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.CategoryOfRawState("Code Review"), Is.EqualTo(StateCategories.Doing));
                Assert.That(team.CategoryOfRawState("Testing"), Is.EqualTo(StateCategories.Doing));
                Assert.That(team.CategoryOfRawState("Review"), Is.EqualTo(StateCategories.Unknown), "The group's name is no state of the tracker.");
            }
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
        public void CategoryOfDisplayName_ListStillHoldsTheGroupNameInPlaceOfItsStates_IsTheCategoryOfThatList()
        {
            var team = ATeamWithReviewMapping("Analysis", "Review", "Waiting");

            Assert.That(team.CategoryOfDisplayName("Review"), Is.EqualTo(StateCategories.Doing));
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

        // A stored list that still holds a group's name (never rewritten, skipped by the start-up pass, or saved by
        // an older instance) is read through the classifier, so every item of the grouped states still loads.
        [Test]
        public void AllStates_StoredListHoldsTheGroupNameInPlaceOfItsStates_ReadsTheStatesThrough()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review", "Waiting"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.AllStates, Is.EquivalentTo(Listed("Backlog", "Analysis", "Code Review", "Testing", "Waiting", "Done")));
        }

        [Test]
        public void OpenStates_StoredListHoldsTheGroupNameInPlaceOfItsStates_ReadsTheStatesThrough()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.OpenStates, Is.EquivalentTo(Listed("Backlog", "Analysis", "Code Review", "Testing")));
        }

        [Test]
        public void AllStates_StoredListHoldsAGroupNameBesideOneOfItsStates_ReadsTheLegacyExpansion()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["In Arbeit", "In Progress"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });

            Assert.That(team.AllStates, Is.EquivalentTo(Listed("Backlog", "In Progress", "Done")));
        }

        [Test]
        [Ignore(Pending)]
        public void AllStates_OwnerTheStartUpPassCouldNotRead_StillHoldsTheStatesOfTheOtherEntries()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review", "Waiting"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = null! });

            Assert.That(() => team.AllStates.ToList(), Throws.Nothing);
            Assert.That(team.AllStates, Is.SupersetOf(Listed("Backlog", "Analysis", "Waiting", "Done")));
        }

        [Test]
        public void AllStates_ReadTwiceOverAListTheStartUpPassAlreadyRewrote_IsUnchanged()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Code Review", "Testing", "Waiting"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(team.AllStates, Is.EquivalentTo(Listed("Backlog", "Analysis", "Code Review", "Testing", "Waiting", "Done")));
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
