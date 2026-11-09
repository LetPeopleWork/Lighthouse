namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// L12: the start-up seeder of the switch is idempotent. Whatever a database holds when the instance starts, two
    /// starts leave exactly one switch, off when none was stored, and never change a stored one.
    /// </summary>
    [TestFixture]
    public class EveryoneSignedInSeederTest
    {
        private EveryoneSignedInScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new EveryoneSignedInScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [TestCase(StoredBefore.Nothing, false, TestName = "L12_with_no_switch_stored_two_starts_store_one_switch_off")]
        [TestCase(StoredBefore.On, true, TestName = "L12_a_switch_stored_on_stays_on_and_single_after_two_starts")]
        [TestCase(StoredBefore.Off, false, TestName = "L12_a_switch_stored_off_stays_off_and_single_after_two_starts")]
        public async Task L12_two_starts_leave_one_switch_and_never_change_a_stored_one(StoredBefore storedBefore, bool onAfterwards)
        {
            TheDatabaseHolds(storedBefore);
            await scenario.TheInstanceHasStarted();
            var afterTheFirstStart = scenario.TheStoredRows();

            await scenario.TheInstanceHasStarted();

            var afterTheSecondStart = scenario.TheStoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheSecondStart.Select(row => row.On), Is.EqualTo(new[] { onAfterwards }), $"WHAT: with {storedBefore} stored before, two starts left the switches {string.Join(", ", afterTheSecondStart)}. WHY: the switch is one row, off unless a System Admin turned it on, and a start must never open the instance to everyone. HOW: let the seeder insert the switch off only when no row exists, and leave an existing row alone.");
                Assert.That(afterTheSecondStart, Is.EqualTo(afterTheFirstStart), "WHAT: the second start changed the stored switch. WHY: a start that rewrites the row would change its token and make a System Admin's open page fail with a conflict. HOW: write nothing when the row exists.");
            }
        }

        private void TheDatabaseHolds(StoredBefore storedBefore)
        {
            if (storedBefore != StoredBefore.Nothing)
            {
                scenario.TheSwitchIsStored(on: storedBefore == StoredBefore.On);
            }
        }

        public enum StoredBefore
        {
            Nothing,
            On,
            Off,
        }
    }
}
