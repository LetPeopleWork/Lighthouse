namespace Lighthouse.Backend.Tests.TestHelpers.Health
{
    public enum HealthDatabaseState
    {
        ReachableAndMigrated,
        ReachableWithPendingMigrations,
        Unreachable,
    }
}
