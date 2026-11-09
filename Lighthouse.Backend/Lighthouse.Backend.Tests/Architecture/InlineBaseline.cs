namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Compares what a rule finds in the code with the list of entries written next to the rule, in both
    /// directions: an offender not on the list fails and is named with the rule's fix hint, and a listed entry
    /// that no longer offends fails until it is deleted, so a list can only shrink. The order of either list
    /// and repeated entries do not matter.
    /// </summary>
    internal static class InlineBaseline
    {
        public static void AssertMatches(IEnumerable<string> actual, IEnumerable<string> baseline, string fixHint)
        {
            var offending = actual.ToHashSet(StringComparer.Ordinal);
            var listed = baseline.ToHashSet(StringComparer.Ordinal);

            var newcomers = offending.Except(listed).Order(StringComparer.Ordinal).ToList();
            var stale = listed.Except(offending).Order(StringComparer.Ordinal).ToList();

            var problems = new List<string>();

            if (newcomers.Count > 0)
            {
                problems.Add($"Not on the list, and offending: {string.Join(", ", newcomers)}. {fixHint}");
            }

            if (stale.Count > 0)
            {
                problems.Add($"On the list, but no longer offending: {string.Join(", ", stale)}. Good - delete {(stale.Count == 1 ? "it" : "them")} from the list so it cannot come back unseen.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, problems));
            }
        }
    }
}
