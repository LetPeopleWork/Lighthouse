using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The page and the product have to say the same thing.
    ///
    /// The consent dialog deliberately carries no list of fields: a list inside a dialog goes stale
    /// silently while still looking authoritative. It links to the usage data page instead, which
    /// makes that page the only place the list exists - so the page being wrong is the product's
    /// privacy disclosure being wrong, not a documentation chore.
    ///
    /// The page was rewritten ahead of the code on purpose, because it is far easier to make code
    /// match a written promise than to write the promise afterwards. That is a reason to check it,
    /// not a reason to skip the check.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataDisclosureTest
    {
        private const string UsageDataPage = "docs/settings/usagedata.md";
        private const string EventListHeading = "The complete list of events:";
        private const string SendsNothingYet = "Lighthouse sends nothing yet";
        private const string EveryEventCarriesHeading = "Every event carries these";
        private const string SourceRowName = "Source";

        /// <summary>
        /// The page still tells the reader that this release sends nothing. That sentence is true
        /// today and becomes false the moment the first event leaves, so removing it is part of
        /// shipping rather than something to tidy up afterwards. A product that sends data while its
        /// own disclosure says it does not has failed the one promise this feature makes, on the
        /// very first event.
        /// </summary>
        [Test]
        public void ThePageThatSaysNothingIsSentYet_DoesNotSurviveTheReleaseThatSends()
        {
            var page = TheUsageDataPage();

            Assert.That(page, Does.Not.Contain(SendsNothingYet),
                "the page the dialog links to as the full account still says this release sends "
                + "nothing. It is the only place the list of what is sent exists, so while it says "
                + "that, the product is sending data it has told people it does not send");
        }

        /// <summary>
        /// An event that ships ahead of the line describing it means data leaves that nobody was
        /// told about. The list in the code is closed for exactly this reason: there is one
        /// declaration to count against.
        /// </summary>
        [Test]
        public void EveryEventTheProductCanSend_HasALineOnThePage()
        {
            var declared = EveryEventTheProductDeclares();
            var described = EveryEventThePageDescribes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(declared, Is.Not.Empty,
                    "nothing declares an event, so counting the page's rows against it compares a "
                    + "list to nothing and holds whatever the page says");
                Assert.That(described, Has.Count.EqualTo(declared.Count),
                    "the page and the code disagree about how many things this product sends. "
                    + $"Declared: {string.Join(", ", declared)}. Described on the page: {described.Count} row(s)");
            }
        }

        /// <summary>
        /// Every event now says which surface it came from, so the page has to name every surface the
        /// product can label. Reading the members off the enum means a fourth surface cannot ship
        /// without the page saying so first.
        /// </summary>
        [Test]
        public void EverySourceAnEventCanCarry_IsNamedOnThePage()
        {
            var sourceRow = TheRowOfWhatEveryEventCarries(SourceRowName);

            Assert.That(sourceRow, Is.Not.Null,
                $"'{EveryEventCarriesHeading}' has no '{SourceRowName}' row, so the page does not say that "
                + "every event names the surface it came from");

            using (Assert.EnterMultipleScope())
            {
                foreach (var source in Enum.GetNames<UsageDataSource>())
                {
                    Assert.That(sourceRow, Does.Contain($"`{source}`"),
                        $"the product can label an event '{source}', and the page does not say so");
                }
            }
        }

        private static List<string> EveryEventTheProductDeclares()
        {
            var vocabulary = typeof(Backend.Program).Assembly
                .GetTypes()
                .FirstOrDefault(type => type.IsEnum
                    && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));

            return vocabulary is null ? [] : [.. Enum.GetNames(vocabulary).OrderBy(name => name, StringComparer.Ordinal)];
        }

        /// <summary>
        /// The rows under the page's own heading for the list, so a table elsewhere on the page
        /// cannot be mistaken for this one.
        /// </summary>
        private static List<string> EveryEventThePageDescribes()
        {
            var lines = TheUsageDataPage().Split('\n');
            var heading = Array.FindIndex(lines, line => line.Contains(EventListHeading, StringComparison.Ordinal));

            Assert.That(heading, Is.GreaterThanOrEqualTo(0),
                $"'{EventListHeading}' is no longer on the page, so this test is reading a table it "
                + "was not written about - or none at all.");

            var rows = new List<string>();
            for (var line = heading + 1; line < lines.Length; line++)
            {
                var text = lines[line].Trim();
                if (text.StartsWith('|'))
                {
                    rows.Add(text);
                }
                else if (rows.Count > 0)
                {
                    break;
                }
            }

            // The header row and the dashes under it describe the table rather than an event.
            return [.. rows.Skip(2)];
        }

        /// <summary>
        /// The row of the page's "every event carries these" table whose first cell is
        /// <paramref name="rowName"/>, or null when there is none.
        /// </summary>
        private static string? TheRowOfWhatEveryEventCarries(string rowName)
        {
            var lines = TheUsageDataPage().Split('\n');
            var heading = Array.FindIndex(lines, line => line.Contains(EveryEventCarriesHeading, StringComparison.Ordinal));

            Assert.That(heading, Is.GreaterThanOrEqualTo(0),
                $"'{EveryEventCarriesHeading}' is no longer on the page, so there is no table to read.");

            return lines
                .Skip(heading + 1)
                .Select(line => line.Trim())
                .SkipWhile(line => !line.StartsWith('|'))
                .TakeWhile(line => line.StartsWith('|'))
                .FirstOrDefault(line => line.Trim('|').Split('|')[0].Trim().Equals(rowName, StringComparison.Ordinal));
        }

        private static string TheUsageDataPage()
        {
            var page = Path.Combine(
                RepositoryRoot(), UsageDataPage.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(page), Is.True,
                $"{UsageDataPage} was moved or deleted. The consent dialog links to it as the "
                + "authoritative account of what is sent, so without it the product's only "
                + "disclosure is a dead link.");

            return File.ReadAllText(page);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the docs read.");

            return Directory.GetParent(directory!.FullName)!.FullName;
        }
    }
}
