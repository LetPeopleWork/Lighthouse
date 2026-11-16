using System.Reflection;
using System.Runtime.ExceptionServices;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.DirectThrow;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A connector that cannot do what one of its interfaces asks says so by throwing a NotSupportedException, and
    /// every such method is held on a list by name that may only shrink. The rule reads the compiled code, so a throw
    /// moved into a private helper, an async body or a lambda is still caught, and an exception type derived from
    /// NotSupportedException counts too. Made-up connectors show each of those cases, and that a type outside the
    /// connector port, or another exception, is not reported.
    /// </summary>
    [TestFixture]
    public class ConnectorsDeclareNoUnsupportedMembersArchUnitTest
    {
        private const string RuleCoreTypeName = "Lighthouse.Backend.Tests.Architecture.ConnectorUnsupportedMembers";

        private const string RuleName = "FindIn";

        private const string ListName = "Listed";

        private const string ConnectorPort = @"^Lighthouse\.Backend\.Services\.Interfaces\.WorkTrackingConnectors$";

        private const string AllOfTheBackend = @"^Lighthouse\.Backend(\.|$)";

        private const string StrategySignature = "(HttpRequestMessage, WorkTrackingSystemConnection, CancellationToken)";

        private const string AConnectorMethodThatIsGone = "AzureDevOpsWorkTrackingConnector.AMethodThatNoLongerExists()";

        private const string FixHint =
            "Each of them creates a NotSupportedException. A connector that cannot do something must not implement the interface that asks for " +
            "it. Remove the interface from the connector's list (and its member), and let the caller test `connector is <Capability>`; " +
            "if the answer depends on the connection, add a probe to the capability as `SupportsIncrementalSync` does.";

        private static readonly Lazy<ModuleDefinition> MadeUpConnectors = new(() => IlDependencies.ReadModule(typeof(AStrategyThatRefusesDirectly).Assembly));

        private static readonly Lazy<ModuleDefinition> TheBackend = new(() => IlDependencies.ReadModule(typeof(IWorkTrackingAuthStrategy).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { MadeUpConnectors, TheBackend }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryConnectorMethodThatCreatesANotSupportedException_IsOnTheListWithItsReason()
        {
            var listed = GivenTheListWithItsReasons();

            var found = WhenTheRuleRunsOverTheBackend();

            ThenEveryListedEntryCarriesAReason(listed);
            ThenTheListMatches(found, listed.Keys);
        }

        [TestCase("DirectThrow", "AStrategyThatRefusesDirectly.ApplyAsync" + StrategySignature)]
        [TestCase("PrivateHelper", "AStrategyThatRefusesInAHelper.Refuse()")]
        [TestCase("AsyncBody", "AStrategyThatRefusesAfterAnAwait.ApplyAsync" + StrategySignature)]
        [TestCase("Lambda", "AStrategyThatRefusesInALambda.ApplyAsync" + StrategySignature)]
        [TestCase("DerivedException", "AStrategyThatRefusesOnThisPlatform.ApplyAsync" + StrategySignature)]
        public void AMadeUpConnectorThatRefuses_IsReportedByName(string madeUpCase, string expectedEntry)
        {
            var found = WhenTheRuleRunsOverTheMadeUpConnectors(madeUpCase);

            ThenTheRuleReportsExactly(found, [expectedEntry], madeUpCase);
        }

        [TestCase("NotAConnector")]
        [TestCase("OtherException")]
        public void AMadeUpTypeOutsideTheRule_IsNotReported(string madeUpCase)
        {
            var found = WhenTheRuleRunsOverTheMadeUpConnectors(madeUpCase);

            ThenTheRuleReportsExactly(found, [], madeUpCase);
        }

        [Test]
        public void AConnectorMethodThatIsNotOnTheList_FailsNamedWithTheFixHint()
        {
            var listed = GivenTheListWithItsReasons();
            var leftOff = listed.Keys.Order(StringComparer.Ordinal).First();

            var failure = WhenTheBackendIsComparedWithTheList(listed.Keys.Where(entry => entry != leftOff));

            ThenTheFailureNamesTheNewcomerWithTheFixHint(failure, leftOff);
        }

        [Test]
        public void AListedConnectorMethodThatNoLongerRefuses_FailsNamedForDeletion()
        {
            var listed = GivenTheListWithItsReasons();

            var failure = WhenTheBackendIsComparedWithTheList([.. listed.Keys, AConnectorMethodThatIsGone]);

            ThenTheFailureNamesTheStaleEntry(failure, AConnectorMethodThatIsGone);
        }

        private static Type TheRuleCore()
        {
            var core = typeof(ConnectorsDeclareNoUnsupportedMembersArchUnitTest).Assembly.GetType(RuleCoreTypeName);

            Assert.That(
                core,
                Is.Not.Null,
                $"There is no rule core {RuleCoreTypeName} in the test assembly. Why it matters: without it no test notices a connector " +
                "that says it cannot do what its interface asks, and the number of such connector methods can grow unseen. How to fix: " +
                $"add the static class {RuleCoreTypeName} with {RuleName}(ModuleDefinition module, string typeNamespacePattern, string " +
                $"portNamespacePattern) returning the entries Type.Method(ParameterTypes), and {ListName}, the entries of today's " +
                "connectors, each with its reason.");

            return core!;
        }

        private static Dictionary<string, string> GivenTheListWithItsReasons()
        {
            var core = TheRuleCore();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var value = core.GetProperty(ListName, flags)?.GetValue(null) ?? core.GetField(ListName, flags)?.GetValue(null);

            Assert.That(
                value,
                Is.InstanceOf<IEnumerable<KeyValuePair<string, string>>>(),
                $"{RuleCoreTypeName}.{ListName} is {value?.GetType().FullName ?? "missing"}, not a static list of entries with their reasons. " +
                "Why it matters: the rule compares what it finds with this list, and a reader must see why each entry is allowed. How " +
                "to fix: make it a static IReadOnlyDictionary<string, string> from each Type.Method(ParameterTypes) entry to its reason.");

            var listed = ((IEnumerable<KeyValuePair<string, string>>)value!).ToDictionary(StringComparer.Ordinal);

            Assert.That(
                listed,
                Is.Not.Empty,
                $"{RuleCoreTypeName}.{ListName} is empty. Why it matters: today's connectors still hold methods that throw a " +
                "NotSupportedException, so an empty list cannot match them. How to fix: list each of them with its reason.");

            return listed;
        }

        private static List<string> WhenTheRuleRunsOverTheBackend()
            => RunTheRule(TheBackend.Value, AllOfTheBackend);

        private static List<string> WhenTheRuleRunsOverTheMadeUpConnectors(string madeUpCase)
            => RunTheRule(MadeUpConnectors.Value, $@"^Lighthouse\.Backend\.Tests\.Architecture\.ConnectorRatchetFixtures\.{madeUpCase}$");

        private static AssertionException? WhenTheBackendIsComparedWithTheList(IEnumerable<string> listed)
        {
            var found = WhenTheRuleRunsOverTheBackend();

            return Assert.Throws<AssertionException>(
                () => InlineBaseline.AssertMatches(found, listed, FixHint),
                "The comparison passed although the list differs from what the rule found by one entry. Why it matters: a list that " +
                "lets a newcomer in, or keeps a gone entry, lets a refusing connector method arrive unseen. How to fix: compare the " +
                "rule's answer with the list in both directions.");
        }

        private static List<string> RunTheRule(ModuleDefinition module, string typeNamespacePattern)
        {
            var rule = TheRuleCore().GetMethod(
                RuleName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(ModuleDefinition), typeof(string), typeof(string)]);

            Assert.That(
                rule,
                Is.Not.Null,
                $"{RuleCoreTypeName} has no static {RuleName}(ModuleDefinition, string, string). Why it matters: the test hands it " +
                "the compiled code and the namespaces to read. How to fix: add it, returning the entries Type.Method(ParameterTypes).");

            object? answer;

            try
            {
                answer = rule!.Invoke(null, [module, typeNamespacePattern, ConnectorPort]);
            }
            catch (TargetInvocationException invocation) when (invocation.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(invocation.InnerException).Throw();
                throw;
            }

            Assert.That(
                answer,
                Is.InstanceOf<IEnumerable<string>>(),
                $"{RuleCoreTypeName}.{RuleName} returned {answer?.GetType().FullName ?? "nothing"}. How to fix: return the entries as a list of strings.");

            return [.. (IEnumerable<string>)answer!];
        }

        private static void ThenEveryListedEntryCarriesAReason(Dictionary<string, string> listed)
        {
            var withoutAReason = listed.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key).ToList();

            Assert.That(
                withoutAReason,
                Is.Empty,
                $"These listed entries carry no reason: {string.Join(", ", withoutAReason)}. Why it matters: an entry is allowed only " +
                "because of what it says about the connector, and a bare name invites the list to grow. How to fix: write, in plain " +
                "words, why that method still throws.");
        }

        private static void ThenTheListMatches(List<string> found, IEnumerable<string> listed)
            => InlineBaseline.AssertMatches(found, listed, FixHint);

        private static void ThenTheRuleReportsExactly(List<string> found, string[] expected, string madeUpCase)
        {
            Assert.That(
                found,
                Is.EquivalentTo(expected),
                $"Over the made-up connectors of '{madeUpCase}' the rule reported [{string.Join(", ", found)}], expected " +
                $"[{string.Join(", ", expected)}]. Why it matters: a rule that reads only top-level method bodies misses async bodies and " +
                "lambdas, one that matches only the exact exception type misses derived ones, one that reports a helper against its " +
                "caller names the wrong method, and one that reads every type reports what is no connector. How to fix: for every type " +
                "implementing an interface of the port namespace, report Type.Method(ParameterTypes) for each method whose code, with " +
                "the compiler-made nested types folded into the method that wrote them, creates a NotSupportedException or a type " +
                "derived from it.");
        }

        private static void ThenTheFailureNamesTheNewcomerWithTheFixHint(AssertionException? failure, string newcomer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure?.Message,
                    Does.Contain($"Not on the list, and offending: {newcomer}. {FixHint}"),
                    $"The failure must name {newcomer}, and only it, as not on the list, followed by the fix hint, so the reader knows " +
                    "which connector method to change and how.");
                Assert.That(
                    failure?.Message,
                    Does.Not.Contain("no longer offending"),
                    "Every other listed entry is still found, so the failure must not ask to delete one from the list.");
            }
        }

        private static void ThenTheFailureNamesTheStaleEntry(AssertionException? failure, string stale)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure?.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"The failure must name {stale}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure?.Message,
                    Does.Not.Contain("Not on the list"),
                    "Everything the rule found is on the list, so the failure must not name a newcomer.");
            }
        }
    }
}
