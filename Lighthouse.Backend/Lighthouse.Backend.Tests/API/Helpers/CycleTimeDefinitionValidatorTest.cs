using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    public class CycleTimeDefinitionValidatorTest
    {
        private static TeamSettingDto SettingsWith(params CycleTimeDefinitionDto[] definitions)
        {
            return new TeamSettingDto
            {
                Name = "Team",
                ToDoStates = ["Backlog"],
                DoingStates = ["Implementation", "Review"],
                DoneStates = ["Done"],
                StateMappings = [],
                CycleTimeDefinitions = [.. definitions],
            };
        }

        private static CycleTimeDefinitionDto Definition(string name, string startState, string endState)
        {
            return new CycleTimeDefinitionDto { Name = name, StartState = startState, EndState = endState };
        }

        [Test]
        public void Validate_EndStateAfterStartState_IsAccepted()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Lead Time", "Implementation", "Done")));

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
        }

        [Test]
        public void Validate_EndStateBeforeStartState_IsRejected()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Reversed", "Done", "Implementation")));

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError));
        }

        [Test]
        public void Validate_EndStateEqualsStartState_IsRejected()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Zero Span", "Review", "Review")));

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError),
                "A definition whose end boundary resolves to the same workflow position as its start has a zero-width window and must be rejected.");
        }

        [Test]
        public void Validate_MissingBoundaryState_IsNotRejectedAtSave_PresenceIsReadTime()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Phantom", "Phantom", "Done")));

            Assert.That(result.IsValid, Is.True,
                "A boundary state missing from the workflow is shown as invalid when the definition is read, not refused at save.");
        }

        [Test]
        public void Validate_EndStateMissingFromTheWorkflow_IsNotRejectedAtSave()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Phantom End", "Review", "Phantom")));

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
        }

        [Test]
        public void Validate_StartStateMissingFromTheWorkflow_WithTheFirstStateAsEnd_IsNotRejectedAtSave()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Phantom Start", "Phantom", "Backlog")));

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
        }

        [Test]
        public void Validate_BlankStartState_IsNotRejectedAtSave()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("No Start", null!, "Done")));

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
        }

        [Test]
        public void Validate_StartAndEndOnTheFirstStateOfTheWorkflow_IsRejected()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("Zero Span At The Start", "Backlog", "Backlog")));

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError));
        }

        [Test]
        public void Validate_BlankName_IsRejected()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(Definition("  ", "Implementation", "Done")));

            Assert.That(result.Errors, Does.Contain("A cycle time name is required."));
        }

        [Test]
        public void Validate_DuplicateNames_AreRejected()
        {
            var result = CycleTimeDefinitionValidator.ValidateSettings(
                SettingsWith(
                    Definition("Lead Time", "Implementation", "Done"),
                    Definition("lead time", "Backlog", "Done")));

            Assert.That(result.Errors, Has.Some.Contains("Duplicate cycle time name"));
        }

        private static TeamSettingDto SettingsWithReviewGroup(CycleTimeDefinitionDto definition)
        {
            return new TeamSettingDto
            {
                Name = "Team",
                ToDoStates = ["Backlog"],
                DoingStates = ["Code Review", "Testing"],
                DoneStates = ["Done"],
                StateMappings = [new StateMappingDto { Name = "Review", States = ["Code Review", "Testing"] }],
                CycleTimeDefinitions = [definition],
            };
        }

        [Test]
        public void Validate_StoredDefinitionInsideOneGroup_SentBackUnchanged_IsAccepted()
        {
            var stored = new CycleTimeDefinition { Id = 7, Name = "Review Time", StartState = "Code Review", EndState = "Testing" };
            var sent = new CycleTimeDefinitionDto { Id = 7, Name = "Review Time", StartState = " code review ", EndState = "TESTING" };

            var result = CycleTimeDefinitionValidator.ValidateSettings(SettingsWithReviewGroup(sent), [stored]);

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
        }

        [Test]
        public void Validate_NewDefinitionInsideOneGroup_IsRejected()
        {
            var stored = new CycleTimeDefinition { Id = 7, Name = "Review Time", StartState = "Code Review", EndState = "Done" };
            var sent = new CycleTimeDefinitionDto { Id = 0, Name = "Review Time", StartState = "Code Review", EndState = "Testing" };

            var result = CycleTimeDefinitionValidator.ValidateSettings(SettingsWithReviewGroup(sent), [stored]);

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError));
        }

        // Only the definition that was stored is let through; a new one with the same boundaries is judged like any other.
        [Test]
        public void Validate_NewDefinitionCopyingAStoredOneInsideOneGroup_IsRejected()
        {
            var stored = new CycleTimeDefinition { Id = 7, Name = "Review Time", StartState = "Code Review", EndState = "Testing" };
            var sent = new CycleTimeDefinitionDto { Id = 0, Name = "Review Time", StartState = "Code Review", EndState = "Testing" };

            var result = CycleTimeDefinitionValidator.ValidateSettings(SettingsWithReviewGroup(sent), [stored]);

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError));
        }

        [Test]
        public void Validate_StoredDefinitionWhoseStartStateThisSaveMovesInsideOneGroup_IsRejected()
        {
            var stored = new CycleTimeDefinition { Id = 7, Name = "Review Time", StartState = "Backlog", EndState = "Testing" };
            var sent = new CycleTimeDefinitionDto { Id = 7, Name = "Review Time", StartState = "Code Review", EndState = "Testing" };

            var result = CycleTimeDefinitionValidator.ValidateSettings(SettingsWithReviewGroup(sent), [stored]);

            Assert.That(result.Errors, Does.Contain(CycleTimeDefinitionValidator.EndStateMustComeAfterStartStateError));
        }
    }
}
