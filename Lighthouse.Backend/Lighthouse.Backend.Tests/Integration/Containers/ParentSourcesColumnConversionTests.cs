using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// How the parent source columns cross a real database. NULL is a follower and has to stay one, a list comes
    /// back in the order it was saved, and text the rules refuse stops the read of that one owner instead of being
    /// repaired, because a repaired list would quietly change where parents come from.
    /// </summary>
    [TestFixture]
    public class ParentSourcesColumnConversionTests : IntegrationFixture
    {
        private static readonly ParentSources FieldThenOwnParent = ParentSources.Of([ParentSourceEntry.Field(11), ParentSourceEntry.TrackerParent]).List!;

        private static readonly ParentSources OwnParentThenField = ParentSources.Of([ParentSourceEntry.TrackerParent, ParentSourceEntry.Field(11)]).List!;

        private SqliteConnection database = null!;

        private DbContextOptions<LighthouseAppContext> options = null!;

        private int connectionId;

        private int teamId;

        private int otherTeamId;

        private int portfolioId;

        [SetUp]
        public async Task CreateTheDatabaseWithOwnersThatHoldNoList()
        {
            database = new SqliteConnection("DataSource=:memory:");
            database.Open();
            options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite(database).Options;

            await using var context = AContext();
            await context.Database.EnsureCreatedAsync();

            var connection = new WorkTrackingSystemConnection { Name = "Jira", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Payments", WorkTrackingSystemConnection = connection };
            var otherTeam = new Team { Name = "Checkout", WorkTrackingSystemConnection = connection };
            var portfolio = new Portfolio { Name = "Launch", WorkTrackingSystemConnection = connection };
            context.AddRange(team, otherTeam, portfolio);
            await context.SaveChangesAsync();

            (connectionId, teamId, otherTeamId, portfolioId) = (connection.Id, team.Id, otherTeam.Id, portfolio.Id);
        }

        [TearDown]
        public void CloseTheDatabase() => database.Dispose();

        [Test]
        public async Task A_NULL_column_reads_back_as_no_list_on_every_owner()
        {
            await using var context = AContext();

            var team = await context.Teams.SingleAsync(owner => owner.Id == teamId);
            var portfolio = await context.Portfolios.SingleAsync(owner => owner.Id == portfolioId);
            var connection = await context.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ParentSources, Is.Null, "Read as an empty list, a follower would turn into an owner of nothing.");
                Assert.That(portfolio.ParentSources, Is.Null);
                Assert.That(connection.ParentSourcesForTeams, Is.Null);
                Assert.That(connection.ParentSourcesForPortfolios, Is.Null);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task Blank_text_reads_back_as_no_list(string blank)
        {
            await StoreTextOnTheTeam(blank);

            await using var context = AContext();
            var team = await context.Teams.SingleAsync(owner => owner.Id == teamId);

            Assert.That(team.ParentSources, Is.Null);
        }

        [TestCase("""[]""", Description = "An empty list")]
        [TestCase("""not a list""", Description = "Text that is no JSON")]
        [TestCase("""[{"Kind":1}]""", Description = "An entry that lost a member")]
        public async Task Text_the_rules_refuse_stops_the_read_of_that_owner_and_of_no_other(string badText)
        {
            await StoreTextOnTheTeam(badText);

            await using var context = AContext();

            await Assert.CatchAsync(async () => await context.Teams.SingleAsync(owner => owner.Id == teamId));
            Assert.That((await context.Teams.SingleAsync(owner => owner.Id == otherTeamId)).Name, Is.EqualTo("Checkout"));
        }

        [Test]
        public async Task Lists_come_back_from_every_column_in_the_order_they_were_saved()
        {
            await using (var saving = AContext())
            {
                (await saving.Teams.SingleAsync(owner => owner.Id == teamId)).OwnParentSources(OwnParentThenField);
                (await saving.Portfolios.SingleAsync(owner => owner.Id == portfolioId)).OwnParentSources(FieldThenOwnParent);
                var connection = await saving.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);
                connection.SetParentSourcesDefaultForTeams(FieldThenOwnParent);
                connection.SetParentSourcesDefaultForPortfolios(OwnParentThenField);
                await saving.SaveChangesAsync();
            }

            await using var reading = AContext();
            var team = await reading.Teams.SingleAsync(owner => owner.Id == teamId);
            var portfolio = await reading.Portfolios.SingleAsync(owner => owner.Id == portfolioId);
            var read = await reading.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ParentSources!.Entries, Is.EqualTo(OwnParentThenField.Entries).AsCollection);
                Assert.That(portfolio.ParentSources!.Entries, Is.EqualTo(FieldThenOwnParent.Entries).AsCollection);
                Assert.That(read.ParentSourcesForTeams!.Entries, Is.EqualTo(FieldThenOwnParent.Entries).AsCollection);
                Assert.That(read.ParentSourcesForPortfolios!.Entries, Is.EqualTo(OwnParentThenField.Entries).AsCollection);
            }
        }

        [Test]
        public async Task A_connection_row_an_older_build_left_with_an_empty_old_column_keeps_its_list_when_reloaded_and_when_read()
        {
            await using (var saving = AContext())
            {
                var stored = await saving.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);
                stored.SetParentSourcesDefaultForTeams(FieldThenOwnParent);
                stored.SetParentSourcesDefaultForPortfolios(FieldThenOwnParent);
                await saving.SaveChangesAsync();
            }

            await using var reloading = AContext();
            var reloaded = await reloading.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);
            await reloading.Database.ExecuteSqlAsync($"""UPDATE "WorkTrackingSystemConnections" SET "ParentOverrideForTeams" = NULL, "ParentOverrideForPortfolios" = NULL WHERE "Id" = {connectionId}""");
            await reloading.Entry(reloaded).ReloadAsync();

            await using var reading = AContext();
            var read = await reading.WorkTrackingSystemConnections.SingleAsync(owner => owner.Id == connectionId);

            using (Assert.EnterMultipleScope())
            {
                foreach (var connection in new[] { reloaded, read })
                {
                    Assert.That(connection.ParentOverrideForTeams, Is.Null, "The row has to be the inconsistent one.");
                    Assert.That(connection.ParentOverrideForPortfolios, Is.Null, "The row has to be the inconsistent one.");
                    Assert.That(connection.ParentSourcesForTeams?.Entries, Is.EqualTo(FieldThenOwnParent.Entries).AsCollection);
                    Assert.That(connection.ParentSourcesForPortfolios?.Entries, Is.EqualTo(FieldThenOwnParent.Entries).AsCollection);
                }
            }
        }

        [Test]
        public async Task Only_a_list_that_differs_counts_as_a_change_worth_saving()
        {
            await using (var saving = AContext())
            {
                (await saving.Teams.SingleAsync(owner => owner.Id == teamId)).OwnParentSources(FieldThenOwnParent);
                await saving.SaveChangesAsync();
            }

            await using var context = AContext();
            var team = await context.Teams.SingleAsync(owner => owner.Id == teamId);

            var afterTheSameEntries = TheListIsModifiedOnceSetBy(context, team, () => team.OwnParentSources(ParentSources.Of(FieldThenOwnParent.Entries).List!));
            var afterAnotherOrder = TheListIsModifiedOnceSetBy(context, team, () => team.OwnParentSources(OwnParentThenField));
            var afterFollowing = TheListIsModifiedOnceSetBy(context, team, team.FollowParentOverrideDefault);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheSameEntries, Is.False);
                Assert.That(afterAnotherOrder, Is.True, "Another order names another parent, so it has to reach the database.");
                Assert.That(afterFollowing, Is.True);
            }
        }

        private static bool TheListIsModifiedOnceSetBy(LighthouseAppContext context, Team team, Action change)
        {
            change();
            context.ChangeTracker.DetectChanges();

            return context.Entry(team).Property(owner => owner.ParentSources).IsModified;
        }

        private async Task StoreTextOnTheTeam(string text)
        {
            await using var context = AContext();
            await context.Database.ExecuteSqlAsync($"""UPDATE "Teams" SET "ParentSources" = {text} WHERE "Id" = {teamId}""");
        }

        private LighthouseAppContext AContext() => new(options, new FakeCryptoService(), NullLogger<LighthouseAppContext>.Instance);
    }
}
