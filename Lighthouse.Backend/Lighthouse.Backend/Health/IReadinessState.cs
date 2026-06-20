namespace Lighthouse.Backend.Health
{
    public interface IReadinessState
    {
        bool IsDraining { get; }

        void BeginDraining();
    }
}
