using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>The generated lists really contain the shapes the property tests claim to cover.</summary>
    public class StateListCasesTest
    {
        private const char NonBreakingSpace = (char)160;

        [Test]
        public void Generate_ThreeHundredCases_HoldEveryKindOfEntryAndEveryKindOfPadding()
        {
            var cases = StateListCases.Generate(300).ToList();
            var entries = cases.SelectMany(c => c.StoredDoing).ToList();
            var kinds = cases.SelectMany(c => c.NamedEntries.Values).Distinct().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kinds, Is.EquivalentTo(new[] { ListEntryKind.StateOfItsOwnMapping, ListEntryKind.CollapsedGroup, ListEntryKind.Shadowing }));
                Assert.That(entries, Has.Some.Matches<string>(entry => entry.StartsWith(' ') && !entry.EndsWith(' ')), "a leading space only");
                Assert.That(entries, Has.Some.Matches<string>(entry => entry.StartsWith('\t')), "a tab");
                Assert.That(entries, Has.Some.Matches<string>(entry => entry.StartsWith(NonBreakingSpace) && entry.EndsWith(NonBreakingSpace)), "non-breaking spaces");
                Assert.That(entries, Has.Some.Matches<string>(entry => entry is { Length: > 3 } && entry.Any(char.IsUpper) && !entry.Any(char.IsLower)), "an upper-case variant");
                Assert.That(cases.Select(c => c.Index), Is.Unique);
            }
        }

        [Test]
        public void Generate_Twice_GivesTheSameCases()
        {
            var first = StateListCases.Generate(50).Select(c => c.ToString()).ToList();
            var second = StateListCases.Generate(50).Select(c => c.ToString()).ToList();

            Assert.That(second, Is.EqualTo(first));
        }
    }
}
