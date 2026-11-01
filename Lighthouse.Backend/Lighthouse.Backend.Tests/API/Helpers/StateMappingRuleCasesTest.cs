using System.Text.Json;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    /// <summary>
    /// The settings page and the server decide with the same words which names and states are the same and which
    /// names are blank. Both read the cases from one file in the frontend's sources, so neither can drift from the other.
    /// </summary>
    public class StateMappingRuleCasesTest
    {
        private static readonly string CasesFile = Path.Combine("..", "Lighthouse.Frontend", "src", "utils", "stateMappingRuleCases.json");

        private static JsonElement Cases() => JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), CasesFile))).RootElement;

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate the Lighthouse.sln that anchors the shared cases.");
            return directory!.FullName;
        }

        private static List<string> DuplicateNameErrorsFor(string first, string second)
        {
            var mappings = new List<StateMapping>
            {
                new() { Name = first, States = ["Code Review"] },
                new() { Name = second, States = ["Testing"] },
            };

            return StateMappingValidator.Validate(mappings).Errors.Where(error => error.StartsWith("Duplicate state mapping name", StringComparison.Ordinal)).ToList();
        }

        [Test]
        public void TwoMappingNames_AreTheSameExactlyWhenTheSharedCasesSayTheyAre()
        {
            var disagreements = Cases().GetProperty("sameNameOrNot").EnumerateArray()
                .Where(pair => (DuplicateNameErrorsFor(pair.GetProperty("a").GetString()!, pair.GetProperty("b").GetString()!).Count > 0) != pair.GetProperty("same").GetBoolean())
                .Select(pair => $"'{pair.GetProperty("a").GetString()}' and '{pair.GetProperty("b").GetString()}' are {(pair.GetProperty("same").GetBoolean() ? "the same" : "different")}")
                .ToList();

            Assert.That(disagreements, Is.Empty);
        }

        [Test]
        public void TwoMappingNames_DifferingOnlyInWhitespaceAtTheEnds_AreTheSame()
        {
            foreach (var pair in Cases().GetProperty("sameOnceTrimmed").EnumerateArray())
            {
                var a = pair.GetProperty("a").GetString()!;
                var b = pair.GetProperty("b").GetString()!;

                Assert.That(DuplicateNameErrorsFor(a, b), Is.Not.Empty, $"'{a}' and '{b}'");
            }
        }

        [Test]
        public void ANameOfNothingButWhitespace_IsBlank()
        {
            foreach (var blank in Cases().GetProperty("blank").EnumerateArray().Select(item => item.GetString()!))
            {
                var errors = StateMappingValidator.Validate([new StateMapping { Name = blank, States = ["Code Review"] }]).Errors;

                Assert.That(errors, Has.Some.EqualTo("A state mapping name is required."), $"name {JsonSerializer.Serialize(blank)}");
            }
        }
    }
}
