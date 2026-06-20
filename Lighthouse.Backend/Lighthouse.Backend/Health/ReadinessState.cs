namespace Lighthouse.Backend.Health
{
    public sealed class ReadinessState : IReadinessState
    {
        private volatile bool isDraining;

        public bool IsDraining => isDraining;

        public void BeginDraining() => isDraining = true;
    }
}
