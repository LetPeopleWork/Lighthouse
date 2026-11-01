using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class CategoryListNormalisationTest
    {
        private const char NonBreakingSpace = (char)160;

        private static string[] Listed(params string[] states) => states;

        private static HashSet<string> Folded(IEnumerable<string> states) => states.Select(state => state.Trim().ToUpperInvariant()).ToHashSet();

        private static Team ATeamWithActiveGroup(params string[] doing)
        {
            var team = new Team { DoingStates = [.. doing] };
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });
            return team;
        }

        private static Team ATeamWithReviewGroup(params string[] doing)
        {
            var team = new Team { DoingStates = [.. doing] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void LegacyShape_AMappingWithNoStates_IsAnEmptyGroup(bool statesAreNull)
        {
            var team = ATeamWithReviewGroup("Analysis", "Review");
            team.StateMappings.Add(new StateMapping { Name = "Waiting", States = statesAreNull ? null! : [] });

            Assert.That(team.CategoryLists().LegacyShape(), Is.EqualTo(LegacyListShape.EmptyGroup));
        }

        [Test]
        public void LegacyShape_AGroupNameBesideOneOfItsStates_IsShadowing()
        {
            var team = ATeamWithReviewGroup("Review", "Analysis", "Testing");

            Assert.That(team.CategoryLists().LegacyShape(), Is.EqualTo(LegacyListShape.Shadowing));
        }

        [Test]
        public void LegacyShape_AListedGroupHoldingAnotherGroupsName_IsChained()
        {
            var team = new Team { DoingStates = ["B", "Analysis"] };
            team.StateMappings.Add(new StateMapping { Name = "A", States = ["Fix"] });
            team.StateMappings.Add(new StateMapping { Name = "B", States = ["A"] });

            Assert.That(team.CategoryLists().LegacyShape(), Is.EqualTo(LegacyListShape.Chained));
        }

        [Test]
        [TestCase("Analysis", "Review", "Waiting")]
        [TestCase("Analysis", "Code Review", "Testing")]
        public void LegacyShape_ACollapsedGroupOrRawStates_IsNone(params string[] doing)
        {
            var team = ATeamWithReviewGroup(doing);

            Assert.That(team.CategoryLists().LegacyShape(), Is.Null);
        }

        [Test]
        public void ClassifyListEntry_NameOfAGroupThatIsOneOfItsOwnStates_IsAStateOfTheTracker()
        {
            var team = ATeamWithActiveGroup("Analysis", "Active", "Blocked");

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Active"), Is.EqualTo(ListEntryKind.StateOfItsOwnMapping));
        }

        [Test]
        public void ClassifyListEntry_NameOfAGroupWhoseStatesAreNotListed_IsACollapsedGroup()
        {
            var team = ATeamWithReviewGroup("Analysis", "Review", "Waiting");

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Review"), Is.EqualTo(ListEntryKind.CollapsedGroup));
        }

        [Test]
        public void ClassifyListEntry_NameOfAGroupBesideOneOfItsStates_IsShadowing()
        {
            var team = ATeamWithReviewGroup("Review", "Analysis", "Testing");

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Review"), Is.EqualTo(ListEntryKind.Shadowing));
        }

        [Test]
        public void ClassifyListEntry_StateOfTheGroupIsListedOnlyInAnotherList_IsStillACollapsedGroup()
        {
            var team = ATeamWithReviewGroup("Analysis", "Review");
            team.DoneStates = ["Testing"];

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Review"), Is.EqualTo(ListEntryKind.CollapsedGroup));
        }

        [Test]
        public void ClassifyListEntry_NameWrittenInOtherCasingWithSpaces_IsRecognisedAsTheGroup()
        {
            var team = ATeamWithReviewGroup("Analysis", "  REVIEW\t");

            Assert.That(team.ClassifyListEntry(team.DoingStates, "  REVIEW\t"), Is.EqualTo(ListEntryKind.CollapsedGroup));
        }

        [Test]
        public void ClassifyListEntry_NameOfNoGroup_IsAStateOfTheTracker()
        {
            var team = ATeamWithReviewGroup("Analysis", "Review");

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Analysis"), Is.EqualTo(ListEntryKind.NotAGroupName));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ClassifyListEntry_NameOfAMappingWithNoStates_IsAStateOfTheTracker(bool statesAreNull)
        {
            var team = new Team { DoingStates = ["Analysis", "Review"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = statesAreNull ? null! : [] });

            Assert.That(team.ClassifyListEntry(team.DoingStates, "Review"), Is.EqualTo(ListEntryKind.NotAGroupName));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void NormalisedStatesFor_NameOfAMappingWithNoStates_StaysInTheList(bool statesAreNull)
        {
            var team = new Team { DoingStates = ["Analysis", "Review", "Waiting"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = statesAreNull ? null! : [] });

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
        }

        [Test]
        public void ClassifyListEntry_AnyGeneratedList_NamesEveryGroupEntryTheWayItWasBuilt()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                foreach (var (name, expected) in generated.NamedEntries)
                {
                    var entry = generated.StoredDoing.First(listed => listed.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));

                    Assert.That(generated.Owner.ClassifyListEntry(generated.StoredDoing, entry), Is.EqualTo(expected), $"{name}. {generated}");
                }
            }
        }

        [Test]
        public void NormalisedStatesFor_GroupNamedAfterItsOwnState_ListsTheOtherStatesRightAfterIt()
        {
            var team = ATeamWithActiveGroup("Analysis", "Active", "Blocked");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
        }

        [Test]
        public void NormalisedStatesFor_CollapsedGroup_IsReplacedByItsStatesInPlace()
        {
            var team = ATeamWithReviewGroup("Analysis", "Review", "Waiting");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
        }

        [Test]
        public void NormalisedStatesFor_GroupNameListedBesideOneOfItsStates_ReadsTheLegacyExpansion()
        {
            var team = ATeamWithReviewGroup("Review", "Testing", "X");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Code Review", "Testing", "X")));
        }

        [Test]
        public void ClassifyListEntry_GroupNameThatIsAStateOfAnotherGroup_IsAStateOfTheTracker()
        {
            var team = ATeamWithReviewGroup("Testing");
            team.StateMappings.Add(new StateMapping { Name = "Testing", States = ["QA"] });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ClassifyListEntry(team.DoingStates, "Testing"), Is.EqualTo(ListEntryKind.NotAGroupName));
                Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Testing")));
            }
        }

        [Test]
        public void NormalisedStatesFor_ChainedGroups_ExpandOneLevelAndThenChangeNothing()
        {
            var team = new Team { DoingStates = ["B", "Analysis"] };
            team.StateMappings.Add(new StateMapping { Name = "A", States = ["Fix"] });
            team.StateMappings.Add(new StateMapping { Name = "B", States = ["A"] });

            var once = team.NormalisedStatesFor(team.DoingStates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(once, Is.EqualTo(Listed("A", "Analysis")));
                Assert.That(team.NormalisedStatesFor(once), Is.EqualTo(once));
            }
        }

        [Test]
        [TestCase("\u212A", "k")]
        [TestCase("\u00DF", "SS")]
        public void NormalisedStatesFor_NamesOnlyAFullCaseFoldWouldJoin_AreDifferentStates(string first, string second)
        {
            var team = new Team();

            Assert.That(team.NormalisedStatesFor([first, second]), Is.EqualTo(Listed(first, second)));
        }

        [Test]
        public void NormalisedStatesFor_ListThatAlreadyHoldsEveryState_PassesThroughInTheSameOrder()
        {
            var team = ATeamWithActiveGroup("Resolved", "Analysis", "Active", "Blocked");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Resolved", "Analysis", "Active", "Blocked")));
        }

        [Test]
        public void NormalisedStatesFor_SameStateInDifferentCasing_KeepsTheFirstSpelling()
        {
            var team = ATeamWithActiveGroup("active", "Analysis", "ACTIVE");

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("active", "Resolved", "Analysis")));
        }

        [Test]
        public void NormalisedStatesFor_NoMappings_ReturnsTheListWithoutRepeats()
        {
            var team = new Team { DoingStates = ["Analysis", "Testing", "analysis"] };

            Assert.That(team.NormalisedStatesFor(team.DoingStates), Is.EqualTo(Listed("Analysis", "Testing")));
        }

        [Test]
        public void NormalisedStatesFor_EmptyList_StaysEmpty()
        {
            var team = ATeamWithActiveGroup();

            Assert.That(team.NormalisedStatesFor([]), Is.Empty);
        }

        [Test]
        public void NormalisedStatesFor_EntriesThatAreNullEmptyOrOnlyWhitespace_AreLeftOut()
        {
            var team = new Team();

            Assert.That(team.NormalisedStatesFor(["Analysis", null!, "", "   ", "\t", NonBreakingSpace.ToString(), "Testing"]), Is.EqualTo(Listed("Analysis", "Testing")));
        }

        [Test]
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
        public void NormalisedStatesFor_AnyGeneratedList_KeepsAGroupNameOnlyWhereItIsOneOfTheGroupsOwnStates()
        {
            foreach (var generated in StateListCases.Generate(300))
            {
                var once = generated.Owner.NormalisedStatesFor(generated.StoredDoing);
                var namesThatAreNoState = generated.Owner.StateMappings
                    .Where(mapping => !mapping.States.Any(state => state.Trim().Equals(mapping.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(mapping => mapping.Name);

                Assert.That(once.Select(state => state.Trim()), Has.None.Matches<string>(state => namesThatAreNoState.Contains(state, StringComparer.OrdinalIgnoreCase)), generated.ToString());
            }
        }

        [Test]
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
