using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// How an entry crosses the serialiser the stored text uses: default options, no indentation, kinds as
    /// numbers. The data step writes this text in SQL, so what the serialiser writes and reads has to be exactly
    /// it, and an entry the rules refuse must not be built by the serialiser either.
    /// </summary>
    [TestFixture]
    [Ignore("Story 6501: enable with the entry's validating constructor.")]
    public class ParentSourceEntrySerialiserTest
    {
        private const string TheOwnParentAfterField12 =
            """[{"Kind":1,"AdditionalFieldDefinitionId":12},{"Kind":0,"AdditionalFieldDefinitionId":null}]""";

        [Test]
        public void Entries_are_written_and_read_back_with_the_default_options_exactly_as_the_upgrade_writes_them()
        {
            var entries = new List<ParentSourceEntry> { ParentSourceEntry.Field(12), ParentSourceEntry.TrackerParent };

            var written = JsonSerializer.Serialize(entries, (JsonSerializerOptions?)null);
            var read = JsonSerializer.Deserialize<List<ParentSourceEntry>>(TheOwnParentAfterField12, (JsonSerializerOptions?)null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Is.EqualTo(TheOwnParentAfterField12));
                Assert.That(read, Is.EqualTo(entries).AsCollection, "The entry has to be built through its own constructor, by the names it was written under.");
            }
        }

        [TestCase("""[{"Kind":0,"AdditionalFieldDefinitionId":3}]""", Description = "The own parent carrying a field id")]
        [TestCase("""[{"Kind":1,"AdditionalFieldDefinitionId":null}]""", Description = "A field naming no field")]
        [TestCase("""[{"Kind":7,"AdditionalFieldDefinitionId":null}]""", Description = "A kind this version does not know")]
        public void An_entry_the_rules_refuse_is_never_built_by_the_serialiser(string text)
        {
            var thrown = Assert.Catch(() => JsonSerializer.Deserialize<List<ParentSourceEntry>>(text, (JsonSerializerOptions?)null));

            Assert.That(thrown, Is.Not.Null.And.Not.InstanceOf<NotImplementedException>());
        }
    }
}
