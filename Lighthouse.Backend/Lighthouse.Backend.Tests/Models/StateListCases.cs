using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Stored Doing lists of the kind the previous release wrote, with the mappings that go with them. Every case
    /// is one the write path accepts: a mapping that is named after one of its own states, or named after nothing
    /// listed, and no mapping that lists another mapping's name. The same seed gives the same cases on every run.
    /// </summary>
    public sealed record StateListCase(int Index, Team Owner, List<string> StoredDoing)
    {
        public override string ToString()
            => $"case {Index}: doing [{string.Join(", ", StoredDoing)}] with {string.Join("; ", Owner.StateMappings.Select(m => $"{m.Name} = [{string.Join(", ", m.States)}]"))}";
    }

    public static class StateListCases
    {
        public const int Seed = 6458;

        private static readonly string[] RawStates =
            ["Analysis", "Code Review", "Testing", "Waiting", "Blocked", "Active", "Resolved", "In Progress", "Review", "QA", "Deploy"];

        private static readonly string[] FreshNames = ["Group A", "Group B", "Group C"];

        public static IEnumerable<StateListCase> Generate(int count)
        {
            var random = new Random(Seed);
            for (var index = 0; index < count; index++)
            {
                yield return OneCase(random, index);
            }
        }

        private static StateListCase OneCase(Random random, int index)
        {
            var unused = RawStates.OrderBy(_ => random.Next()).ToList();
            var owner = new Team();
            var freshNames = FreshNames.OrderBy(_ => random.Next()).ToList();
            var selfNamed = new List<string>();

            var mappingCount = random.Next(0, 4);
            for (var m = 0; m < mappingCount; m++)
            {
                var members = unused.Take(random.Next(1, 4)).ToList();
                unused.RemoveRange(0, members.Count);

                var isSelfNamed = random.Next(2) == 0;
                var name = isSelfNamed ? members[random.Next(members.Count)] : freshNames[m];
                if (isSelfNamed)
                {
                    selfNamed.Add(name);
                }

                owner.StateMappings.Add(new StateMapping { Name = name, States = members });
            }

            var listed = RawStates
                .Where(_ => random.Next(3) != 0)
                .OrderBy(_ => random.Next())
                .Select(state => random.Next(5) == 0 ? state.ToUpperInvariant() : state)
                .ToList();

            if (listed.Count > 0 && random.Next(4) == 0)
            {
                listed.Add(listed[random.Next(listed.Count)]);
            }

            // The previous release left a self-named group in the list under its name alone.
            foreach (var name in selfNamed.Where(_ => random.Next(2) == 0))
            {
                var members = owner.StateMappings.Single(m => m.Name == name).States;
                listed.RemoveAll(state => members.Contains(state, StringComparer.OrdinalIgnoreCase));
                listed.Insert(random.Next(listed.Count + 1), name);
            }

            owner.DoingStates = listed;
            return new StateListCase(index, owner, listed);
        }
    }
}
