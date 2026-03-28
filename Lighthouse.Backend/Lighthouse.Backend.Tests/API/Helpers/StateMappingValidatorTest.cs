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
            var allStates = new List<string> { "Active", "Resolved", "Closed" };

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EmptyMappingName_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "", States = ["Active"] }
            };
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Resolved"));
            }
        }

        [Test]
        public void Validate_SourceStateInMultipleMappingsCaseInsensitive_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Group A", States = ["active"] },
                new() { Name = "Group B", States = ["Active"] }
            };
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_MappingNameCollidesWithDirectState_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Active", States = ["In Work", "Started"] }
            };
            var allStates = new List<string> { "Active", "Closed" };

            var result = StateMappingValidator.Validate(mappings, allStates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Active"));
            }
        }

        [Test]
        public void Validate_MappingNameCollidesWithDirectStateCaseInsensitive_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "active", States = ["In Work"] }
            };
            var allStates = new List<string> { "Active" };

            var result = StateMappingValidator.Validate(mappings, allStates);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_MappingWithEmptyStates_ReturnsError()
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = "Empty", States = [] }
            };
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

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
            var allStates = new List<string>();

            var result = StateMappingValidator.Validate(mappings, allStates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.GreaterThanOrEqualTo(2));
            }
        }
    }
}
