using System.Reflection;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Epic 5733 slice 01c (ADO #5980) - the structural half of the promise.
    ///
    /// The scenarios in the integration fixtures watch what leaves through the client the framework
    /// hands out. That is only the whole story while nothing builds a client of its own, and this is
    /// where that is held: without it, the first piece of code that reaches for its own client sends
    /// where nothing is looking, and the assertion that nothing was sent quietly becomes an
    /// assertion about nothing.
    ///
    /// Every rule here begins by proving that what it inspects exists. A rule phrased over an empty
    /// set is not a rule that holds, it is a rule with nothing to say - which is exactly the state
    /// this file is in today, and why each scenario is still ignored.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataEmitSeamArchUnitTest
    {
        private const string UsageData = "UsageData";
        private const string PersistenceNamespace = "Lighthouse.Backend.Data.";
        private const string DomainEventDispatcher = "IDomainEventDispatcher";
        private const string CollectorName = "posthog";

        private const string NothingToInspectYet =
            "Pending: the emit path this inspects does not exist yet (Epic 5733 slice 01c, ADO #5980).";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private static readonly string[] BuildingAClientByHand = ["new HttpClient(", "new SocketsHttpHandler(", "new HttpClientHandler("];

        /// <summary>
        /// The rule that keeps "nothing reached the collector" from decaying into a tautology. A
        /// client built by hand never passes the framework's pipeline, so nothing installed there
        /// can see what it sends - and there are already several in this codebase, which is why this
        /// is scoped to the usage data path rather than written as a rule for everybody.
        /// </summary>
        [Test]
        public void NothingOnTheEmitPath_BuildsItsOwnWayOut()
        {
            var sources = EverySourceFileOnTheEmitPath();

            var builtByHand = sources
                .Where(file => BuildingAClientByHand.Any(shape =>
                    File.ReadAllText(file).Contains(shape, StringComparison.Ordinal)))
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sources, Is.Not.Empty,
                    "there is no emit path to inspect, so this rule forbids nothing and would hold "
                    + "however the code was eventually written");
                Assert.That(builtByHand, Is.Empty,
                    "a client built here goes out through a pipeline nothing is watching, so the "
                    + "assertion that an instance without consent contacts nobody stops covering "
                    + "it. Ask the framework for one. Found: " + string.Join(", ", builtByHand));
            }
        }

        /// <summary>
        /// One place names the address, so there is one place to look when somebody asks where this
        /// data goes, and one thing for the rule above to be about.
        /// </summary>
        [Test]
        public void TheAddressTheDataGoesTo_IsWrittenDownInExactlyOnePlace()
        {
            var naming = EveryProductionSourceFile()
                .Where(file => File.ReadAllText(file).Contains(CollectorName, StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(naming, Has.Count.EqualTo(1),
                "the address appears in more than one file, or in none. Spread out, there is no "
                + "single answer to where this data goes and no single thing to point a rule at. "
                + "Found: " + string.Join(", ", naming));
        }

        /// <summary>
        /// No field on the wire can carry a sentence somebody typed, a name they chose or an
        /// identifier of theirs. That is a property of the message's shape rather than a habit
        /// reviewers have to keep, which is the whole reason the design chose closed lists.
        /// </summary>
        [Test]
        public void NothingSentOrReceivedOnThisPath_HasAFieldThatCouldHoldFreeText()
        {
            var messages = EveryMessageTypeOnTheEmitPath();

            var canHoldText = messages
                .SelectMany(message => message.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.PropertyType == typeof(string))
                    .Select(property => $"{message.Name}.{property.Name}"))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages, Is.Not.Empty,
                    "there is no message to inspect, so this rule permits every shape the message "
                    + "could eventually take");
                Assert.That(canHoldText, Is.Empty,
                    "a field of free text is all it takes for a page address, a Team name or "
                    + "something a person typed to reach a third party. Every field here is a "
                    + "choice from a closed list or a bounded number. Found: "
                    + string.Join(", ", canHoldText));
            }
        }

        /// <summary>
        /// The part that sends is allowed to read what is waiting and to send it. Reaching into the
        /// database from there would give it a second answer to whether consent is live, and two
        /// answers is how one of them goes stale.
        /// </summary>
        [Test]
        public void ThePartThatSends_TouchesNothingBeyondWhatIsWaitingAndTheWayOut()
        {
            var forwarder = Architecture.Types
                .Where(type => type.Name.Contains(UsageData, StringComparison.Ordinal)
                    && type.Name.Contains("Forward", StringComparison.Ordinal))
                .ToList();

            var reachesFurther = forwarder
                .SelectMany(type => type.Dependencies.Select(dependency => dependency.Target.FullName))
                .Where(target => target.StartsWith(PersistenceNamespace, StringComparison.Ordinal)
                    || target.Contains("Repository", StringComparison.Ordinal)
                    || target.Contains(DomainEventDispatcher, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forwarder, Is.Not.Empty,
                    "nothing here sends anything yet, so this rule constrains nothing");
                Assert.That(reachesFurther, Is.Empty,
                    "this is meant to empty a queue and make one call. Anything else it can touch "
                    + "is something it can be made to do on a path nobody is watching. Found: "
                    + string.Join(", ", reachesFurther));
            }
        }

        private static List<Type> EveryMessageTypeOnTheEmitPath()
        {
            return [.. typeof(Backend.Program).Assembly
                .GetTypes()
                .Where(type => type.IsPublic
                    && type.Name.StartsWith("UsageDataEvent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal)];
        }

        private static List<string> EverySourceFileOnTheEmitPath()
        {
            return [.. EveryProductionSourceFile()
                .Where(file => Path.GetFileName(file).Contains(UsageData, StringComparison.Ordinal))];
        }

        private static List<string> EveryProductionSourceFile()
        {
            var production = Path.Combine(SolutionRoot(), "Lighthouse.Backend");

            return [.. Directory
                .EnumerateFiles(production, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .OrderBy(file => file, StringComparer.Ordinal)];
        }

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null,
                "Lighthouse.sln could not be found, so none of the rules in this file read the "
                + "source they are written about.");

            return directory!.FullName;
        }
    }
}
