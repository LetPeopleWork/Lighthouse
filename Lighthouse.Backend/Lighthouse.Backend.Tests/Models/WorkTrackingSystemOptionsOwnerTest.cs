using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemOptionsOwnerTest
    {
        [Test]
        [TestCase("Prioritized", StateCategories.ToDo)]
        [TestCase("Analysis In Progress", StateCategories.Doing)]
        [TestCase("Delivered", StateCategories.Done)]
        [TestCase("Jellybean", StateCategories.Unknown)]
        [TestCase("New", StateCategories.ToDo)]
        [TestCase("NEW", StateCategories.ToDo)]
        [TestCase("new", StateCategories.ToDo)]
        [TestCase("nEw", StateCategories.ToDo)]
        public void MapStateToStateCategory_MapsCorrect(string state, StateCategories expectedStateCategory)
        {
            var subject = CreateSubject();
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New", "Prioritized"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Analysis In Progress", "Implementation"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Delivered"]);

            var stateCategory = subject.MapStateToStateCategory(state);

            Assert.That(stateCategory, Is.EqualTo(expectedStateCategory));
        }

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesSLE()
        {
            var subject = CreateSubject();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ServiceLevelExpectationProbability, Is.Zero);
                Assert.That(subject.ServiceLevelExpectationRange, Is.Zero);
            }
        }

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesWIPLimit()
        {
            var subject = CreateSubject();
            Assert.That(subject.SystemWIPLimit, Is.Zero);
        }

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesEmptyStateMappings()
        {
            var subject = CreateSubject();
            Assert.That(subject.StateMappings, Is.Empty);
        }

        [Test]
        public void StateMappings_CanAddMapping()
        {
            var subject = CreateSubject();
            var mapping = new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] };

            subject.StateMappings.Add(mapping);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.StateMappings, Has.Count.EqualTo(1));
                Assert.That(subject.StateMappings[0].Name, Is.EqualTo("In Progress"));
                Assert.That(subject.StateMappings[0].States, Is.EquivalentTo(["Active", "Resolved"]));
            }
        }

        [Test]
        public void GetRawStatesForCategory_NoMappings_ReturnsSameStates()
        {
            var subject = CreateSubject();
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active", "Resolved"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved"]));
        }

        [Test]
        public void GetRawStatesForCategory_WithMappedLabel_ExpandsToRawStates()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved"]));
        }

        [Test]
        public void GetRawStatesForCategory_MixedRawAndMapped_ExpandsOnlyMappedLabels()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress", "Development"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved", "Development"]));
        }

        [Test]
        public void GetRawStatesForCategory_MappedLabelCaseInsensitive_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["in progress"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active"]));
        }

        [Test]
        [TestCase("Active", StateCategories.Doing)]
        [TestCase("Resolved", StateCategories.Doing)]
        [TestCase("Development", StateCategories.Doing)]
        [TestCase("New", StateCategories.ToDo)]
        [TestCase("Closed", StateCategories.Done)]
        [TestCase("Unknown", StateCategories.Unknown)]
        public void MapStateToStateCategory_WithMappings_ExpandsAndMatchesRawState(string state, StateCategories expected)
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress", "Development"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            var result = subject.MapStateToStateCategory(state);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void MapStateToStateCategory_WithMappingInToDoStates_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "Backlog", States = ["New", "Prioritized"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["Backlog"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.MapStateToStateCategory("New"), Is.EqualTo(StateCategories.ToDo));
                Assert.That(subject.MapStateToStateCategory("Prioritized"), Is.EqualTo(StateCategories.ToDo));
                Assert.That(subject.MapStateToStateCategory("Active"), Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        public void MapStateToStateCategory_WithMappingInDoneStates_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "Completed", States = ["Closed", "Delivered"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Completed"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.MapStateToStateCategory("Closed"), Is.EqualTo(StateCategories.Done));
                Assert.That(subject.MapStateToStateCategory("Delivered"), Is.EqualTo(StateCategories.Done));
            }
        }

        [Test]
        public void MapRawStateToMappedName_NoMappings_ReturnsSameState()
        {
            var subject = CreateSubject();

            var result = subject.MapRawStateToMappedName("Active");

            Assert.That(result, Is.EqualTo("Active"));
        }

        [Test]
        public void MapRawStateToMappedName_WithMapping_ReturnsMappedName()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });

            var result = subject.MapRawStateToMappedName("Active");

            Assert.That(result, Is.EqualTo("In Progress"));
        }

        [Test]
        public void MapRawStateToMappedName_NoMatchingMapping_ReturnsSameState()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });

            var result = subject.MapRawStateToMappedName("New");

            Assert.That(result, Is.EqualTo("New"));
        }

        [Test]
        public void MapRawStateToMappedName_CaseInsensitive_ReturnsMappedName()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active"] });

            var result = subject.MapRawStateToMappedName("active");

            Assert.That(result, Is.EqualTo("In Progress"));
        }

        [Test]
        public void AllStates_NoMappings_ReturnsConfiguredStates()
        {
            var subject = CreateSubject();
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            var result = subject.AllStates.ToList();

            Assert.That(result, Is.EquivalentTo(["New", "Active", "Closed"]));
        }

        [Test]
        public void AllStates_WithMappings_ReturnsRawExpandedStates()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            var result = subject.AllStates.ToList();

            Assert.That(result, Is.EquivalentTo(["New", "Active", "Resolved", "Closed"]));
        }

        [Test]
        public void OpenStates_WithMappings_ReturnsRawExpandedStates()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress"]);

            var result = subject.OpenStates.ToList();

            Assert.That(result, Is.EquivalentTo(["New", "Active", "Resolved"]));
        }

        [TestCase(null)]
        [TestCase(4)]
        public void Owning_a_Parent_Override_Field_keeps_the_value_and_marks_it_the_owners(int? fieldId)
        {
            var subject = CreateSubject();

            subject.OwnParentOverride(fieldId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ParentOverrideSource, Is.EqualTo(SettingSource.Own));
                Assert.That(subject.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(fieldId));
                Assert.That(subject.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(fieldId));
            }
        }

        [Test]
        public void Following_the_default_clears_the_owners_own_Parent_Override_Field()
        {
            var subject = CreateSubject();
            subject.OwnParentOverride(4);

            subject.FollowParentOverrideDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ParentOverrideSource, Is.EqualTo(SettingSource.ConnectionDefault));
                Assert.That(subject.OwnParentOverrideAdditionalFieldDefinitionId, Is.Null);
            }
        }

        [Test]
        public void An_owner_nobody_has_set_follows_the_default()
        {
            var subject = CreateSubject();

            Assert.That(subject.ParentOverrideSource, Is.EqualTo(SettingSource.ConnectionDefault));
        }

        [Test]
        public void A_sent_field_of_its_own_is_the_field_the_owner_would_hold()
        {
            var subject = CreateSubject();

            Assert.That(subject.ParentOverrideOwnedOnceTaken(SettingSource.Own, 4), Is.EqualTo(4));
        }

        [Test]
        public void A_Team_that_follows_the_default_would_hold_no_field_of_its_own_even_one_its_row_still_carries()
        {
            var team = new Team();
            StoreOwnParentOverrideAsReadFromTheDatabase(team, 4);

            Assert.That(team.ParentOverrideOwnedOnceTaken(null, null), Is.Null);
        }

        [Test]
        public void Taking_a_sent_follow_the_default_makes_an_owner_that_owned_a_field_follow()
        {
            var subject = CreateSubject();
            subject.OwnParentOverride(4);

            subject.TakeParentOverrideAsSent(SettingSource.ConnectionDefault, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ParentOverrideSource, Is.EqualTo(SettingSource.ConnectionDefault));
                Assert.That(subject.OwnParentOverrideAdditionalFieldDefinitionId, Is.Null);
            }
        }

        private static readonly string[] DefaultToDoStates = ["New", "Proposed", "To Do"];

        private static readonly string[] DefaultDoingStates = ["Active", "Resolved", "In Progress", "Committed"];

        private static readonly string[] DefaultDoneStates = ["Done", "Closed"];

        private static readonly string[] OwnedCategoryValues = ["S", "M"];

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_StartsWithTheCommonStateNamesAndNoQuery()
        {
            var subject = CreateSubject();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ToDoStates, Is.EqualTo(DefaultToDoStates));
                Assert.That(subject.DoingStates, Is.EqualTo(DefaultDoingStates));
                Assert.That(subject.DoneStates, Is.EqualTo(DefaultDoneStates));
                Assert.That(subject.DataRetrievalValue, Is.Empty);
            }
        }

        [Test]
        public void FollowsConnectionDefault_AnOwnerThatOffersNoDefaults_FollowsNone()
        {
            var subject = CreateSubject();

            Assert.That(Enum.GetValues<ConnectionDefaultSetting>().Where(subject.FollowsConnectionDefault), Is.Empty);
        }

        [Test]
        public void OwnEstimation_StoresTheWholeBundleAsTheOwnersOwn()
        {
            var subject = CreateSubject();

            subject.OwnEstimation(new EstimationBundle(12, "Points", true, ["S", "M"]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.EstimationSource, Is.EqualTo(SettingSource.Own));
                Assert.That(subject.OwnEstimationAdditionalFieldDefinitionId, Is.EqualTo(12));
                Assert.That(subject.OwnEstimationUnit, Is.EqualTo("Points"));
                Assert.That(subject.OwnUseNonNumericEstimation, Is.True);
                Assert.That(subject.OwnEstimationCategoryValues, Is.EqualTo(OwnedCategoryValues));
            }
        }

        [Test]
        public void RefreshUpdateTime_RecordsTheGivenInstant_AndResetForgetsIt()
        {
            var subject = CreateSubject();
            var refreshedAt = new DateTime(2026, 7, 27, 23, 30, 0, DateTimeKind.Utc);

            subject.RefreshUpdateTime(refreshedAt);
            var afterRefresh = subject.UpdateTime;
            subject.ResetUpdateTime();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterRefresh, Is.EqualTo(refreshedAt));
                Assert.That(subject.UpdateTime, Is.EqualTo(DateTime.MinValue));
            }
        }

        [TestCase("New", "Done", true)]
        [TestCase("Jellybean", "Done", false)]
        [TestCase("New", "Jellybean", false)]
        [TestCase("Nothing", "Done", false)]
        [TestCase("New", "Nothing", false)]
        [TestCase("Partly", "Done", false)]
        [TestCase("New", "Partly", false)]
        [TestCase("Started", "Finished", true)]
        public void IsCycleTimeDefinitionValid_NeedsBothBoundariesToNameOnlyStatesTheOwnerKnows(string start, string end, bool expected)
        {
            var subject = CreateSubject();
            subject.StateMappings =
            [
                new StateMapping { Name = "Nothing", States = [] },
                new StateMapping { Name = "Partly", States = ["New", "Jellybean"] },
                new StateMapping { Name = "Started", States = ["New", "Active"] },
                new StateMapping { Name = "Finished", States = ["Done", "Closed"] },
            ];

            var valid = subject.IsCycleTimeDefinitionValid(new CycleTimeDefinition { Name = "Lead", StartState = start, EndState = end });

            Assert.That(valid, Is.EqualTo(expected));
        }

        // The setter is private because only EF, loading a stored row, writes the value without choosing a source.
        private static void StoreOwnParentOverrideAsReadFromTheDatabase(WorkTrackingSystemOptionsOwner owner, int fieldId)
            => typeof(WorkTrackingSystemOptionsOwner)
                .GetProperty(nameof(WorkTrackingSystemOptionsOwner.OwnParentOverrideAdditionalFieldDefinitionId))!
                .SetValue(owner, fieldId);

        private static WorkTrackingSystemOptionsOwnerTestClass CreateSubject()
        {
            return new WorkTrackingSystemOptionsOwnerTestClass();
        }

    }

    internal class WorkTrackingSystemOptionsOwnerTestClass : WorkTrackingSystemOptionsOwner
    {
        public override List<string> WorkItemTypes { get; set; } = new List<string>();
        public override int DoneItemsCutoffDays { get; set; } = 180;
    }
}
