namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// Every verdict from one evaluation, handed over together so a caller reads one answer for a whole page
    /// rather than asking the same question again per row. The copy on the way in is deliberate: what was
    /// decided has to stay decided, whatever happens to the list it was built from afterwards.
    /// </summary>
    public sealed class HonouredDependencies
    {
        private readonly List<DependencyVerdict> verdicts;

        public HonouredDependencies(IEnumerable<DependencyVerdict> verdicts)
        {
            this.verdicts = verdicts.ToList();
        }

        public IReadOnlyCollection<DependencyVerdict> Verdicts => verdicts;
    }
}
