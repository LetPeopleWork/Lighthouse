using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class StateMappingValidatorRawListsTest
    {
        private const string Pending = "pending: category lists hold raw states";

        private static TeamSettingDto Settings(string[] toDo, string[] doing, string[] done, params StateMappingDto[] mappings)
            => new()
            {
                ToDoStates = [.. toDo],
                DoingStates = [.. doing],
                DoneStates = [.. done],
                StateMappings = [.. mappings],
            };

        private static StateMappingDto Mapping(string name, params string[] states) => new() { Name = name, States = [.. states] };

        [Test]
        [Ignore(Pending)]
        public void EntryNamedLikeAGroupThatIsNotOneOfItsStates_IsRefusedNamingTheFix()
        {
            var settings = Settings(["Backlog"], ["In Arbeit", "In Progress"], ["Done"], Mapping("In Arbeit", "In Progress"));

            var result = StateMappingValidator.ValidateSettings(settings);

            Assert.That(result.Errors, Has.Some.Contains("In Arbeit").And.Contains("Rename the mapping"));
        }

        [Test]
        [Ignore(Pending)]
        public void EntryNamedLikeAGroupThatIsNotOneOfItsStates_IsRefusedWhicheverListHoldsIt()
        {
            var inToDo = Settings(["In Arbeit"], ["Active"], ["Done"], Mapping("In Arbeit", "In Progress"));
            var inDone = Settings(["Backlog"], ["Active"], ["In Arbeit"], Mapping("In Arbeit", "In Progress"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateMappingValidator.ValidateSettings(inToDo).IsValid, Is.False);
                Assert.That(StateMappingValidator.ValidateSettings(inDone).IsValid, Is.False);
            }
        }

        [Test]
        public void EntryNamedLikeAGroupAndAlsoOneOfItsStates_IsNotShadowing()
        {
            var settings = Settings(["Backlog"], ["Active", "Resolved"], ["Done"], Mapping("Active", "Active", "Resolved"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).IsValid, Is.True);
        }

        [Test]
        [Ignore(Pending)]
        public void GroupThatListsAnotherGroupsName_IsRefusedNamingBothGroups()
        {
            var settings = Settings(["Backlog"], ["X"], ["Done"], Mapping("A", "X"), Mapping("B", "A"));

            var result = StateMappingValidator.ValidateSettings(settings);

            Assert.That(result.Errors, Has.Some.Contains("A").And.Contains("B").And.Contains("another mapping"));
        }

        [Test]
        public void GroupWithAStateListedNowhere_IsValid()
        {
            var settings = Settings(["Backlog"], ["Code Review"], ["Done"], Mapping("Review", "Code Review", "Testing"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        [Test]
        [Ignore(Pending)]
        public void GroupWhoseListedStatesSitInDifferentCategories_IsRefused()
        {
            var settings = Settings(["Code Review"], ["Testing"], ["Done"], Mapping("Review", "Code Review", "Testing"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).IsValid, Is.False);
        }

        [Test]
        public void TwoGroupsOverTheSameState_AreRefusedWithoutBlamingAListEntry()
        {
            var settings = Settings(["Backlog"], ["Y", "X"], ["Done"], Mapping("A", "X"), Mapping("B", "X"));

            var result = StateMappingValidator.ValidateSettings(settings);

            Assert.That(result.Errors, Has.Some.Contains("appears in multiple mappings"));
        }
    }
}
