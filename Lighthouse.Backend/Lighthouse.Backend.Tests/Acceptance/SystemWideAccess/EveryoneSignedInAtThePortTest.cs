using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using static Lighthouse.Backend.Tests.Acceptance.SystemWideAccess.EveryoneSignedInScenario;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A System Admin turns on "everyone signed in can view all Teams and Portfolios". It is off after install, and
    /// only a System Admin changes it, after reading it, on the token that read gave. While it is on, every signed-in
    /// person who has a profile, and their keys, reads every Team and Portfolio and writes nothing it did not already
    /// write; someone with no profile reads nothing. When the switch cannot be read, a System Admin still manages
    /// access and a read of a unit is an error, never a grant. Turning it off ends the reach on the next request.
    /// </summary>
    [TestFixture]
    public class EveryoneSignedInAtThePortTest
    {
        private EveryoneSignedInScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new EveryoneSignedInScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [Test]
        public async Task After_install_the_switch_is_off_with_a_token_and_a_person_with_no_role_reads_nothing()
        {
            await scenario.TheInstanceHasStarted();

            var theSwitch = await scenario.TheSwitchAsReadByAlex();
            var readsZenith = await scenario.CanReadTeam(Morgan_SignedIn(), TeamZenith);
            var readsOceanExplorer = await scenario.CanReadPortfolio(Morgan_SignedIn(), PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theSwitch.On, Is.False, "WHAT: the switch reads on after install. WHY: an instance that keeps Teams apart must not open them to everyone by upgrading. HOW: let the start-up seeder store the switch off when it is missing.");
                Assert.That(theSwitch.Token, Is.Not.EqualTo(Guid.Empty), "WHAT: the switch was read with an empty token. WHY: a save is judged against the token it was read with, so two System Admins cannot overwrite each other unseen. HOW: answer the stored row's concurrency token.");
                Assert.That(readsZenith, Is.False, "WHAT: Morgan, with a profile and no role or group, reads Team Zenith while the switch is off. WHY: off must change nothing for anyone. HOW: add the switch's Viewer reach only when the stored switch is on.");
                Assert.That(readsOceanExplorer, Is.False, "WHAT: Morgan reads the Portfolio while the switch is off. WHY: off must change nothing for anyone. HOW: add the switch's Viewer reach only when the stored switch is on.");
            }
        }

        /// <summary>L10: the switch adds Viewer reach only, only for a resolved profile, and an absent row equals off.</summary>
        [TestCase(Someone.Morgan, SwitchAtStart.On, true, false, true, false, TestName = "L10_on_a_person_with_a_profile_and_no_group_reads_every_Team_and_writes_none")]
        [TestCase(Someone.MorgansKey, SwitchAtStart.On, true, false, true, false, TestName = "L10_on_a_key_reads_as_its_owner_and_writes_nothing")]
        [TestCase(Someone.Stranger, SwitchAtStart.On, false, false, false, false, TestName = "L10_on_a_person_with_no_profile_reads_nothing")]
        [TestCase(Someone.StrangersKey, SwitchAtStart.On, false, false, false, false, TestName = "L10_on_a_key_whose_owner_has_no_profile_reads_nothing")]
        [TestCase(Someone.Morgan, SwitchAtStart.Off, false, false, false, false, TestName = "L10_off_a_person_with_no_role_reads_nothing")]
        [TestCase(Someone.Morgan, SwitchAtStart.NoRowStored, false, false, false, false, TestName = "L10_with_no_switch_stored_it_reads_as_off")]
        [TestCase(Someone.Jordan, SwitchAtStart.On, true, true, true, false, TestName = "L10_on_a_Team_Admin_keeps_admin_on_their_Team_and_only_reads_another")]
        [TestCase(Someone.Jordan, SwitchAtStart.Off, true, true, false, false, TestName = "L10_off_a_Team_Admin_keeps_admin_on_their_Team_and_reads_no_other")]
        public async Task L10_the_switch_adds_Viewer_reach_only_for_a_resolved_profile(
            Someone who, SwitchAtStart switchAtStart, bool readsZenith, bool writesZenith, bool readsApollo, bool writesApollo)
        {
            await scenario.TheSwitchStartsAs(switchAtStart);

            var answers = await scenario.WhatTheyMayDoOnTwoTeams(who);

            Assert.That(
                answers,
                Is.EqualTo(new ReachOnTwoTeams(readsZenith, writesZenith, readsApollo, writesApollo)),
                $"WHAT: {who} with the switch {switchAtStart} may do {answers} on Team Zenith and Team Apollo. WHY: the switch adds reading on every unit for a person with a profile and nothing else; a missing switch reads as off; a real role stays as it is. HOW: after the profile is resolved, add a Viewer reach on all units when the stored switch is on, merged so the higher role wins.");
        }

        [Test]
        public async Task With_the_switch_on_a_person_with_no_group_reads_every_Team_and_Portfolio_including_a_later_one_and_changes_none()
        {
            await scenario.TheInstanceHasStarted();
            await scenario.AlexHasTurnedTheSwitchOn();

            var readsOceanExplorer = await scenario.CanReadPortfolio(Morgan_SignedIn(), PortfolioOceanExplorer);
            var readsLaterTeam = await scenario.CanReadTeam(Morgan_SignedIn(), TeamMeridianCreatedLater);
            var teams = await scenario.TheTeamsListed(Morgan_SignedIn());
            var portfolios = await scenario.ThePortfoliosListed(Morgan_SignedIn());
            var writesOceanExplorer = await scenario.CanWritePortfolio(Morgan_SignedIn(), PortfolioOceanExplorer);
            var managesAccess = await scenario.CanManageAccess(Morgan_SignedIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readsOceanExplorer, Is.True, "WHAT: Morgan cannot read the Portfolio with the switch on. WHY: the switch reaches every Portfolio too. HOW: let the Portfolio read check use the same Viewer reach.");
                Assert.That(readsLaterTeam, Is.True, "WHAT: Morgan cannot read a Team that appeared after the switch was turned on. WHY: the switch names no unit, so later units are covered. HOW: decide the reach from the switch, never from a list of units.");
                Assert.That(teams, Is.EquivalentTo(EveryTeamAskedAbout), "WHAT: Morgan's Team list misses Teams. WHY: a newcomer's Overview lists every Team on day one. HOW: filter the list through the same reach as the single read check.");
                Assert.That(portfolios, Is.EquivalentTo(EveryPortfolioAskedAbout), "WHAT: Morgan's Portfolio list misses the Portfolio. WHY: the Overview lists every Portfolio too. HOW: filter the Portfolio list through the reach.");
                Assert.That(writesOceanExplorer, Is.False, "WHAT: Morgan may change the Portfolio through the switch. WHY: the switch can only give reading. HOW: give the switch the Viewer role and nothing above it.");
                Assert.That(managesAccess, Is.False, "WHAT: Morgan may manage access through the switch. WHY: only a System Admin manages access, and the switch never makes one. HOW: resolve the System role without the switch.");
            }
        }

        [Test]
        public async Task A_save_on_a_token_read_before_the_last_save_is_a_conflict_and_a_save_with_no_token_is_refused_both_leaving_the_switch_as_it_was()
        {
            await scenario.TheInstanceHasStarted();
            var readBeforeTheLastSave = await scenario.TheSwitchAsReadByAlex();
            await scenario.AlexHasTurnedTheSwitchOn();
            var stored = scenario.TheStoredSwitch();

            var staleSave = await scenario.WhatASaveAnswers(Alex_SignedIn(), false, readBeforeTheLastSave.Token);
            var saveWithNoToken = await scenario.WhatASaveAnswers(Alex_SignedIn(), false, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(staleSave.Conflict, Is.True, $"WHAT: turning the switch off on a token read before the last save answered {staleSave}. WHY: a System Admin who saves on an old reading would undo another's change unseen; the browser shows the conflict message from a 409. HOW: apply the token read as the original token, so the save throws DbUpdateConcurrencyException.");
                Assert.That(saveWithNoToken.Refused, Is.True, $"WHAT: a save with no token answered {saveWithNoToken}. WHY: this setting has required its token since it first shipped, so a save without one cannot say what it was based on. HOW: refuse a save whose token is missing, before writing.");
                Assert.That(saveWithNoToken.Detail, Is.EqualTo("The setting was sent without the token it was read with. Reload the page and try again."), "WHAT: the refusal of a save with no token does not tell the System Admin what to do. WHY: the page shows this message. HOW: answer the reload message.");
                Assert.That(scenario.TheStoredSwitch(), Is.EqualTo(stored), "WHAT: the stored switch changed after a refused save. WHY: a refused save must leave the setting as it was. HOW: write nothing when the token is missing or stale.");
            }
        }

        [Test]
        public async Task Only_a_System_Admin_may_turn_the_switch_and_a_refused_save_changes_nothing()
        {
            await scenario.TheInstanceHasStarted();
            var theSwitch = await scenario.TheSwitchAsReadByAlex();

            var byATeamAdmin = await scenario.WhatASaveAnswers(Jordan_SignedIn(), true, theSwitch.Token);
            var byAPersonWithNoRole = await scenario.WhatASaveAnswers(Morgan_SignedIn(), true, theSwitch.Token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(byATeamAdmin.Refused, Is.True, $"WHAT: a Team Admin's save answered {byATeamAdmin}. WHY: only a System Admin opens every Team and Portfolio to everyone. HOW: refuse the save unless the caller may manage access for the whole instance.");
                Assert.That(byATeamAdmin.Detail, Is.EqualTo("Only a System Admin changes whether everyone signed in can view all Teams and Portfolios."), "WHAT: the refusal does not say who may change the switch. WHY: a refusal says why. HOW: answer the System Admin message.");
                Assert.That(byAPersonWithNoRole.Refused, Is.True, $"WHAT: a save by a person with no role answered {byAPersonWithNoRole}. WHY: only a System Admin changes the switch. HOW: refuse the save unless the caller may manage access.");
                Assert.That(scenario.TheStoredSwitch().On, Is.False, "WHAT: the switch is on after only refused saves. WHY: a refused save changes nothing. HOW: check the caller before writing.");
            }
        }

        /// <summary>L13: the System role never reads the switch, so a failing switch read leaves System Admin true and makes unit reads errors.</summary>
        [Test]
        public async Task L13_when_the_switch_cannot_be_read_a_System_Admin_still_manages_access_and_a_read_of_a_unit_is_an_error_not_a_grant()
        {
            await scenario.TheInstanceHasStarted();
            await scenario.AlexHasTurnedTheSwitchOn();
            scenario.TheSwitchCannotBeRead();

            var alexManagesAccess = await scenario.CanManageAccess(Alex_SignedIn());
            var readOfTheTeam = await scenario.WhatAReadAnswers(() => scenario.CanReadTeam(Morgan_SignedIn(), TeamZenith));
            var readOfThePortfolio = await scenario.WhatAReadAnswers(() => scenario.CanReadPortfolio(Morgan_SignedIn(), PortfolioOceanExplorer));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alexManagesAccess, Is.True, "WHAT: Alex lost System Admin while the switch could not be read. WHY: a System Admin must be able to manage access to repair the instance whatever else fails. HOW: resolve the System role from the System grant alone, without reading the switch.");
                Assert.That(readOfTheTeam, Is.EqualTo(ReadAnswer.Error), $"WHAT: Morgan's read of Team Zenith answered {readOfTheTeam} while the switch could not be read. WHY: a failed read must never grant, and a silent refusal would show a newcomer an empty Overview as if nothing were shared. HOW: let the failed switch read end the request in an error.");
                Assert.That(readOfThePortfolio, Is.EqualTo(ReadAnswer.Error), $"WHAT: Morgan's read of the Portfolio answered {readOfThePortfolio} while the switch could not be read. WHY: a failed read is an error, never a grant. HOW: let the failed switch read end the request in an error.");
            }
        }

        [Test]
        public async Task Turning_the_switch_off_ends_the_reach_on_the_next_request_and_leaves_real_roles_as_they_were()
        {
            await scenario.TheInstanceHasStarted();
            await scenario.AlexHasTurnedTheSwitchOn();
            var readWhileOn = await scenario.CanReadTeam(Morgan_SignedIn(), TeamZenith);

            await scenario.AlexHasTurnedTheSwitchOff();

            var readAfterOff = await scenario.CanReadTeam(Morgan_SignedIn(), TeamZenith);
            var keyReadAfterOff = await scenario.CanReadTeam(scenario.MorgansKey(), TeamZenith);
            var jordanWritesZenith = await scenario.CanWriteTeam(Jordan_SignedIn(), TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readWhileOn, Is.True, "WHAT: Morgan could not read Team Zenith while the switch was on. WHY: this is the reach turning it off must end. HOW: add the switch's Viewer reach when it is on.");
                Assert.That(readAfterOff, Is.False, "WHAT: Morgan still reads Team Zenith after the switch was turned off. WHY: off ends the reach on the next request. HOW: read the stored switch on every request, with no cache.");
                Assert.That(keyReadAfterOff, Is.False, "WHAT: Morgan's key still reads Team Zenith after the switch was turned off. WHY: a key reads as its owner. HOW: give the key its owner's reach as resolved now.");
                Assert.That(jordanWritesZenith, Is.True, "WHAT: Jordan lost Team Admin on Team Zenith when the switch was turned off. WHY: turning the switch off leaves roles and mappings alone. HOW: change only the stored switch.");
            }
        }

        [Test]
        public async Task With_no_group_claim_configured_the_switch_still_reaches_a_signed_in_person()
        {
            using var withoutGroups = new EveryoneSignedInScenario(groupClaimName: string.Empty);
            await withoutGroups.TheInstanceHasStarted();
            await withoutGroups.AlexHasTurnedTheSwitchOn();

            var readsZenith = await withoutGroups.CanReadTeam(Morgan_SignedIn(), TeamZenith);

            Assert.That(readsZenith, Is.True, "WHAT: with no group claim configured, Morgan cannot read Team Zenith while the switch is on. WHY: the switch is for instances that need no group at all. HOW: read the switch after the profile is resolved, outside the path that stops when there are no group values.");
        }

        [Test]
        public async Task Should_a_second_switch_be_stored_the_one_stored_first_decides()
        {
            scenario.TheSwitchIsStored(on: false);
            scenario.TheSwitchIsStored(on: true);

            var theSwitch = await scenario.TheSwitchAsReadByAlex();
            var morganReadsZenith = await scenario.CanReadTeam(Morgan_SignedIn(), TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theSwitch.On, Is.False, "WHAT: with two switches stored, the one stored later was read. WHY: one switch decides, the same one every time, and it is the one the instance has always had. HOW: read the switch stored first.");
                Assert.That(morganReadsZenith, Is.False, "WHAT: a person with no role reads Team Zenith through the switch stored later. WHY: the read checks and the read of the switch must agree on which switch decides. HOW: as above.");
            }
        }

        [Test]
        public async Task A_units_rules_name_everyone_signed_in_as_Viewer_while_the_switch_is_on_and_not_after_it_is_off()
        {
            await scenario.TheInstanceHasStarted();
            await scenario.AlexHasTurnedTheSwitchOn();
            var teamWhileOn = await scenario.TheRulesReachingTeam(TeamZenith);
            var portfolioWhileOn = await scenario.TheRulesReachingPortfolio(PortfolioOceanExplorer);

            await scenario.AlexHasTurnedTheSwitchOff();

            var teamAfterOff = await scenario.TheRulesReachingTeam(TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamWhileOn, Is.EqualTo(new[] { (UserRole.Viewer, EveryoneSignedIn) }), "WHAT: Team Zenith's rules do not name everyone signed in as Viewer while the switch is on. WHY: a Team Admin must see on the Access tab that everyone signed in reads the Team. HOW: add a rule with the Viewer role and the holder \"everyone signed in\" when the stored switch is on.");
                Assert.That(portfolioWhileOn, Is.EqualTo(new[] { (UserRole.Viewer, EveryoneSignedIn) }), "WHAT: the Portfolio's rules do not name everyone signed in as Viewer while the switch is on. WHY: a Portfolio Admin needs the same view. HOW: add the same rule to a Portfolio's rules.");
                Assert.That(teamAfterOff, Is.Empty, "WHAT: Team Zenith's rules still name everyone signed in after the switch was turned off. WHY: the Access tab must say who reaches the Team now. HOW: add the rule only while the stored switch is on.");
            }
        }
    }

    /// <summary>Who asks, in the table of what the switch gives.</summary>
    public enum Someone
    {
        Morgan,
        MorgansKey,
        Stranger,
        StrangersKey,
        Jordan,
    }

    /// <summary>How the switch is stored when a scenario starts.</summary>
    public enum SwitchAtStart
    {
        On,
        Off,
        NoRowStored,
    }

    /// <summary>What a read of a unit answered: allowed, refused, or an error instead of an answer.</summary>
    public enum ReadAnswer
    {
        Allowed,
        Refused,
        Error,
    }

    public sealed record ReachOnTwoTeams(bool ReadsZenith, bool WritesZenith, bool ReadsApollo, bool WritesApollo);

    /// <summary>The switch as read: whether everyone signed in can view, and the token a save must be based on.</summary>
    public sealed record SwitchState(bool On, Guid Token);

    /// <summary>What a save of the switch answered: accepted, refused, or a conflict because its token was stale.</summary>
    public sealed record SaveAnswer(bool Accepted, bool Refused, bool Conflict, string Detail);

    /// <summary>
    /// Drives the access service the way a signed-in person, their key and a System Admin reach it, with no web host:
    /// a database of the scenario's own, access control switched on, a Premium licence, Alex as the one System Admin,
    /// Jordan as Team Admin of Team Zenith, and Morgan, who has a profile and a key but no role and no group. A person
    /// with no profile is a stranger. The instance starts by running the start-up seeder of the switch, as the host does.
    /// </summary>
    public sealed class EveryoneSignedInScenario : IDisposable
    {
        public const string Alex = SystemWideAccessScenario.Alex;

        public const string Morgan = "oidc|morgan-davies";

        public const string Jordan = "oidc|jordan-lee";

        public const string Stranger = "oidc|not-signed-in-before";

        public const int TeamZenith = SystemWideAccessScenario.TeamZenith;

        public const int TeamApollo = SystemWideAccessScenario.TeamApollo;

        public const int TeamMeridianCreatedLater = SystemWideAccessScenario.TeamMeridianCreatedLater;

        public const int PortfolioOceanExplorer = SystemWideAccessScenario.PortfolioOceanExplorer;

        public const string EveryoneSignedIn = "everyone signed in";

        public static readonly int[] EveryTeamAskedAbout = [TeamZenith, TeamApollo, TeamMeridianCreatedLater];

        public static readonly int[] EveryPortfolioAskedAbout = [PortfolioOceanExplorer];

        private const int AKeyWithNoOwner = 9999;

        private readonly ServicePortHarness harness;

        private readonly FailingSwitchReads failingReads = new();

        private readonly int morgansKeyId;

        public EveryoneSignedInScenario(string groupClaimName = SystemWideAccessScenario.GroupClaim)
        {
            harness = new ServicePortHarness(services =>
            {
                services.ConfigureDbContext<LighthouseAppContext>(options => options.AddInterceptors(failingReads));
                RegisterThePort(services, groupClaimName);
            });

            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            ThePeopleAreKnown(context);
            morgansKeyId = AKeyIsStoredFor(context, Morgan);
        }

        public static ClaimsPrincipal Alex_SignedIn() => SystemWideAccessScenario.SignedIn(Alex);

        public static ClaimsPrincipal Morgan_SignedIn() => SystemWideAccessScenario.SignedIn(Morgan);

        public static ClaimsPrincipal Jordan_SignedIn() => SystemWideAccessScenario.SignedIn(Jordan);

        public static ClaimsPrincipal Stranger_SignedIn() => SystemWideAccessScenario.SignedIn(Stranger);

        public ClaimsPrincipal MorgansKey() => SystemWideAccessScenario.TheKey(Morgan, morgansKeyId);

        /// <summary>The access service and what it needs, as a scenario on any database registers them.</summary>
        public static void RegisterThePort(IServiceCollection services, string groupClaimName = SystemWideAccessScenario.GroupClaim)
        {
            services.AddSingleton(Options.Create(new AuthorizationConfiguration { Enabled = true, GroupClaimName = groupClaimName }));
            services.AddSingleton(AValidPremiumLicence());
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICurrentUserProfileService, PeopleWhoSignedInBefore>();
            services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
        }

        /// <summary>Alex as System Admin, Jordan as Team Admin of Team Zenith, and Morgan with no role and no group.</summary>
        public static void ThePeopleAreKnown(LighthouseAppContext context)
        {
            var alex = new UserProfile { Subject = Alex, SubjectClaimType = "sub", DisplayName = "Alex Chen" };
            var jordan = new UserProfile { Subject = Jordan, SubjectClaimType = "sub", DisplayName = "Jordan Lee" };
            var morgan = new UserProfile { Subject = Morgan, SubjectClaimType = "sub", DisplayName = "Morgan Davies", LastKnownGroupClaimValues = JsonSerializer.Serialize(Array.Empty<string>()) };
            context.UserProfiles.AddRange(alex, jordan, morgan);
            context.SaveChanges();
            context.UserPermissions.Add(new UserPermission { UserProfileId = alex.Id, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null });
            context.UserPermissions.Add(new UserPermission { UserProfileId = jordan.Id, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = TeamZenith });
            context.SaveChanges();
        }

        public async Task TheInstanceHasStarted()
        {
            using var scope = harness.NewScope();
            await TheSwitch.StartUpSeederRuns(scope.ServiceProvider);
        }

        public async Task TheSwitchStartsAs(SwitchAtStart switchAtStart)
        {
            if (switchAtStart == SwitchAtStart.NoRowStored)
            {
                return;
            }

            await TheInstanceHasStarted();
            if (switchAtStart == SwitchAtStart.On)
            {
                await AlexHasTurnedTheSwitchOn();
            }
        }

        public Task<SwitchState> TheSwitchAsReadByAlex() => Port(TheSwitch.Read);

        public Task<SaveAnswer> WhatASaveAnswers(ClaimsPrincipal who, bool on, Guid? token)
            => Port(port => TheSwitch.Save(port, who, on, token));

        public Task AlexHasTurnedTheSwitchOn() => AlexHasTurnedTheSwitch(true);

        public Task AlexHasTurnedTheSwitchOff() => AlexHasTurnedTheSwitch(false);

        public SwitchState TheStoredSwitch()
        {
            using var scope = harness.NewScope();
            var rows = TheSwitch.StoredRows(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());
            Assert.That(rows, Has.Count.EqualTo(1), "WHAT: there is not exactly one stored switch. WHY: the switch is one row, read and saved under its own token. HOW: keep a single row, inserted when missing.");
            return rows[0];
        }

        public void TheSwitchCannotBeRead()
        {
            using var scope = harness.NewScope();
            failingReads.FailReadsOf(TheSwitch.TableName(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>()));
        }

        public async Task<ReachOnTwoTeams> WhatTheyMayDoOnTwoTeams(Someone who)
        {
            var principal = ThePrincipalOf(who);
            return new ReachOnTwoTeams(
                await CanReadTeam(principal, TeamZenith),
                await CanWriteTeam(principal, TeamZenith),
                await CanReadTeam(principal, TeamApollo),
                await CanWriteTeam(principal, TeamApollo));
        }

        public async Task<ReadAnswer> WhatAReadAnswers(Func<Task<bool>> read)
        {
            try
            {
                return await read() ? ReadAnswer.Allowed : ReadAnswer.Refused;
            }
            catch (Exception exception) when (exception is not AssertionException)
            {
                return ReadAnswer.Error;
            }
        }

        public Task<bool> CanReadTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanReadTeamAsync(person, teamId));

        public Task<bool> CanWriteTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanWriteTeamAsync(person, teamId));

        public Task<bool> CanReadPortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanReadPortfolioAsync(person, portfolioId));

        public Task<bool> CanWritePortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanWritePortfolioAsync(person, portfolioId));

        public Task<bool> CanManageAccess(ClaimsPrincipal person) => Port(port => port.CanManageRbacAsync(person));

        public Task<IReadOnlyList<int>> TheTeamsListed(ClaimsPrincipal person) => Port(port => port.GetReadableTeamIdsAsync(person, EveryTeamAskedAbout));

        public Task<IReadOnlyList<int>> ThePortfoliosListed(ClaimsPrincipal person) => Port(port => port.GetReadablePortfolioIdsAsync(person, EveryPortfolioAskedAbout));

        public async Task<IReadOnlyList<(UserRole Role, string Holder)>> TheRulesReachingTeam(int teamId)
            => [.. (await Port(port => port.GetTeamSystemWideRulesAsync(teamId))).Select(rule => (rule.Role, rule.Holder))];

        public async Task<IReadOnlyList<(UserRole Role, string Holder)>> TheRulesReachingPortfolio(int portfolioId)
            => [.. (await Port(port => port.GetPortfolioSystemWideRulesAsync(portfolioId))).Select(rule => (rule.Role, rule.Holder))];

        /// <summary>Stores the switch as a row in the state given, as a database restored or written earlier holds it.</summary>
        public void TheSwitchIsStored(bool on)
        {
            using var scope = harness.NewScope();
            TheSwitch.StoreARow(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>(), on);
        }

        public IReadOnlyList<SwitchState> TheStoredRows()
        {
            using var scope = harness.NewScope();
            return TheSwitch.StoredRows(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());
        }

        public void Dispose() => harness.Dispose();

        private async Task AlexHasTurnedTheSwitch(bool on)
        {
            var read = await TheSwitchAsReadByAlex();
            var saved = await WhatASaveAnswers(Alex_SignedIn(), on, read.Token);
            Assert.That(saved.Accepted, Is.True, $"WHAT: Alex, a System Admin, could not turn the switch {(on ? "on" : "off")} on the token just read ({saved}). WHY: a System Admin turns everyone signed in on and off. HOW: save the switch under the token read, inserting the row when it is missing.");
        }

        private ClaimsPrincipal ThePrincipalOf(Someone who) => who switch
        {
            Someone.Morgan => Morgan_SignedIn(),
            Someone.MorgansKey => MorgansKey(),
            Someone.Stranger => Stranger_SignedIn(),
            Someone.StrangersKey => SystemWideAccessScenario.TheKey(Stranger, AKeyWithNoOwner),
            Someone.Jordan => Jordan_SignedIn(),
            _ => throw new ArgumentOutOfRangeException(nameof(who)),
        };

        private async Task<T> Port<T>(Func<IRbacAdministrationService, Task<T>> step)
        {
            using var scope = harness.NewScope();
            return await step(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>());
        }

        private static int AKeyIsStoredFor(LighthouseAppContext context, string owner)
        {
            var profile = context.UserProfiles.Single(p => p.Subject == owner);
            var key = new ApiKey
            {
                Name = $"key of {profile.DisplayName}",
                KeyHash = "hash",
                Salt = "salt",
                CreatedByUser = profile.DisplayName ?? owner,
                OwnerUserProfileId = profile.Id,
                OwnerSubject = owner,
                CreatedAt = TestToday.FrozenNow.UtcDateTime,
            };
            context.ApiKeys.Add(key);
            context.SaveChanges();
            return key.Id;
        }

        private static ILicenseService AValidPremiumLicence()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
            return licence.Object;
        }

        /// <summary>Finds the person a principal speaks for among those who signed in before; a stranger is nobody.</summary>
        private sealed class PeopleWhoSignedInBefore(LighthouseAppContext context) : ICurrentUserProfileService
        {
            public async Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(principal);
                var subject = principal.FindFirst("sub")?.Value;
                return string.IsNullOrWhiteSpace(subject)
                    ? null
                    : await context.UserProfiles.SingleOrDefaultAsync(profile => profile.Subject == subject, cancellationToken);
            }
        }
    }

    /// <summary>
    /// The switch as the access service reads and saves it, as the start-up seeder stores it, and as the database
    /// holds it, so every scenario speaks of it in the same words.
    /// </summary>
    public static class TheSwitch
    {
        public static async Task<SwitchState> Read(IRbacAdministrationService port)
        {
            var stored = await port.GetEveryoneSignedInAsync();
            return new SwitchState(stored.EveryoneCanView, stored.ConcurrencyToken);
        }

        public static async Task<SaveAnswer> Save(IRbacAdministrationService port, ClaimsPrincipal caller, bool on, Guid? token)
        {
            try
            {
                var result = await port.SetEveryoneSignedInAsync(caller, on, token);
                return new SaveAnswer(result.Succeeded, !result.Succeeded, false, result.Message ?? "accepted");
            }
            catch (DbUpdateConcurrencyException conflict)
            {
                return new SaveAnswer(false, false, true, conflict.GetType().Name);
            }
        }

        public static async Task StartUpSeederRuns(IServiceProvider services)
            => await ActivatorUtilities.CreateInstance<EveryoneSignedInSeeder>(services).Seed();

        public static IReadOnlyList<SwitchState> StoredRows(LighthouseAppContext context)
            => [.. context.EveryoneSignedInSettings.AsNoTracking().OrderBy(row => row.Id).Select(row => new SwitchState(row.EveryoneCanView, row.ConcurrencyToken))];

        public static void StoreARow(LighthouseAppContext context, bool on)
        {
            context.EveryoneSignedInSettings.Add(EveryoneSignedInSetting.StoredAs(on));
            context.SaveChanges();
        }

        public static string TableName(LighthouseAppContext context)
            => context.Model.FindEntityType(typeof(EveryoneSignedInSetting))!.GetTableName()!;
    }

    /// <summary>Makes every read of one table fail, as a database that cannot answer for that table would.</summary>
    public sealed class FailingSwitchReads : DbCommandInterceptor
    {
        private string? quotedTable;

        public void FailReadsOf(string table) => quotedTable = $"\"{table}\"";

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            FailIfItReadsTheSwitch(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            FailIfItReadsTheSwitch(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            FailIfItReadsTheSwitch(command);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            FailIfItReadsTheSwitch(command);
            return ValueTask.FromResult(result);
        }

        private void FailIfItReadsTheSwitch(DbCommand command)
        {
            if (quotedTable is not null && command.CommandText.Contains(quotedTable, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The database could not answer for the switch.");
            }
        }
    }
}
