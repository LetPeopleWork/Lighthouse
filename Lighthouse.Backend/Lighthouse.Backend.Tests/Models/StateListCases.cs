using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Stored Doing lists of the kind the previous release wrote, with the mappings that go with them: states of
    /// the tracker, a group named after one of its own states, a group's name standing in for its states, and a
    /// group's name listed beside one of its own states. States come with other casing and with
    /// spaces, tabs or non-breaking spaces around them. The same seed gives the same cases on every run.
    /// </summary>
    public sealed record StateListCase(int Index, Team Owner, List<string> StoredDoing, Dictionary<string, ListEntryKind> NamedEntries)
    {
        public bool HasShadowing => NamedEntries.ContainsValue(ListEntryKind.Shadowing);

        public override string ToString()
            => $"case {Index}: doing [{string.Join(", ", StoredDoing.Select(entry => $"'{entry}'"))}] with {string.Join("; ", Owner.StateMappings.Select(m => $"{m.Name} = [{string.Join(", ", m.States)}]"))}";
    }

    public static class StateListCases
    {
        public const int Seed = 6458;

        private enum Shape
        {
            Unreferenced,
            NamedAfterOwnState,
            CollapsedUnderFreshName,
            ShadowedByFreshName,
        }

        private static readonly string[] RawStates =
            ["Analysis", "Code Review", "Testing", "Waiting", "Blocked", "Active", "Resolved", "In Progress", "Review", "QA", "Deploy"];

        private static readonly string[] FreshNames = ["Group A", "Group B", "Group C"];

        private const char NonBreakingSpace = (char)160;

        private static readonly string[] Paddings = [" {0}", "{0} ", "\t{0}", $"{NonBreakingSpace}{{0}}{NonBreakingSpace}", "  {0}  "];

        public static IEnumerable<StateListCase> Generate(int count)
        {
            var random = new Random(Seed);
            for (var index = 0; index < count; index++)
            {
                yield return OneCase(random, index);
            }
        }

        // Case and surrounding whitespace never change which state is meant.
        private static string Disguised(Random random, string state)
        {
            var text = random.Next(4) == 0 ? state.ToUpperInvariant() : state;
            return random.Next(4) == 0 ? string.Format(System.Globalization.CultureInfo.InvariantCulture, Paddings[random.Next(Paddings.Length)], text) : text;
        }

        private static StateListCase OneCase(Random random, int index)
        {
            var unused = RawStates.OrderBy(_ => random.Next()).ToList();
            var freshNames = FreshNames.OrderBy(_ => random.Next()).ToList();
            var owner = new Team();
            var entries = new List<string>();
            var named = new Dictionary<string, ListEntryKind>(StringComparer.OrdinalIgnoreCase);

            var mappingCount = random.Next(0, 4);
            for (var m = 0; m < mappingCount; m++)
            {
                var members = unused.Take(random.Next(1, 4)).ToList();
                unused.RemoveRange(0, members.Count);
                var shape = (Shape)random.Next(4);
                var name = shape == Shape.NamedAfterOwnState ? members[random.Next(members.Count)] : freshNames[m];

                owner.StateMappings.Add(new StateMapping { Name = name, States = [.. members.Select(member => random.Next(5) == 0 ? member.ToUpperInvariant() : member)] });
                entries.AddRange(EntriesFor(random, shape, name, members, named));
            }

            entries.AddRange(unused.Where(_ => random.Next(3) != 0).Select(state => Disguised(random, state)));

            var listed = entries.OrderBy(_ => random.Next()).ToList();
            if (listed.Count > 0 && random.Next(4) == 0)
            {
                listed.Add(listed[random.Next(listed.Count)]);
            }

            owner.DoingStates = listed;
            return new StateListCase(index, owner, listed, named);
        }

        private static List<string> EntriesFor(Random random, Shape shape, string name, List<string> members, Dictionary<string, ListEntryKind> named)
        {
            var someMembers = members.Where(_ => random.Next(2) == 0).ToList();
            switch (shape)
            {
                case Shape.NamedAfterOwnState:
                    var nameListed = random.Next(2) == 0;
                    var others = someMembers.Where(member => !member.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(member => Disguised(random, member)).ToList();
                    if (!nameListed)
                    {
                        return others;
                    }

                    named[name] = ListEntryKind.StateOfItsOwnMapping;
                    return [.. others, random.Next(4) == 0 ? name.ToUpperInvariant() : name];
                case Shape.CollapsedUnderFreshName:
                    named[name] = ListEntryKind.CollapsedGroup;
                    return [random.Next(4) == 0 ? name.ToUpperInvariant() : name];
                case Shape.ShadowedByFreshName:
                    named[name] = ListEntryKind.Shadowing;
                    var beside = someMembers.Count > 0 ? someMembers : [members[0]];
                    return [name, .. beside.Select(member => Disguised(random, member))];
                default:
                    return [.. someMembers.Select(member => Disguised(random, member))];
            }
        }
    }
}
