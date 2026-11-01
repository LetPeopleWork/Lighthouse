using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class StateMappingValidatorTest
    {
        [Test]
        public void Validate_EmptyMappings_IsValid()
        {
            var mappings = new List<StateMapping>();

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            }
        }

        [Test]
        public void Validate_SingleValidMapping_IsValid()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "In Progress", States = ["Active", "Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EmptyMappingName_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "", States = ["Active"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("name"));
            }
        }

        [Test]
        public void Validate_WhitespaceMappingName_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "   ", States = ["Active"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_DuplicateMappingNames_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "In Progress", States = ["Active"] },
                new() { Name = "In Progress", States = ["Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("In Progress"));
            }
        }

        [Test]
        public void Validate_DuplicateMappingNamesCaseInsensitive_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "In Progress", States = ["Active"] },
                new() { Name = "in progress", States = ["Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_SourceStateInMultipleMappings_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Group A", States = ["Active", "Resolved"] },
                new() { Name = "Group B", States = ["Resolved", "Closed"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Resolved"));
            }
        }

        [Test]
        public void Validate_SourceStateInMultipleMappings_NamesTheStateAndEachMappingOnce()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Group A", States = ["Resolved", "Resolved"] },
                new() { Name = "Group B", States = ["Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(
                result.Errors,
                Is.EqualTo(new List<string> { "Source state 'Resolved' appears in multiple mappings: 'Group A', 'Group B'." }));
        }

        [Test]
        public void ValidateSettings_SourceStateInMultipleMappings_ReportsTheClashOfTheSettingsMappings()
        {
            var settings = new TeamSettingDto
            {
                StateMappings =
                [
                    new StateMappingDto { Name = "Group A", States = ["Active"] },
                    new StateMappingDto { Name = "Group B", States = ["Active"] }
                ]
            };

            var result = StateMappingValidator.ValidateSettings(settings);

            Assert.That(
                result.Errors,
                Is.EqualTo(new List<string> { "Source state 'Active' appears in multiple mappings: 'Group A', 'Group B'." }));
        }

        [Test]
        public void Validate_SourceStateInMultipleMappingsCaseInsensitive_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Group A", States = ["active"] },
                new() { Name = "Group B", States = ["Active"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_MappingNameDifferentFromAllItsStates_IsValid()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Implementation", States = ["Active", "Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MappingNameEqualToOwnSourceStateIgnoringCase_IsValid()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Implementation", States = ["implementation", "Resolved"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MappingNamedLikeAStateOutsideItsSourceStates_IsValid()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Active", States = ["In Work", "Started"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MappingWithEmptyStates_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Empty", States = [] }
            };

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Empty"));
            }
        }

        [Test]
        public void Validate_MultipleErrors_ReturnsAll()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "", States = ["Active"] },
                new() { Name = "Group A", States = ["Resolved"] },
                new() { Name = "Group A", States = ["Closed"] }
            };

            var result = StateMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.GreaterThanOrEqualTo(2));
            }
        }
    }
}
