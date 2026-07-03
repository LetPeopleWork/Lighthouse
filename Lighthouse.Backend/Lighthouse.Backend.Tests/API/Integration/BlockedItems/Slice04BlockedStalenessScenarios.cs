using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 04: Blocked → Stale linkage (settings contract).
    /// Job: job-flow-coach-stale-when-blocked-too-long. Persona: flow-coach (Priya) + config-admin.
    /// Driving port: team settings PUT/GET (blockedStalenessThresholdDays). Stale rendering (driver +
    /// context, stale-once — UC-1) is FE deriveStaleness (Vitest, DELIVER). All [Ignore]-pending.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-04")]
    public partial class Slice04BlockedStalenessTest
    {
        // @driving_port @us-04 (threshold round-trips)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-04 (blockedStalenessThresholdDays round-trip)")]
        public async Task The_blocked_staleness_threshold_is_saved_and_read_back()
        {
            var team = GivenATeam();

            var save = await WhenTheAdminSetsTheBlockedStalenessThreshold(team, 10);
            ThenTheThresholdSaveSucceeds(save);

            var settings = await WhenTheThresholdIsRead(team);
            ThenTheThresholdIs(settings, 10);
        }

        // @edge @us-04 (0 = disabled, and is the default)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-04 (default threshold 0 = blocked-staleness disabled)")]
        public async Task A_new_team_defaults_the_blocked_staleness_threshold_to_zero()
        {
            var team = GivenATeam();

            var settings = await WhenTheThresholdIsRead(team);

            ThenTheThresholdIs(settings, 0);
        }

        // @error @us-04 (below range)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-04 (range validation, below range)")]
        public async Task A_blocked_staleness_threshold_below_range_is_rejected()
        {
            var team = GivenATeam();

            var save = await WhenTheAdminSetsTheBlockedStalenessThreshold(team, -1);

            ThenTheThresholdSaveIsRejected(save);
        }

        // @error @us-04 (above range)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-04 (range validation, above range)")]
        public async Task A_blocked_staleness_threshold_above_range_is_rejected()
        {
            var team = GivenATeam();

            var save = await WhenTheAdminSetsTheBlockedStalenessThreshold(team, 366);

            ThenTheThresholdSaveIsRejected(save);
        }

        // @error @us-04 @rbac (config-admin gate; GREEN-when-enabled via pre-existing TeamWrite guard)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-04 (RBAC gate on threshold write; GREEN-when-enabled)")]
        public async Task A_non_admin_cannot_change_the_blocked_staleness_threshold()
        {
            var team = GivenATeam();

            var save = await WhenANonAdminSetsTheBlockedStalenessThreshold(team, 10);

            ThenTheThresholdSaveIsForbidden(save);
        }
    }
}
