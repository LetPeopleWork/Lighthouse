using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class StateMappingValidatorRawListsTest
    {
        // The copy the settings page already shows for a name that shadows a state.
        private const string InArbeitShadowsADoingState =
            "Mapping \"In Arbeit\" has the same name as the Doing state \"In Arbeit\". Add \"In Arbeit\" to this mapping to group it, or choose another name.";

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
        public void NameBesideOneOfItsGroupsStatesInDoing_IsRefusedWithTheSentenceTheSettingsPageShows()
        {
            var settings = Settings(["Backlog"], ["In Arbeit", "In Progress"], ["Done"], Mapping("In Arbeit", "In Progress"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Has.Some.EqualTo(InArbeitShadowsADoingState));
        }

        [Test]
        public void NameBesideOneOfItsGroupsStatesInToDo_IsRefusedNamingTheToDoList()
        {
            var settings = Settings(["In Arbeit", "In Progress"], ["Active"], ["Done"], Mapping("In Arbeit", "In Progress"));

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Mapping \"In Arbeit\" has the same name as the To Do state \"In Arbeit\". Choose another name."));
        }

        [Test]
        public void NameBesideOneOfItsGroupsStatesInDone_IsRefusedNamingTheDoneList()
        {
            var settings = Settings(["Backlog"], ["Active"], ["In Arbeit", "In Progress"], Mapping("In Arbeit", "In Progress"));

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Mapping \"In Arbeit\" has the same name as the Done state \"In Arbeit\". Choose another name."));
        }

        [Test]
        public void NameStandingAloneInPlaceOfItsGroupsStates_IsNotRefused()
        {
            var settings = Settings(["Backlog"], ["Analysis", "In Arbeit"], ["Done"], Mapping("In Arbeit", "In Progress"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).IsValid, Is.True);
        }

        [Test]
        public void EntryNamedLikeAGroupAndAlsoOneOfItsStates_IsNotShadowing()
        {
            var settings = Settings(["Backlog"], ["Active", "Resolved"], ["Done"], Mapping("Active", "Active", "Resolved"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).IsValid, Is.True);
        }

        [Test]
        public void GroupThatListsAnotherGroupsName_IsRefusedWithTheChainSentence()
        {
            var settings = Settings(["Backlog"], ["X"], ["Done"], Mapping("A", "X"), Mapping("B", "A"));

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Mapping \"A\" has the same name as a state grouped by mapping \"B\". Choose another name."));
        }

        [Test]
        public void GroupWithAStateListedNowhere_IsValid()
        {
            var settings = Settings(["Backlog"], ["Code Review"], ["Done"], Mapping("Review", "Code Review", "Testing"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        [Test]
        public void GroupWhoseListedStatesSitInDifferentCategories_IsRefusedNamingTheTwoStates()
        {
            var settings = Settings(["Code Review"], ["Testing"], ["Done"], Mapping("Review", "Code Review", "Testing"));

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Mapping \"Review\" groups states from different categories: \"Code Review\" is in To Do and \"Testing\" is in Doing. Keep a mapping's states in one category."));
        }

        [Test]
        public void GroupWhoseOnlyStateIsListedInTwoCategories_IsRefused()
        {
            var settings = Settings(["Testing"], ["Active"], ["Testing"], Mapping("Review", "Testing"));

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Mapping \"Review\" groups \"Testing\", which is in both To Do and Done. Keep a mapping's states in one category."));
        }

        [Test]
        public void TwoGroupsOverTheSameState_AreRefusedWithoutBlamingAListEntry()
        {
            var settings = Settings(["Backlog"], ["Y", "X"], ["Done"], Mapping("A", "X"), Mapping("B", "X"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Has.Some.Contains("appears in multiple mappings"));
        }

        [Test]
        public void GroupWithABlankName_IsRefusedWhateverTheListsHold()
        {
            var settings = Settings(["Backlog"], ["Code Review"], ["Done"], Mapping("  ", "Code Review"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Has.Some.EqualTo("A state mapping name is required."));
        }

        [Test]
        public void GroupWithNoStates_IsRefusedWhateverTheListsHold()
        {
            var settings = Settings(["Backlog"], ["Code Review"], ["Done"], Mapping("Review"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Has.Some.EqualTo("State mapping 'Review' must contain at least one state."));
        }

        [Test]
        public void TwoGroupsWithTheSameName_AreRefusedOnce()
        {
            var settings = Settings(["Backlog"], ["Code Review", "Testing"], ["Done"], Mapping("Review", "Code Review"), Mapping("REVIEW", "Testing"));

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Has.Some.EqualTo("Duplicate state mapping name 'Review'."));
        }

        [Test]
        public void ListsWithNullAndBlankEntries_DoNotMakeTheValidatorThrow()
        {
            var settings = Settings(["Backlog", ""], ["  ", "Code Review"], ["Done", "\t"], Mapping("Review", "Code Review"));
            settings.DoingStates.Add(null!);

            Assert.That(() => StateMappingValidator.ValidateSettings(settings), Throws.Nothing);
        }
    }
}
