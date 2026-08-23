using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Waits written out by hand: a fixture says which Features wait on which and nothing else has to be
    /// true for it to run. What decides a wait has its own tests, and a fixture built on this one is about
    /// what the simulation does once it has been told.
    /// </summary>
    public sealed class Waits
    {
        private readonly List<DependencyVerdict> honoured = [];

        public static Waits On(string dependent, string blocker) => new Waits().And(dependent, blocker);

        public Waits And(string dependent, string blocker)
        {
            honoured.Add(new DependencyVerdict(dependent, blocker, reason: null, blockerPositionedBelow: false));
            return this;
        }

        public static implicit operator ForecastWaits(Waits waits)
            => ForecastWaits.From(new HonouredDependencies(waits.honoured));
    }
}
