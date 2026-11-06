using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// A client that only knows the old single field must not leave the list behind, and must not flatten a list
    /// it cannot see. What it sends back unchanged is an echo and leaves the list alone; anything else is a choice,
    /// and the list becomes what the upgrade would have made of that field.
    /// </summary>
    [TestFixture]
    public class ParentSourcesBridgeTest
    {
        private const int A = 11;

        private const int B = 12;

        private const int C = 13;

        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static readonly ParentSourceEntry[] OnlyTheOwnParent = [OwnParent];

        private static ParentSources ListOf(params ParentSourceEntry[] entries) => ParentSources.Of(entries).List!;

        private static ParentSources ARicherListStartingWith(int field) => ListOf(ParentSourceEntry.Field(field), ParentSourceEntry.Field(B), OwnParent);

        private static ParentSourceEntry[] TheFieldThenTheOwnParent(int field) => [ParentSourceEntry.Field(field), OwnParent];

        public sealed record OldFieldWay(string Name, Action<Team, int?> Give)
        {
            public override string ToString() => Name;
        }

        private static readonly OldFieldWay Owning = new("Owning the old field", (team, field) => team.OwnParentOverride(field));

        private static readonly OldFieldWay SentAsItsOwn = new("Sent with the source Own", (team, field) => team.TakeParentOverrideAsSent(SettingSource.Own, field));

        private static readonly OldFieldWay SentWithoutASource = new("Sent without a source", (team, field) => team.TakeParentOverrideAsSent(null, field));

        private static IEnumerable<OldFieldWay> WaysToGiveAField() => [Owning, SentAsItsOwn, SentWithoutASource];

        // A client that sends no source and no field has chosen nothing, so that way is not one of these.
        private static IEnumerable<OldFieldWay> WaysToGiveNoField() => [Owning, SentAsItsOwn];

        [TestCaseSource(nameof(WaysToGiveAField))]
        public void An_owner_given_the_old_field_lists_it_then_the_own_Parent(OldFieldWay way)
        {
            var team = new Team();

            way.Give(team, A);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ParentSources?.Entries, Is.EqualTo(TheFieldThenTheOwnParent(A)).AsCollection);
                Assert.That(team.TheEffectiveParentSources().Entries, Is.EqualTo(TheFieldThenTheOwnParent(A)).AsCollection);
                Assert.That(team.ParentOverrideSource, Is.EqualTo(SettingSource.Own));
                Assert.That(team.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(A));
            }
        }

        [TestCaseSource(nameof(WaysToGiveNoField))]
        public void An_owner_given_no_field_lists_the_own_Parent_alone(OldFieldWay way)
        {
            var aFollower = new Team();
            var anOwnerOfARicherList = new Team();
            anOwnerOfARicherList.OwnParentSources(ARicherListStartingWith(A));

            way.Give(aFollower, null);
            way.Give(anOwnerOfARicherList, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aFollower.ParentSources?.Entries, Is.EqualTo(OnlyTheOwnParent).AsCollection, "None is a choice of its own, not the absence of a list.");
                Assert.That(aFollower.ParentOverrideSource, Is.EqualTo(SettingSource.Own));
                Assert.That(anOwnerOfARicherList.ParentSources?.Entries, Is.EqualTo(OnlyTheOwnParent).AsCollection);
                Assert.That(anOwnerOfARicherList.OwnParentOverrideAdditionalFieldDefinitionId, Is.Null);
            }
        }

        [TestCaseSource(nameof(WaysToGiveAField))]
        public void An_echo_of_the_projection_leaves_a_richer_list_alone(OldFieldWay way)
        {
            var richerList = ARicherListStartingWith(A);
            var team = new Team();
            team.OwnParentSources(richerList);

            way.Give(team, richerList.ToTheLegacyField());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ParentSources, Is.SameAs(richerList));
                Assert.That(team.ParentOverrideSource, Is.EqualTo(SettingSource.Own));
            }
        }

        [TestCaseSource(nameof(WaysToGiveAField))]
        public void A_changed_old_field_replaces_a_richer_list(OldFieldWay way)
        {
            var team = new Team();
            team.OwnParentSources(ARicherListStartingWith(A));

            way.Give(team, C);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ParentSources?.Entries, Is.EqualTo(TheFieldThenTheOwnParent(C)).AsCollection);
                Assert.That(team.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(C));
            }
        }

        public sealed record FollowWay(string Name, Action<Team> Follow)
        {
            public override string ToString() => Name;
        }

        private static IEnumerable<FollowWay> WaysToFollowTheDefault()
        {
            yield return new("Following through the old member", team => team.FollowParentOverrideDefault());
            yield return new("Sent with the source Connection Default", team => team.TakeParentOverrideAsSent(SettingSource.ConnectionDefault, A));
        }

        [TestCaseSource(nameof(WaysToFollowTheDefault))]
        public void An_owner_that_follows_again_keeps_no_list_of_its_own(FollowWay way)
        {
            var theDefault = ListOf(ParentSourceEntry.Field(C), OwnParent);
            var connection = new WorkTrackingSystemConnection();
            connection.SetParentSourcesDefaultForTeams(theDefault);
            var team = new Team { WorkTrackingSystemConnection = connection };
            team.OwnParentSources(ARicherListStartingWith(A));

            way.Follow(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.FollowsParentOverrideDefault, Is.True);
                Assert.That(team.ParentSources, Is.Null, "A list left behind would come back the moment the owner chooses its own again.");
                Assert.That(team.TheEffectiveParentSources(), Is.SameAs(theDefault));
            }
        }

        private static ConnectionFieldDefault TheRegistryRow(ConnectionDefaultSetting setting)
            => ConnectionFieldDefaults.All.Single(row => row.Setting == setting);

        [Test]
        public void A_single_default_saved_through_the_Teams_registry_row_moves_the_Teams_followers_effective_list()
        {
            var connection = new WorkTrackingSystemConnection();
            var followingTeam = new Team { WorkTrackingSystemConnection = connection };
            var followingPortfolio = new Portfolio { WorkTrackingSystemConnection = connection };

            TheRegistryRow(ConnectionDefaultSetting.ParentOverrideForTeams).PointAt(connection, A);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(followingTeam.TheEffectiveParentSources().Entries, Is.EqualTo(TheFieldThenTheOwnParent(A)).AsCollection);
                Assert.That(followingPortfolio.TheEffectiveParentSources().Entries, Is.EqualTo(OnlyTheOwnParent).AsCollection, "The Portfolios hold a default of their own.");
            }
        }

        [Test]
        public void A_single_default_saved_through_the_Portfolios_registry_row_moves_the_Portfolio_followers_effective_list()
        {
            var connection = new WorkTrackingSystemConnection();
            var followingTeam = new Team { WorkTrackingSystemConnection = connection };
            var followingPortfolio = new Portfolio { WorkTrackingSystemConnection = connection };

            TheRegistryRow(ConnectionDefaultSetting.ParentOverrideForPortfolios).PointAt(connection, A);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(followingPortfolio.TheEffectiveParentSources().Entries, Is.EqualTo(TheFieldThenTheOwnParent(A)).AsCollection);
                Assert.That(followingTeam.TheEffectiveParentSources().Entries, Is.EqualTo(OnlyTheOwnParent).AsCollection, "The Teams hold a default of their own.");
            }
        }

        [Test]
        public void A_single_default_set_on_the_old_connection_member_moves_the_followers_effective_list()
        {
            var connection = new WorkTrackingSystemConnection();
            var followingTeam = new Team { WorkTrackingSystemConnection = connection };
            var followingPortfolio = new Portfolio { WorkTrackingSystemConnection = connection };

            connection.ParentOverrideForTeams = A;
            connection.ParentOverrideForPortfolios = C;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(followingTeam.TheEffectiveParentSources().Entries, Is.EqualTo(TheFieldThenTheOwnParent(A)).AsCollection);
                Assert.That(followingPortfolio.TheEffectiveParentSources().Entries, Is.EqualTo(TheFieldThenTheOwnParent(C)).AsCollection);
            }
        }

        public sealed record DefaultOfAKind(
            string Name,
            ConnectionDefaultSetting Setting,
            Action<WorkTrackingSystemConnection, ParentSources?> Hold,
            Func<WorkTrackingSystemConnection, ParentSources?> Held)
        {
            public override string ToString() => Name;
        }

        private static IEnumerable<DefaultOfAKind> BothDefaults()
        {
            yield return new("The Teams' default", ConnectionDefaultSetting.ParentOverrideForTeams,
                (connection, list) => connection.SetParentSourcesDefaultForTeams(list), connection => connection.ParentSourcesForTeams);
            yield return new("The Portfolios' default", ConnectionDefaultSetting.ParentOverrideForPortfolios,
                (connection, list) => connection.SetParentSourcesDefaultForPortfolios(list), connection => connection.ParentSourcesForPortfolios);
        }

        [TestCaseSource(nameof(BothDefaults))]
        public void An_echo_of_the_defaults_projection_leaves_a_richer_default_alone(DefaultOfAKind kind)
        {
            var richerList = ARicherListStartingWith(A);
            var theOwnParentAlone = ListOf(OwnParent);
            var holdingARicherList = new WorkTrackingSystemConnection();
            kind.Hold(holdingARicherList, richerList);
            var holdingTheOwnParentAlone = new WorkTrackingSystemConnection();
            kind.Hold(holdingTheOwnParentAlone, theOwnParentAlone);

            TheRegistryRow(kind.Setting).PointAt(holdingARicherList, A);
            TheRegistryRow(kind.Setting).PointAt(holdingTheOwnParentAlone, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kind.Held(holdingARicherList), Is.SameAs(richerList));
                Assert.That(kind.Held(holdingTheOwnParentAlone), Is.SameAs(theOwnParentAlone));
            }
        }

        [TestCaseSource(nameof(BothDefaults))]
        public void A_changed_single_default_replaces_a_richer_default(DefaultOfAKind kind)
        {
            var connection = new WorkTrackingSystemConnection();
            kind.Hold(connection, ARicherListStartingWith(A));

            TheRegistryRow(kind.Setting).PointAt(connection, C);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kind.Held(connection)?.Entries, Is.EqualTo(TheFieldThenTheOwnParent(C)).AsCollection);
                Assert.That(TheRegistryRow(kind.Setting).FieldsOn(connection), Is.EqualTo(new[] { C }).AsCollection);
            }
        }

        [TestCaseSource(nameof(BothDefaults))]
        public void A_single_default_cleared_removes_the_default_list(DefaultOfAKind kind)
        {
            var connection = new WorkTrackingSystemConnection();
            kind.Hold(connection, ARicherListStartingWith(A));

            TheRegistryRow(kind.Setting).PointAt(connection, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kind.Held(connection), Is.Null, "A connection that names no default holds no list, as the upgrade leaves it.");
                Assert.That(TheRegistryRow(kind.Setting).FieldsOn(connection), Is.Empty);
            }
        }

        [Test]
        public void Holding_a_default_list_keeps_the_old_members_on_its_projection_in_both_kinds()
        {
            var connection = new WorkTrackingSystemConnection();
            var richerList = ARicherListStartingWith(A);

            connection.SetParentSourcesDefaultForTeams(richerList);
            connection.SetParentSourcesDefaultForPortfolios(richerList);
            connection.SetParentSourcesDefaultForTeams(null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.ParentSourcesForTeams, Is.Null);
                Assert.That(connection.ParentOverrideForTeams, Is.Null);
                Assert.That(connection.ParentSourcesForPortfolios, Is.SameAs(richerList));
                Assert.That(connection.ParentOverrideForPortfolios, Is.EqualTo(A));
            }
        }
    }
}
