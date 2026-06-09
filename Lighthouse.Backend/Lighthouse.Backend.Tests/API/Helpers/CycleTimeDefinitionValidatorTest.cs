using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    public class CycleTimeDefinitionValidatorTest
    {
        private static SettingsOwnerDtoBase SettingsWith(params CycleTimeDefinitionDto[] definitions)
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
                "D5: a boundary state absent from the workflow is a read-time validity concern (IsValid:false on projection), not a save rejection.");
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
    }
}
