using System.Reflection;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every edge from one packaged context into another has one owning rule: the contracts rule that lists them. The check
    /// reads every static list of strings in the Architecture tests by value and takes the entries shaped "Source -> Target".
    /// No list other than the contracts rule's own may name an entry whose source is in one packaged context and whose target is
    /// in a different one; an edge into another context's Contracts is legal by namespace and listed nowhere. Two members are
    /// left out by name and compared with the lists both ways: the contracts rule's own list, and the list of edges that became
    /// legal, which records compiled edges instead of allowing them. A made-up list that names such an edge is named by the same check.
    /// </summary>
    [TestFixture]
    public class EachCrossContextEdgeHasOneOwningRuleArchUnitTest
    {
        private const string TheEdgeSeparator = " -> ";

        private static readonly Lazy<IReadOnlyList<string>> PackagedContextsOfTheCode = new(() =>
        {
            using var production = IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly);

            return PackagedContexts.InTheAssembly(production);
        });

        private static readonly (Type Owner, string Member)[] ListsLeftOutByName =
        [
            (typeof(ContextContractsArchUnitTest), "CrossContextEdgesOutsideContracts"),
            (typeof(OrganisationSetupIsAnOpenHostArchUnitTest), "WorkItemsEdgesIntoSetUpThatBecomeLegal"),
        ];

        private static readonly string[] AMadeUpList =
        [
            "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver -> Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementCadenceCalendar -> Lighthouse.Backend.Modules.Refinement.Domain.Cadence",
            "Lighthouse.Backend.API.TeamController -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.TeamDeleted",
            "Lighthouse.Backend.Modules.RefinementFixtures.Application.AType -> Lighthouse.Backend.Modules.AccessFixtures.Application.AnotherType",
        ];

        [Test]
        public void NoPerModuleBoundaryListNamesAnEdgeFromOneContextIntoAnother()
        {
            var lists = GivenTheListsOfTheArchitectureTests();

            var named = WhenTheEdgesBetweenContextsAreTaken(lists.Where(list => !IsLeftOutByName(list)));

            ThenNoListNamesAnEdgeBetweenContexts(lists, named);
        }

        [Test]
        public void AMadeUpListNamingAnEdgeFromRefinementIntoAccess_IsNamedWithItsListItsEntryAndTheContractsRuleAsOwner()
        {
            var madeUp = new List<ListedEdges> { new(typeof(EachCrossContextEdgeHasOneOwningRuleArchUnitTest), nameof(AMadeUpList), AMadeUpList) };

            var named = WhenTheEdgesBetweenContextsAreTaken(madeUp);

            ThenOnlyTheEdgeFromRefinementIntoAccessIsNamedWithItsOwner(named);
        }

        [Test]
        public void TheListsLeftOutByName_StillExistAndStillNameAnEdgeBetweenContexts()
        {
            var lists = GivenTheListsOfTheArchitectureTests();

            var leftOutAndFound = WhenTheListsLeftOutAreLookedUp(lists);

            ThenEveryListLeftOutIsFoundAndStillNamesAnEdgeBetweenContexts(leftOutAndFound);
        }

        private static List<ListedEdges> GivenTheListsOfTheArchitectureTests()
        {
            var checkedType = typeof(EachCrossContextEdgeHasOneOwningRuleArchUnitTest);
            const BindingFlags anyStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

            var lists = new List<ListedEdges>();
            foreach (var type in checkedType.Assembly.GetTypes().Where(type => type != checkedType && type.Namespace == checkedType.Namespace))
            {
                var fields = type.GetFields(anyStatic).Where(field => IsAListOfStrings(field.FieldType) && !field.IsLiteral)
                    .Select(field => (field.Name, Value: field.GetValue(null)));
                var properties = type.GetProperties(anyStatic).Where(property => property.GetIndexParameters().Length == 0 && property.CanRead && IsAListOfStrings(property.PropertyType))
                    .Select(property => (property.Name, Value: property.GetValue(null)));

                lists.AddRange(fields.Concat(properties)
                    .Where(member => member.Value is not null)
                    .Select(member => new ListedEdges(type, member.Name, [.. ((IEnumerable<string>)member.Value!).Where(entry => entry is not null && IsShapedAsAnEdge(entry))])));
            }

            return lists;
        }

        private static bool IsAListOfStrings(Type type)
            => type != typeof(string) && typeof(IEnumerable<string>).IsAssignableFrom(type);

        private static bool IsShapedAsAnEdge(string entry)
        {
            var parts = entry.Split(TheEdgeSeparator);

            return parts.Length == 2 && parts.All(part => part.Length > 0 && !part.Any(char.IsWhiteSpace));
        }

        private static bool IsLeftOutByName(ListedEdges list)
            => ListsLeftOutByName.Any(left => left.Owner == list.Owner && left.Member == list.Member);

        private static List<NamedEdge> WhenTheEdgesBetweenContextsAreTaken(IEnumerable<ListedEdges> lists)
        {
            var contexts = PackagedContextsOfTheCode.Value;

            return [.. lists.SelectMany(list => list.Entries
                .Where(entry => IsBetweenTwoDifferentContexts(entry, contexts))
                .Select(entry => new NamedEdge(list, entry)))];
        }

        private static bool IsBetweenTwoDifferentContexts(string entry, IReadOnlyList<string> contexts)
        {
            var parts = entry.Split(TheEdgeSeparator);
            var source = ContextOf(parts[0], contexts);
            var target = ContextOf(parts[1], contexts);

            return source is not null && target is not null && source != target;
        }

        private static string? ContextOf(string typeName, IReadOnlyList<string> contexts)
        {
            var underTheModulesRoot = $"{ContextContractsMembership.TheModulesRoot}.";
            if (!typeName.StartsWith(underTheModulesRoot, StringComparison.Ordinal))
            {
                return null;
            }

            var root = typeName[underTheModulesRoot.Length..].Split('.')[0];

            return contexts.Contains(root, StringComparer.Ordinal) ? root : null;
        }

        private static List<(string Owner, string Member, bool Found, bool NamesAnEdgeBetweenContexts)> WhenTheListsLeftOutAreLookedUp(List<ListedEdges> lists)
        {
            var contexts = PackagedContextsOfTheCode.Value;

            return [.. ListsLeftOutByName.Select(left =>
            {
                var found = lists.FirstOrDefault(list => list.Owner == left.Owner && list.Member == left.Member);

                return (left.Owner.Name, left.Member, found is not null, found is not null && found.Entries.Any(entry => IsBetweenTwoDifferentContexts(entry, contexts)));
            })];
        }

        private static string TheWordingOf(NamedEdge named)
            => $"The list {named.List.Owner.Name}.{named.List.Member} names the edge {named.Entry} from one context into another. " +
               "Why it matters: the contracts rule owns every edge from one context into another, so a second list naming one gives the edge two owners and the per-module rule keeps a ratchet the contracts rule already holds. " +
               "How to fix: take the entry off this list; if the compiled code still makes the edge outside the target's Contracts, add it to the contracts rule's list (owner: the contracts rule), otherwise reach the target through its Contracts.";

        private static void ThenNoListNamesAnEdgeBetweenContexts(List<ListedEdges> lists, List<NamedEdge> named)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(lists.Count(list => !IsLeftOutByName(list) && list.Entries.Count > 0), Is.GreaterThan(0),
                    "No list of edges was read from the Architecture tests, so the check looked at nothing. Why it matters: a check over nothing passes while it guards nothing. How to fix: read every static list of strings in the Architecture tests by value.");
                Assert.That(named, Is.Empty, string.Join(Environment.NewLine, named.Select(TheWordingOf)));
            }
        }

        private static void ThenOnlyTheEdgeFromRefinementIntoAccessIsNamedWithItsOwner(List<NamedEdge> named)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Select(edge => edge.Entry), Is.EqualTo(new[] { AMadeUpList[0] }),
                    $"The check named [{string.Join(" | ", named.Select(edge => edge.Entry))}] in a made-up list. Expected exactly the edge from Refinement into Access, and not the edge inside one context, the edge from the API, nor the edge between made-up fixture roots. Why it matters: a check that names a legal edge makes the lists grow, and one that misses an edge lets a second owner appear unseen. How to fix: name an entry only when its source and target are in two different packaged contexts.");
                var wording = named.Count == 1 ? TheWordingOf(named[0]) : string.Empty;
                Assert.That(wording, Does.Contain(nameof(AMadeUpList)).And.Contain(AMadeUpList[0]).And.Contain("contracts rule"),
                    $"The wording for the made-up edge reads: {wording}. Expected it to name the list, the entry and the contracts rule as owner. Why it matters: a reader of a failing run must know which list to edit and who owns the edge. How to fix: name the list, the entry and the owner in the failure text.");
            }
        }

        private static void ThenEveryListLeftOutIsFoundAndStillNamesAnEdgeBetweenContexts(List<(string Owner, string Member, bool Found, bool NamesAnEdgeBetweenContexts)> leftOut)
        {
            var gone = leftOut.Where(list => !list.Found).Select(list => $"{list.Owner}.{list.Member}").ToList();
            var idle = leftOut.Where(list => list.Found && !list.NamesAnEdgeBetweenContexts).Select(list => $"{list.Owner}.{list.Member}").ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gone, Is.Empty, $"These lists left out by name are not among the lists read: {string.Join(", ", gone)}. Why it matters: a name that matches no list leaves the exclusion pointing at nothing, and a renamed list would be read as an owner of edges. How to fix: name the list as it is now, or take it off the left-out names.");
                Assert.That(idle, Is.Empty, $"These lists left out by name no longer name an edge between contexts: {string.Join(", ", idle)}. Why it matters: an exclusion that excludes nothing would hide the next edge added to that list. How to fix: take the list off the left-out names.");
            }
        }

        private sealed record ListedEdges(Type Owner, string Member, IReadOnlyList<string> Entries);

        private sealed record NamedEdge(ListedEdges List, string Entry);
    }
}
