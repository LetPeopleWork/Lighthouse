using Lighthouse.Backend.Models.Authorization;
using static Lighthouse.Backend.Tests.Acceptance.SystemWideAccess.SystemWideAccessScenario;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A System Admin gives one SSO group Viewer on every Team and Portfolio with a single rule. Its members read every
    /// unit, including ones that appear after the rule, and change none; anyone outside the group gains nothing; the rule
    /// is listed once and removing it ends the reach on the next request.
    /// </summary>
    [TestFixture]
    public class ViewerOnAllAtThePortTest
    {
        private const string SystemAdminsGroup = "lh-system-admins";

        private const string DuplicateMessage = "This group already has this role here.";

        private SystemWideAccessScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new SystemWideAccessScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [Test]
        public async Task A_System_Admin_adds_Viewer_on_all_for_a_group_and_the_rules_list_shows_it_once()
        {
            var added = await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            Assert.That(added.Succeeded, Is.True, $"WHAT: adding Viewer on all for {TeamReaders} was refused ({added.Message}). WHY: a System Admin gives a group read access to every Team and Portfolio with this one rule. HOW: accept Viewer with the AllUnits scope and no unit.");
            var lines = await scenario.TheRulesList();
            Assert.That(
                lines.Select(line => (line.GroupValue, line.Role, line.ScopeType, line.ScopeId)),
                Is.EqualTo(new[] { (TeamReaders, UserRole.Viewer, AllUnits, (int?)null) }),
                "WHAT: the rules list does not show exactly one line Viewer on all for team-readers. WHY: the System Admin must see the rule they added, once. HOW: store the rule as a group mapping on AllUnits and list it.");
        }

        [Test]
        public async Task A_member_of_the_group_reads_every_Team_and_Portfolio_including_one_created_after_the_rule()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var readsZenith = await scenario.CanReadTeam(Noor_SignedIn(), TeamZenith);
            var readsMeridian = await scenario.CanReadTeam(Noor_SignedIn(), TeamMeridianCreatedLater);
            var readsOceanExplorer = await scenario.CanReadPortfolio(Noor_SignedIn(), PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readsZenith, Is.True, "WHAT: a member of team-readers, with no role on any unit, cannot read Team Zenith. WHY: Viewer on all reaches every Team. HOW: let the read check ask whether a rule of the person's groups reaches the unit.");
                Assert.That(readsMeridian, Is.True, "WHAT: the member cannot read a Team that appeared after the rule. WHY: the rule names no unit, so later units are covered with no further grant. HOW: decide reach from the rule's scope, never from a list of units copied when it was added.");
                Assert.That(readsOceanExplorer, Is.True, "WHAT: the member cannot read the Portfolio. WHY: Viewer on all reaches every Portfolio too. HOW: let the Portfolio read check use the same reach.");
            }
        }

        [Test]
        public async Task A_members_lists_hold_every_Team_and_Portfolio_asked_about()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var teams = await scenario.TheTeamsListed(Noor_SignedIn(), TeamZenith, TeamApollo, TeamMeridianCreatedLater);
            var portfolios = await scenario.ThePortfoliosListed(Noor_SignedIn(), PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teams, Is.EquivalentTo(new[] { TeamZenith, TeamApollo, TeamMeridianCreatedLater }), "WHAT: the member's Team list misses Teams. WHY: the Overview lists what the person may read, and Viewer on all reaches every Team. HOW: filter the list through the same reach as the single read check.");
                Assert.That(portfolios, Is.EquivalentTo(new[] { PortfolioOceanExplorer }), "WHAT: the member's Portfolio list misses the Portfolio. WHY: Viewer on all reaches every Portfolio. HOW: filter the Portfolio list through the reach.");
            }
        }

        [Test]
        public async Task A_member_reading_through_the_rule_changes_nothing_and_does_not_administer_access()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var writesTeam = await scenario.CanWriteTeam(Noor_SignedIn(), TeamZenith);
            var writesPortfolio = await scenario.CanWritePortfolio(Noor_SignedIn(), PortfolioOceanExplorer);
            var managesAccess = await scenario.CanManageAccess(Noor_SignedIn());
            var summary = await scenario.TheSummaryOf(Noor_SignedIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writesTeam, Is.False, "WHAT: Viewer on all let the member change a Team. WHY: Viewer reads only. HOW: a Viewer reach never answers a write check.");
                Assert.That(writesPortfolio, Is.False, "WHAT: Viewer on all let the member change a Portfolio. WHY: Viewer reads only. HOW: a Viewer reach never answers a write check.");
                Assert.That(managesAccess, Is.False, "WHAT: Viewer on all made the member a System Admin. WHY: a rule never changes the System role. HOW: resolve the System role from the System row alone.");
                Assert.That(summary.IsSystemAdmin, Is.False, "WHAT: the member's summary says System Admin. WHY: the browser would show admin pages. HOW: keep the summary's System role apart from rules.");
                Assert.That(summary.AdminTeamIds.Concat(summary.AdminPortfolioIds), Is.Empty, "WHAT: the member's summary lists units they administer. WHY: a Viewer reach administers nothing. HOW: fill the admin lists only from admin roles.");
            }
        }

        [Test]
        public async Task Someone_outside_the_group_gains_nothing_from_the_rule()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var readsZenith = await scenario.CanReadTeam(Sam_SignedIn(), TeamZenith);
            var teams = await scenario.TheTeamsListed(Sam_SignedIn(), TeamZenith, TeamApollo);
            var readsOceanExplorer = await scenario.CanReadPortfolio(Sam_SignedIn(), PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readsZenith, Is.False, "WHAT: a person in finance-staff only reads Team Zenith. WHY: the rule reaches members of team-readers alone. HOW: apply a rule only for groups the person carries.");
                Assert.That(teams, Is.Empty, "WHAT: the outsider's Team list is not empty. WHY: they hold no role and are in no ruled group. HOW: apply a rule only for the person's groups.");
                Assert.That(readsOceanExplorer, Is.False, "WHAT: the outsider reads the Portfolio. WHY: the rule is not theirs. HOW: apply a rule only for the person's groups.");
            }
        }

        [Test]
        public async Task A_members_unlimited_key_reads_what_the_member_reads()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);
            var unlimitedKey = scenario.AKeyIsStoredFor(Noor);

            var readsZenith = await scenario.CanReadTeam(TheKey(Noor, unlimitedKey), TeamZenith);
            var readsOceanExplorer = await scenario.CanReadPortfolio(TheKey(Noor, unlimitedKey), PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readsZenith, Is.True, "WHAT: the member's key without a limit cannot read Team Zenith. WHY: a key without scopes carries all of its owner's rights, and the owner reads through the rule by the groups stored at her last sign-in. HOW: build the key's owner access with the rules of the stored groups.");
                Assert.That(readsOceanExplorer, Is.True, "WHAT: the member's key without a limit cannot read the Portfolio. WHY: it reads what its owner reads. HOW: as above.");
            }
        }

        [Test]
        public async Task Removing_the_rule_ends_the_members_reach_on_the_next_request()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);
            var readBeforeRemoval = await scenario.CanReadTeam(Noor_SignedIn(), TeamZenith);

            await scenario.AlexRemovesTheRulesOf(TeamReaders);

            var readAfterRemoval = await scenario.CanReadTeam(Noor_SignedIn(), TeamZenith);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readBeforeRemoval, Is.True, "WHAT: the member could not read Team Zenith while the rule existed. WHY: without the reach first, the removal proves nothing. HOW: make Viewer on all reach every Team.");
                Assert.That(readAfterRemoval, Is.False, "WHAT: the member still reads Team Zenith after the rule was removed. WHY: removing a rule must end what it gave on the next request. HOW: read rules on every request, keep no cache.");
            }
        }

        [Test]
        public async Task Removing_a_System_Admin_group_stored_twice_removes_both_and_the_list_shows_it_once()
        {
            scenario.TwoEqualRowsWereStored(SystemAdminsGroup, UserRole.SystemAdmin, PermissionScopeType.System, null);

            await TheTwinsAreListedOnceAndRemovedTogether(SystemAdminsGroup);
        }

        [Test]
        public async Task Removing_a_Viewer_on_all_rule_stored_twice_removes_both_and_the_list_shows_it_once()
        {
            scenario.TwoEqualRowsWereStored(TeamReaders, UserRole.Viewer, AllUnits, null);

            await TheTwinsAreListedOnceAndRemovedTogether(TeamReaders);
        }

        [Test]
        public async Task Adding_a_rule_that_already_exists_is_refused_with_the_duplicate_message()
        {
            await scenario.AlexAddsAMapping(TeamReaders, UserRole.Viewer, PermissionScopeType.Team, TeamZenith);

            var addedAgain = await scenario.AlexAddsAMapping(TeamReaders, UserRole.Viewer, PermissionScopeType.Team, TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(addedAgain.Succeeded, Is.False, "WHAT: adding a rule equal to an existing one was accepted. WHY: the System Admin must learn the group already has that role there. HOW: refuse an equal row on create.");
                Assert.That(addedAgain.Message, Is.EqualTo(DuplicateMessage), "WHAT: the refusal does not say the group already has the role. WHY: this is the message the page shows. HOW: answer the duplicate message.");
                Assert.That(scenario.StoredRowsFor(TeamReaders), Is.EqualTo(1), "WHAT: a refused add stored a row. WHY: a refusal writes nothing. HOW: check for the equal row before saving.");
            }
        }

        [Test]
        public async Task Changing_a_role_onto_an_equal_rule_that_already_exists_is_refused()
        {
            await scenario.AlexAddsAMapping(TeamReaders, UserRole.Viewer, PermissionScopeType.Team, TeamZenith);
            await scenario.AlexAddsAMapping(TeamReaders, UserRole.TeamAdmin, PermissionScopeType.Team, TeamZenith);
            var adminLine = (await scenario.TheRulesList()).Single(line => line.Role == UserRole.TeamAdmin);

            var changed = await scenario.AlexChangesTheRole(adminLine.Id, UserRole.Viewer);

            var linesAfter = await scenario.TheRulesList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Succeeded, Is.False, "WHAT: changing a role onto an equal existing line was accepted. WHY: it would store two equal rules, which then show and remove as one. HOW: refuse the change with the duplicate message.");
                Assert.That(changed.Message, Is.EqualTo(DuplicateMessage), "WHAT: the refusal does not say the group already has the role. WHY: this is the message the page shows. HOW: answer the duplicate message.");
                Assert.That(
                    linesAfter.Select(line => line.Role),
                    Is.EquivalentTo(new[] { UserRole.Viewer, UserRole.TeamAdmin }),
                    "WHAT: a refused change still altered the stored lines. WHY: a refusal writes nothing. HOW: check for the equal line before saving.");
            }
        }

        [Test]
        public async Task Saving_a_rule_with_the_role_it_already_has_is_accepted_and_changes_nothing()
        {
            await scenario.AlexAddsAMapping(TeamReaders, UserRole.Viewer, PermissionScopeType.Team, TeamZenith);
            var line = (await scenario.TheRulesList()).Single();

            var saved = await scenario.AlexChangesTheRole(line.Id, UserRole.Viewer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.Succeeded, Is.True, $"WHAT: saving a rule with its own role was refused ({saved.Message}). WHY: the rule is not a duplicate of itself, and the page saves a row whose role was left as it was. HOW: look for an equal rule only when the role changes.");
                Assert.That(scenario.StoredRowsFor(TeamReaders), Is.EqualTo(1), "WHAT: saving a rule with its own role changed the stored rows. WHY: nothing changed. HOW: as above.");
            }
        }

        [Test]
        public async Task The_rules_list_is_ordered_by_group_then_scope_then_unit_then_role()
        {
            await scenario.AlexAddsAMapping("delivery-leads", UserRole.Viewer, AllUnits, null);
            await scenario.AlexAddsAMapping("delivery-leads", UserRole.Viewer, PermissionScopeType.Team, TeamApollo);
            await scenario.AlexAddsAMapping("delivery-leads", UserRole.Viewer, PermissionScopeType.Team, TeamZenith);
            await scenario.AlexAddsAMapping("delivery-leads", UserRole.TeamAdmin, PermissionScopeType.Team, TeamZenith);
            await scenario.AlexAddsAMapping("agile-coaches", UserRole.Viewer, AllUnits, null);

            var lines = await scenario.TheRulesList();

            Assert.That(
                lines.Select(line => (line.GroupValue, line.Role, line.ScopeType, line.ScopeId)),
                Is.EqualTo(new[]
                {
                    ("agile-coaches", UserRole.Viewer, AllUnits, (int?)null),
                    ("delivery-leads", UserRole.TeamAdmin, PermissionScopeType.Team, (int?)TeamZenith),
                    ("delivery-leads", UserRole.Viewer, PermissionScopeType.Team, (int?)TeamZenith),
                    ("delivery-leads", UserRole.Viewer, PermissionScopeType.Team, (int?)TeamApollo),
                    ("delivery-leads", UserRole.Viewer, AllUnits, (int?)null),
                }),
                "WHAT: the rules list is not in a stable order. WHY: the System Admin finds a group's rules together and the list does not jump between reloads. HOW: order by group, then scope, then unit, then role.");
        }

        [Test]
        public async Task Of_a_rule_stored_twice_the_list_shows_the_row_stored_first()
        {
            var storedAs = scenario.TwoEqualRowsWereStored(TeamReaders, UserRole.Viewer, AllUnits, null);

            var line = (await scenario.TheRulesList()).Single(listed => listed.GroupValue == TeamReaders);

            Assert.That(line.Id, Is.EqualTo(storedAs[0]), "WHAT: the list shows the twin stored later. WHY: the line a System Admin sees must not change from one reload to the next. HOW: keep the first stored row of equal rows.");
        }

        private async Task TheTwinsAreListedOnceAndRemovedTogether(string group)
        {
            var lines = (await scenario.TheRulesList()).Where(line => line.GroupValue == group).ToList();
            Assert.That(lines, Has.Count.EqualTo(1), "WHAT: a rule stored twice is listed twice. WHY: the System Admin sees one rule, one line, and one Remove that removes it. HOW: list one line per group, role and scope.");

            await scenario.AlexRemovesOneLine(lines[0].Id);

            Assert.That(scenario.StoredRowsFor(group), Is.Zero, "WHAT: a twin of the removed rule is still stored. WHY: it would keep granting its role after the System Admin removed the rule. HOW: on removal delete every row with the same group, role and scope.");
        }

        /// <summary>A mapping is accepted for exactly the valid pairs of role, scope and unit; every role and scope the server knows is tried, so a value added later fails here until it is placed.</summary>
        [TestCaseSource(nameof(EveryRoleScopeAndUnit))]
        public async Task A_rule_is_accepted_for_exactly_the_valid_pairs_of_role_and_scope(UserRole role, PermissionScopeType scope, int? unit)
        {
            var result = await scenario.AlexAddsAMapping("any-group", role, scope, unit);

            var valid = IsAValidPair(role, scope, unit);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.EqualTo(valid), $"WHAT: {role} on {scope} with unit {unit?.ToString() ?? "none"} was {(result.Succeeded ? "accepted" : "refused")}. WHY: only the valid pairs may become a rule. HOW: keep the valid-pairs table in step with the roles and scopes.");
                Assert.That(scenario.StoredRowsFor("any-group"), Is.EqualTo(valid ? 1 : 0), "WHAT: the stored rows do not match the answer. WHY: a refusal writes no row. HOW: refuse before saving.");
            }
        }

        private static IEnumerable<TestCaseData> EveryRoleScopeAndUnit()
            => from role in Enum.GetValues<UserRole>()
               from scope in Enum.GetValues<PermissionScopeType>()
               from unit in new int?[] { null, TeamZenith }
               select new TestCaseData(role, scope, unit);

        private static bool IsAValidPair(UserRole role, PermissionScopeType scope, int? unit)
            => (role, scope, unit.HasValue) switch
            {
                (UserRole.SystemAdmin, PermissionScopeType.System, false) => true,
                (UserRole.TeamAdmin, PermissionScopeType.Team, true) => true,
                (UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, true) => true,
                (UserRole.Viewer, PermissionScopeType.Team, true) => true,
                (UserRole.Viewer, PermissionScopeType.Portfolio, true) => true,
                (UserRole.Viewer, PermissionScopeType.AllUnits, false) => true,
                (UserRole.UnitAdmin, PermissionScopeType.AllUnits, false) => true,
                _ => false,
            };
    }
}
