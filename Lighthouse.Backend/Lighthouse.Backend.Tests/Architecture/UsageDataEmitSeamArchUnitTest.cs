using System.Reflection;
using System.Text.Json.Serialization;
using Lighthouse.Backend.Services.Interfaces.UsageData;
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
        /// Every field that goes to the collector, and the whole of it. Written out by hand rather
        /// than worked out from the code, because the point is that adding one is something somebody
        /// comes here and does: the page this product ships tells people what it collects, and a
        /// field that turned up without a line added here would be one nobody told them about.
        /// </summary>
        private static readonly string[] EveryFieldTheCollectorIsSent =
        [
            "$geoip_disable",
            "$ip",
            "api_key",
            "auth_enabled",
            "deployment_mode",
            "distinct_id",
            "enabled",
            "event",
            "licence_tier",
            "optional_feature",
            "properties",
            "route",
            "timestamp",
            "version",
            "work_tracking_system",
        ];

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
        /// Nothing a browser hands in can carry a sentence somebody typed, a name they chose or an
        /// identifier of theirs. That is a property of the message's shape rather than a habit
        /// reviewers have to keep, which is the whole reason the design chose closed lists.
        ///
        /// What goes the other way cannot be held like this, and the rule below is the one that
        /// holds it: the route this application substitutes, the version, the tier and the key are
        /// all text, and all have to be.
        /// </summary>
        [Test]
        public void NothingOnTheWayIn_HasAFieldThatCouldHoldFreeText()
        {
            var messages = EveryTypeOnTheWayIn();

            var canHoldText = messages
                .SelectMany(message => message.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.PropertyType == typeof(string))
                    .Select(property => $"{message.Name}.{property.Name}"))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages, Is.Not.Empty,
                    "there is nothing arriving to inspect, so this rule permits every shape the "
                    + "message could eventually take");
                Assert.That(canHoldText, Is.Empty,
                    "a field of free text is all it takes for a page address, a Team name or "
                    + "something a person typed to reach a third party. Every field here is a "
                    + "choice from a closed list or a bounded number. Found: "
                    + string.Join(", ", canHoldText));
            }
        }

        /// <summary>
        /// The other direction, held by a different promise. What leaves is mostly text and has to
        /// be - the route this application substitutes for the address the browser was at, what
        /// version this is, which tier its licence is - so "no field could hold free text" is not
        /// available here and saying it anyway would be saying nothing.
        ///
        /// What is available is this: the fields that go out are the fields written down, all of
        /// them and only them. Adding one then becomes a thing somebody comes here and does, which
        /// is the point - the page this product ships tells people what it collects, and the way
        /// that page goes out of date is a field somebody added in passing.
        ///
        /// The shapes are reached through the thing that sends rather than by their names. They are
        /// declared private, beside the way out and nowhere else, so a rule that went looking for a
        /// name would inspect nothing at all and hold whatever was added to them.
        /// </summary>
        [Test]
        public void EveryFieldOnTheWayOut_IsOneSomebodyWroteDown()
        {
            var shapes = EveryShapeOnTheWayOut();

            var fields = shapes
                .SelectMany(shape => shape.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(WhatItIsCalledOnTheWire))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shapes, Is.Not.Empty,
                    "nothing was found that goes to the collector, so this rule permits every field "
                    + "a message could eventually carry");
                Assert.That(fields, Is.EquivalentTo(EveryFieldTheCollectorIsSent),
                    "what goes out is not what is written down here. A field that arrived without "
                    + "somebody adding it to that list is one nobody decided to send and nobody was "
                    + "told about. Found: " + string.Join(", ", fields));
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

        private static List<Type> EveryTypeOnTheWayIn()
        {
            return [.. typeof(Backend.Program).Assembly
                .GetTypes()
                .Where(type => type.IsPublic
                    && type.Name.StartsWith("UsageDataEvent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal)];
        }

        /// <summary>
        /// Every shape that is serialised to the collector, found through whatever implements the
        /// way out. They are declared inside it and are not public, which is right - nothing else
        /// has any business naming them - and is why they are reached this way rather than by name.
        /// </summary>
        private static List<Type> EveryShapeOnTheWayOut()
        {
            var waysOut = typeof(Backend.Program).Assembly
                .GetTypes()
                .Where(type => typeof(IUsageDataPublisher).IsAssignableFrom(type) && !type.IsInterface);

            return [.. waysOut
                .SelectMany(EverythingDeclaredInside)
                .Distinct()
                .OrderBy(type => type.Name, StringComparer.Ordinal)];
        }

        private static IEnumerable<Type> EverythingDeclaredInside(Type declaring)
        {
            foreach (var nested in declaring.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                yield return nested;

                foreach (var deeper in EverythingDeclaredInside(nested))
                {
                    yield return deeper;
                }
            }
        }

        private static string WhatItIsCalledOnTheWire(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
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
