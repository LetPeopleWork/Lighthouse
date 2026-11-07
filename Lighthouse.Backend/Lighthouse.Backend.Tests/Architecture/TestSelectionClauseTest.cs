using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// How the test selection strings read, taken from the files that hold them. A clause that leaves tests out fails
    /// nothing, because dotnet test stays green on whatever the filter still selects. How NUnit reads the same strings
    /// was checked by hand, so this pins the logic and the text, not the test adapter.
    /// </summary>
    [TestFixture]
    public partial class TestSelectionClauseTest
    {
        private const string CiWorkflow = ".github/workflows/ci_backend.yml";

        private const string AlwaysOnClause = "Category!=Integration|(Category!=JiraIntegration&Category!=AdoIntegration&Category!=LinearIntegration&Category!=ServiceNowIntegration&Category!=GithubIntegration)";

        private const string GuardedIntegrationClause = "(Category=Integration&Category!=UsageDataCanary)";

        [GeneratedRegex(@"parts\s*=\s*@?\(\s*[""']([^""']+)[""']\s*\)", RegexOptions.CultureInvariant)]
        private static partial Regex FirstPartOfTheFilter();

        [GeneratedRegex(@"\(Category=Integration[^""')]*\)", RegexOptions.CultureInvariant)]
        private static partial Regex ClauseThatAddsIntegrationTests();

        [GeneratedRegex(@"[""']Category=Integration[""']", RegexOptions.CultureInvariant)]
        private static partial Regex BareIntegrationTerm();

        [Test]
        public void TheAlwaysOnClauseKeepsRegularAndHostedTests()
        {
            var clause = AlwaysOnClauseInCi();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Clause.Selects(clause), Is.True, "A regular test has no category.");
                Assert.That(Clause.Selects(clause, "Integration"), Is.True, "A hosted test carries Integration alone.");
            }
        }

        [TestCase("JiraIntegration")]
        [TestCase("AdoIntegration")]
        [TestCase("LinearIntegration")]
        [TestCase("ServiceNowIntegration")]
        [TestCase("GithubIntegration")]
        public void TheAlwaysOnClauseLeavesOutALiveTest(string connector)
        {
            Assert.That(Clause.Selects(AlwaysOnClauseInCi(), "Integration", connector), Is.False);
        }

        [TestCase("JiraIntegration")]
        [TestCase("AdoIntegration")]
        public void TheAlwaysOnClauseKeepsAnOfflineTestOfAMixedClass(string connector)
        {
            Assert.That(Clause.Selects(AlwaysOnClauseInCi(), connector), Is.True);
        }

        [Test]
        public void TheFiveConnectorsAloneWouldDropAnOfflineTestOfAMixedClass()
        {
            const string WrongClause = "Category!=JiraIntegration&Category!=AdoIntegration&Category!=LinearIntegration&Category!=ServiceNowIntegration&Category!=GithubIntegration";

            Assert.That(Clause.Selects(WrongClause, "JiraIntegration"), Is.False);
        }

        [Test]
        public void TheOldClauseWouldDropAHostedTest()
        {
            Assert.That(Clause.Selects("Category!=Integration", "Integration"), Is.False);
        }

        [Test]
        public void AForceFullRunTakesEverythingButTheCanary()
        {
            var alwaysOn = AlwaysOnClauseInCi();
            var guarded = GuardedIntegrationClauseInCi();
            var forceFull = $"{alwaysOn}|{guarded}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Clause.Selects(forceFull), Is.True, "A regular test.");
                Assert.That(Clause.Selects(forceFull, "Integration"), Is.True, "A hosted test.");
                Assert.That(Clause.Selects(forceFull, "JiraIntegration"), Is.True, "An offline test of a mixed class.");
                Assert.That(Clause.Selects(forceFull, "Integration", "JiraIntegration"), Is.True, "A live test.");
                Assert.That(Clause.Selects(forceFull, "Integration", "UsageDataCanary", "JiraIntegration"), Is.False, "The canary, with its connector.");
                Assert.That(Clause.Selects(guarded, "Integration", "UsageDataCanary"), Is.False, "The canary, by the second half.");
                Assert.That(Clause.Selects(alwaysOn, "Integration", "UsageDataCanary"), Is.True, "Without a connector the first half keeps the canary, so the canary has to carry one.");
            }
        }

        [TestCase("JiraIntegration")]
        [TestCase("AdoIntegration")]
        [TestCase("LinearIntegration")]
        [TestCase("ServiceNowIntegration")]
        [TestCase("GithubIntegration")]
        public void AConnectorsClauseJoinedOnTakesThatConnectorsLiveTestsAlone(string connector)
        {
            string[] allConnectors = ["JiraIntegration", "AdoIntegration", "LinearIntegration", "ServiceNowIntegration", "GithubIntegration"];
            var workflow = ReadFromRepository(CiWorkflow);
            var term = $"Category={connector}";
            var joined = $"{AlwaysOnClauseInCi()}|{term}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workflow, Does.Contain($"parts+=(\"{term}\")"));
                Assert.That(Clause.Selects(joined, "Integration", connector), Is.True);
                Assert.That(allConnectors.Where(other => other != connector && Clause.Selects(joined, "Integration", other)), Is.Empty);
            }
        }

        [TestCase(CiWorkflow)]
        [TestCase("Scripts/test-selection/dev-test.sh")]
        [TestCase("Scripts/test-selection/dev-test.ps1")]
        [TestCase("Scripts/test-selection/test-build-filter.sh")]
        public void EveryPlaceHoldsTheSameText(string place)
        {
            var text = ReadFromRepository(place);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(AlwaysOnClauseIn(text), Is.EqualTo(AlwaysOnClause));
                Assert.That(ClauseThatAddsIntegrationTests().Match(text).Value, Is.EqualTo(GuardedIntegrationClause));
                Assert.That(BareIntegrationTerm().IsMatch(text), Is.False, "An unguarded Integration term selects the Explicit probes too.");
            }
        }

        private static string AlwaysOnClauseInCi()
        {
            return AlwaysOnClauseIn(ReadFromRepository(CiWorkflow));
        }

        private static string AlwaysOnClauseIn(string text)
        {
            return FirstPartOfTheFilter().Match(text).Groups[1].Value;
        }

        private static string GuardedIntegrationClauseInCi()
        {
            return ClauseThatAddsIntegrationTests().Match(ReadFromRepository(CiWorkflow)).Value;
        }

        private static string ReadFromRepository(string place)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), place.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate the repository root above Lighthouse.Backend/Lighthouse.sln.");
            return directory!.FullName;
        }

        /// <summary>
        /// Reads =, !=, &amp;, | and parentheses over the categories of one test, with &amp; binding tighter than |.
        /// </summary>
        private sealed partial class Clause
        {
            private const string Excludes = "Category!=";
            private const string Includes = "Category=";

            private readonly string[] tokens;
            private readonly HashSet<string> categories;
            private int position;

            private Clause(string text, string[] categories)
            {
                tokens = Token().Matches(text).Select(match => match.Value).ToArray();
                this.categories = [.. categories];

                if (string.Concat(tokens) != text)
                {
                    throw new FormatException($"Holds something other than category terms: {text}");
                }
            }

            public static bool Selects(string text, params string[] categories)
            {
                var clause = new Clause(text, categories);
                var selected = clause.ReadAnyOf();

                if (clause.position != clause.tokens.Length)
                {
                    throw new FormatException($"Stopped reading before the end: {text}");
                }

                return selected;
            }

            [GeneratedRegex(@"\(|\)|&|\||Category!?=\w+", RegexOptions.CultureInvariant)]
            private static partial Regex Token();

            private bool ReadAnyOf()
            {
                var selected = ReadAllOf();
                while (Takes("|"))
                {
                    var next = ReadAllOf();
                    selected = selected || next;
                }

                return selected;
            }

            private bool ReadAllOf()
            {
                var selected = ReadTerm();
                while (Takes("&"))
                {
                    var next = ReadTerm();
                    selected = selected && next;
                }

                return selected;
            }

            private bool ReadTerm()
            {
                if (Takes("("))
                {
                    var selected = ReadAnyOf();
                    return Takes(")") ? selected : throw new FormatException("A parenthesis is not closed.");
                }

                var term = tokens[position++];
                return term.StartsWith(Excludes, StringComparison.Ordinal)
                    ? !categories.Contains(term[Excludes.Length..])
                    : categories.Contains(term[Includes.Length..]);
            }

            private bool Takes(string token)
            {
                if (position >= tokens.Length || tokens[position] != token)
                {
                    return false;
                }

                position++;
                return true;
            }
        }
    }
}
