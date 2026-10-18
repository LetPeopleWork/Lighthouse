namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Which Teams, or which Portfolios, a connection save changed what they ask the tracker for. Each map holds,
    /// per owner id, a rendering of everything its fetch depends on, taken before and after the save; an owner
    /// moved when the two differ. Teams and Portfolios are numbered separately, so each grain is asked on its own.
    /// </summary>
    public static class OwnersWhoseFetchMoved
    {
        // An owner that appeared during the save has nothing to compare against, so it is updated rather than
        // trusted to already ask for the right thing. One that disappeared has nothing left to update.
        public static IReadOnlyList<int> Between(IReadOnlyDictionary<int, string> before, IReadOnlyDictionary<int, string> after)
            => [.. after
                .Where(owner => !before.TryGetValue(owner.Key, out var fetchBefore) || fetchBefore != owner.Value)
                .Select(owner => owner.Key)
                .Order()];
    }
}
