using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    /// <summary>
    /// A wait state is compared with an item's history, which records a mapping's name and never the states inside it.
    /// A wait state naming one state of a mapping would silently turn all the time in the mapping into wait, so the
    /// save is refused and asks for the mapping's name instead.
    /// </summary>
    public class StateMappingValidatorWaitStatesTest
    {
        private const string Pending = "pending: a wait state inside a mapping is refused";

        private const string TestingIsPartOfReview =
            "Wait state \"Testing\" is part of the mapping \"Review\". Use \"Review\" as the wait state instead.";

        private static string[] Listed(params string[] errors) => errors;

        private static StateMappingDto Review() => new() { Name = "Review", States = ["Code Review", "Testing"] };

        private static TOwner Settings<TOwner>(string[] waitStates, params StateMappingDto[] mappings)
            where TOwner : SettingsOwnerDtoBase, new()
            => new()
            {
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysis", "Code Review", "Testing", "Active", "Resolved"],
                DoneStates = ["Done"],
                StateMappings = [.. mappings],
                WaitStates = [.. waitStates],
            };

        private static TeamSettingDto TeamSettings(string[] waitStates, params StateMappingDto[] mappings)
            => Settings<TeamSettingDto>(waitStates, mappings);

        private static Team StoredTeam(string[] waitStates, params StateMapping[] mappings)
            => new()
            {
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysis", "Code Review", "Testing", "Active", "Resolved"],
                DoneStates = ["Done"],
                StateMappings = [.. mappings],
                WaitStates = [.. waitStates],
            };

        [Test]
        [Ignore(Pending)]
        public void TeamWaitStateNamingOneStateInsideAMapping_IsRefusedAskingForTheMappingsName()
        {
            var settings = TeamSettings(["Testing"], Review());

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.EqualTo(Listed(TestingIsPartOfReview)));
        }

        [Test]
        [Ignore(Pending)]
        public void PortfolioWaitStateNamingOneStateInsideAMapping_IsRefusedAskingForTheMappingsName()
        {
            var settings = Settings<PortfolioSettingDto>(["Testing"], Review());

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.EqualTo(Listed(TestingIsPartOfReview)));
        }

        [Test]
        [Ignore(Pending)]
        public void WaitStateWrittenInAnotherCaseWithPadding_IsRefusedNamingItTrimmed()
        {
            var settings = TeamSettings([" testing "], new StateMappingDto { Name = " Review ", States = ["Code Review", "Testing"] });

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Has.Some.EqualTo("Wait state \"testing\" is part of the mapping \"Review\". Use \"Review\" as the wait state instead."));
        }

        [Test]
        [Ignore(Pending)]
        public void WaitStateNamingTheOtherStateOfAMappingNamedAfterOneOfItsStates_IsRefused()
        {
            var settings = TeamSettings(["Resolved"], new StateMappingDto { Name = "Active", States = ["Active", "Resolved"] });

            Assert.That(
                StateMappingValidator.ValidateSettings(settings).Errors,
                Is.EqualTo(Listed("Wait state \"Resolved\" is part of the mapping \"Active\". Use \"Active\" as the wait state instead.")));
        }

        [Test]
        public void WaitStateNamingAMappingNamedAfterOneOfItsStates_IsTheMappingsNameAndNotRefused()
        {
            var settings = TeamSettings(["active"], new StateMappingDto { Name = "Active", States = ["Active", "Resolved"] });

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        [Test]
        public void WaitStateNamingTheMapping_IsNotRefused()
        {
            var settings = TeamSettings(["Review"], Review());

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        [Test]
        public void WaitStateOutsideEveryMapping_IsNotRefused()
        {
            var settings = TeamSettings(["Analysis"], Review());

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        [Test]
        public void BlankWaitStates_AreNeitherRefusedNorMakeTheValidatorThrow()
        {
            var settings = TeamSettings(["", "  "], Review());
            settings.WaitStates.Add(null!);

            Assert.That(StateMappingValidator.ValidateSettings(settings).Errors, Is.Empty);
        }

        // Settings stored before this rule may already hold such a wait state. Refusing it when it comes back unchanged
        // would block every unrelated save, so only a wait state or a mapping this save adds or changes is judged.
        [Test]
        public void StoredWaitStateInsideAnUnchangedMapping_SentBackUnchanged_IsNotRefused()
        {
            var stored = StoredTeam(["Testing"], new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(StateMappingValidator.ValidateSettings(TeamSettings(["Testing"], Review()), stored).Errors, Is.Empty);
        }

        [Test]
        [Ignore(Pending)]
        public void NewWaitStateInsideAStoredMapping_IsRefused()
        {
            var stored = StoredTeam([], new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            Assert.That(StateMappingValidator.ValidateSettings(TeamSettings(["Testing"], Review()), stored).Errors, Is.EqualTo(Listed(TestingIsPartOfReview)));
        }

        [Test]
        [Ignore(Pending)]
        public void StoredWaitStateThatThisSaveGroupsIntoAMapping_IsRefused()
        {
            var stored = StoredTeam(["Testing"], new StateMapping { Name = "Review", States = ["Code Review"] });

            Assert.That(StateMappingValidator.ValidateSettings(TeamSettings(["Testing"], Review()), stored).Errors, Is.EqualTo(Listed(TestingIsPartOfReview)));
        }
    }
}
