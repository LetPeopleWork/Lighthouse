using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// How a list of parent sources is rendered into the text that decides whether the next update downloads
    /// everything again. Order matters, because the same sources in another order name another parent. A list of
    /// the tracker's own parent alone keeps the text of "no override", so those owners are not re-read. Every other
    /// list, including the field-then-own-parent list the upgrade writes for an owner that had an override,
    /// renders in a new form, which re-reads that owner once.
    /// </summary>
    [TestFixture]
    [Ignore("Story 6501: enable with the fingerprint's ordered rendering.")]
    public class FetchFingerprintParentSourcesTest
    {
        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static string Rendered(params ParentSourceEntry[] entries)
            => FetchFingerprint.RenderParentSources(ParentSources.Of(entries).List!);

        [TestCase(new[] { 11, 0 }, "F:11,P", Description = "A field, then the own parent: the migrated old override")]
        [TestCase(new[] { 0, 11 }, "P,F:11", Description = "The same two in the other order")]
        [TestCase(new[] { 11, 12 }, "F:11,F:12", Description = "Two fields")]
        [TestCase(new[] { 12, 11 }, "F:12,F:11", Description = "Two fields the other way round")]
        [TestCase(new[] { 0 }, "-", Description = "Only the own parent: no override, and its meaning cannot have moved")]
        public void A_list_renders_each_entry_and_its_position(int[] entries, string expected)
        {
            var list = entries.Select(entry => entry == 0 ? OwnParent : ParentSourceEntry.Field(entry)).ToArray();

            Assert.That(Rendered(list), Is.EqualTo(expected));
        }

        [Test]
        public void Every_valid_list_over_three_fields_and_the_own_parent_renders_in_a_form_no_old_fingerprint_has()
        {
            var everyEntry = new[] { OwnParent, ParentSourceEntry.Field(1), ParentSourceEntry.Field(2), ParentSourceEntry.Field(3) };
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var list in EveryOrderedSelectionOf(everyEntry))
            {
                var rendered = Rendered([.. list]);
                var listAsText = string.Join(",", list.Select(entry => entry.AdditionalFieldDefinitionId?.ToString() ?? "P"));

                var isTheLegacyCollapse = list is [{ Kind: ParentSourceKind.TrackerParent }];
                if (!isTheLegacyCollapse)
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(IsInAnOldFingerprintsForm(rendered), Is.False,
                            $"{listAsText} rendered as {rendered}, which an old fingerprint could also hold, so an untouched owner could be mistaken for a changed one.");
                        Assert.That(rendered, Does.StartWith("F").Or.StartWith("P"));
                    }
                }

                Assert.That(seen.TryGetValue(rendered, out var other), Is.False,
                    $"{listAsText} and {other} render alike, so changing from one to the other would not re-read anything.");
                seen[rendered] = listAsText;
            }
        }

        private static bool IsInAnOldFingerprintsForm(string rendered)
            => rendered == "-" || (rendered.Length > 0 && rendered.All(char.IsAsciiDigit));

        private static List<IReadOnlyList<ParentSourceEntry>> EveryOrderedSelectionOf(ParentSourceEntry[] entries)
        {
            var found = new List<IReadOnlyList<ParentSourceEntry>>();
            Extend([], entries, found);
            return found;
        }

        private static void Extend(List<ParentSourceEntry> soFar, ParentSourceEntry[] entries, List<IReadOnlyList<ParentSourceEntry>> found)
        {
            foreach (var next in entries.Except(soFar))
            {
                var longer = new List<ParentSourceEntry>(soFar) { next };
                found.Add(longer);
                Extend(longer, entries, found);
            }
        }
    }
}
