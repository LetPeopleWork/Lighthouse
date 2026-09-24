using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// Step definitions for Story #5913. Backend-observable contract: after start-up the settings list
    /// carries no Faster Updates row and the store holds none, whatever an older release left behind; and
    /// a team or portfolio whose change stamps are already stored takes the cheaper refresh on its next
    /// cycle without anybody having asked for it.
    /// </summary>
    public partial class Story5913AlwaysFasterUpdatesTest : FasterUpdatesAcceptanceTest
    {
        public enum HowTheOperatorLeftIt
        {
            On,
            Off,
        }

        /// <summary>
        /// The key an older release stored the switch under, spelled out rather than read off the product's
        /// constant. The row an upgrading instance carries has this key whatever the constant says today,
        /// so a renamed constant must not be able to make "the row is gone" pass while the old row stays.
        /// </summary>
        private const string FasterUpdatesStoredKey = "DeltaSync";

        private const string SummaryMarker = "Update completed";
        private const string ModeField = "mode=";
        private const string ScannedField = "scanned=";
        private const string FetchedField = "fetched=";

        private const string TheParentFeature = "PARENT-1";

        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        private readonly record struct SeededTeam(int Id);

        private readonly record struct SeededPortfolio(int Id);

        // --- Given: the instance ---

        /// <summary>
        /// The switch as an older release left it in the database. Written straight into the store because
        /// that is where an upgrading instance carries it; the product no longer offers a way to set it.
        /// </summary>
        private void GivenTheInstanceHadFasterUpdates(HowTheOperatorLeftIt position)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var enabled = position == HowTheOperatorLeftIt.On;
            var stored = repository.GetByPredicate(feature => feature.Key == FasterUpdatesStoredKey);

            if (stored == null)
            {
                repository.Add(new OptionalFeature
                {
                    Id = 0,
                    Key = FasterUpdatesStoredKey,
                    Name = "Faster Updates",
                    Description = "Fetch only the {{workItems}} that changed since the last update instead of the whole query.",
                    Enabled = enabled,
                    IsPreview = false,
                });
            }
            else
            {
                stored.Enabled = enabled;
                repository.Update(stored);
            }

            repository.Save().GetAwaiter().GetResult();
        }

        private void GivenTheInstanceHasNoPremiumLicence()
            => LicenseServiceMock.Setup(licence => licence.CanUsePremiumFeatures()).Returns(false);

        /// <summary>
        /// An upgraded instance is one the upgrade actually ran against, not a database hand-shaped to look
        /// like its result.
        /// </summary>
        private void GivenTheInstanceHasBeenUpgraded() => WhenTheInstanceIsUpgraded();

        private Task<List<string>> GivenWhatTheAdminSawBeforeTheUpgrade() => WhenTheAdminOpensTheSystemSettings();

        // --- Given: the tracker and what is already stored ---

        private SeededTeam GivenAJiraCloudTeamWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var team = new SeededTeam(SeedTeam(connectionId, $"Team {Guid.NewGuid():N}"));
            TheTrackerCanBeScanned();

            return team;
        }

        private SeededPortfolio GivenAJiraCloudPortfolioWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var portfolio = new SeededPortfolio(SeedPortfolio(connectionId, $"Portfolio {Guid.NewGuid():N}"));
            TheTrackerCanBeScanned();

            return portfolio;
        }

        private void GivenTheTrackerHoldsThreeIssues()
            => TheTrackerHolds(
                new RemoteRecord("ITEM-1", AWhileAgo),
                new RemoteRecord("ITEM-2", AWhileAgo),
                new RemoteRecord("ITEM-3", AWhileAgo));

        private void GivenTheTrackerHoldsThreeFeatures()
            => TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo),
                new RemoteRecord("FEAT-2", AWhileAgo),
                new RemoteRecord("FEAT-3", AWhileAgo));

        private void GivenTheTrackerHoldsTwoFeaturesUnderOneParent()
        {
            TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo) { ParentReferenceId = TheParentFeature },
                new RemoteRecord("FEAT-2", AWhileAgo) { ParentReferenceId = TheParentFeature });

            TheTrackerHoldsParentFeatures(new RemoteRecord(TheParentFeature, AWhileAgo) { Name = "The parent feature" });
        }

        /// <summary>
        /// The refresh the instance ran while the switch was still off. It downloads everything, and in
        /// doing so stores the change stamps and the fetch fingerprint the next cycle compares against.
        /// </summary>
        private Task GivenTheTeamWasRefreshedBeforeTheUpgrade(SeededTeam team) => WhenTheScheduledRefreshRuns(team);

        private Task GivenThePortfolioWasRefreshedBeforeTheUpgrade(SeededPortfolio portfolio) => WhenTheScheduledRefreshRuns(portfolio);

        private void GivenOneIssueMovedOnTheTracker(string referenceId)
            => OnTheTrackerTheIssueChanges(referenceId, AWhileAgo.AddHours(1), state: "Done");

        private void GivenOneFeatureMovedOnTheTracker(string referenceId)
            => OnTheTrackerTheFeatureChanges(referenceId, AWhileAgo.AddHours(1), state: "Done");

        // --- When ---

        private void WhenTheInstanceIsUpgraded() => TheInstanceIsUpgradedAgain();

        /// <summary>The keys of every row on the Settings → System list, read the way the page reads them.</summary>
        private async Task<List<string>> WhenTheAdminOpensTheSystemSettings()
        {
            using var client = Factory.CreateClient().AsSystemAdmin();
            using var response = await client.GetAsync("/api/latest/optionalfeatures");
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"The settings list could not be read: {body}");

            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.EnumerateArray().Select(row => row.GetProperty("key").GetString() ?? string.Empty)];
        }

        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        // --- Then: the settings list and the store ---

        private static void ThenTheSettingsListOffersNoFasterUpdatesSwitch(List<string> offered)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.Not.Empty,
                    "positive control: the list came back empty, so its missing a row proves nothing.");
                Assert.That(offered, Does.Not.Contain(FasterUpdatesStoredKey),
                    "There is nothing left to switch, so an administrator must not be offered a switch. Offered: "
                    + string.Join(", ", offered));
            }
        }

        private static void ThenOnlyTheFasterUpdatesSwitchIsGone(List<string> before, List<string> after)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(before, Does.Contain(FasterUpdatesStoredKey),
                    "positive control: the instance was not offering the switch before the upgrade, so its absence afterwards proves nothing.");
                Assert.That(after, Is.EquivalentTo(before.Where(key => key != FasterUpdatesStoredKey)),
                    "The upgrade removes the Faster Updates row whichever way it was set, and nothing else. Before: "
                    + string.Join(", ", before) + " ; after: " + string.Join(", ", after));
            }
        }

        private void ThenNoFasterUpdatesSettingIsStored()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            Assert.That(repository.Exists(feature => feature.Key == FasterUpdatesStoredKey), Is.False,
                "A row that comes back on a later start-up is a switch nobody can see that the next release might read again.");
        }

        // --- Then: what the tracker was asked for ---

        private void ThenTheWholeQueryWasScannedForIdentitiesOnly()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ScansIssued, Is.EqualTo(1),
                    "Nobody asked for the cheaper refresh and nobody has to any more: the tracker can be scanned, so it is.");
                Assert.That(FullDownloadsIssued, Is.Zero,
                    "The team's change stamps were stored before the upgrade, so downloading the whole query again is the cost this story removes.");
            }
        }

        private void ThenOnlyTheIssuesThatMovedWereDownloaded(params string[] referenceIds)
            => Assert.That(PayloadDownloads, Is.EqualTo(new List<List<string>> { new(referenceIds) }),
                "One request, for exactly the issues whose change stamp moved. Requested: "
                + string.Join(" / ", PayloadDownloads.ConvertAll(request => string.Join(",", request))));

        private void ThenTheWholeFeatureQueryWasScannedForIdentitiesOnly()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FeatureScansIssued, Is.EqualTo(1),
                    "The portfolio's Feature query can be scanned, so it is, without anybody having asked.");
                Assert.That(FullFeatureDownloadsIssued, Is.Zero,
                    "The portfolio's change stamps were stored before the upgrade, so downloading every Feature again is the cost this story removes.");
            }
        }

        private void ThenOnlyTheFeaturesThatMovedWereDownloaded(params string[] referenceIds)
            => Assert.That(FeaturePayloadDownloads, Is.EqualTo(new List<List<string>> { new(referenceIds) }),
                "One request, for exactly the Features whose change stamp moved. Requested: "
                + string.Join(" / ", FeaturePayloadDownloads.ConvertAll(request => string.Join(",", request))));

        private void ThenTheParentFeaturesWereScannedFor(params string[] parentReferenceIds)
            => Assert.That(ParentFeatureScans, Is.EqualTo(new List<List<string>> { new(parentReferenceIds) }),
                "The parents take the cheaper path on their own, and nothing asks for it any more. Scans: "
                + string.Join(" / ", ParentFeatureScans.ConvertAll(request => string.Join(",", request))));

        private void ThenNoParentFeatureWasDownloaded()
            => Assert.That(ParentFeatureDownloads, Is.Empty,
                "No parent moved on the tracker, so none may be downloaded. Requested: "
                + string.Join(" / ", ParentFeatureDownloads.ConvertAll(request => string.Join(",", request))));

        // --- Then: what the operator reads and what was recorded ---

        private void ThenTheOperatorSeesACheaperUpdate(int scanned, int fetched)
        {
            var summary = TheSummaryLine();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Does.Contain($"{ModeField}delta").IgnoreCase,
                    "The log line is how an operator sees that the switch they had turned off no longer holds them back.");
                Assert.That(summary, Does.Contain($"{ScannedField}{scanned}"));
                Assert.That(summary, Does.Contain($"{FetchedField}{fetched}"));
            }
        }

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededTeam team, int scanned, int fetched)
            => ThenTheRefreshWasRecordedAsCheaper(TheLastRefreshLogFor(RefreshType.Team, team.Id), scanned, fetched);

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededPortfolio portfolio, int scanned, int fetched)
            => ThenTheRefreshWasRecordedAsCheaper(TheLastRefreshLogFor(RefreshType.Portfolio, portfolio.Id), scanned, fetched);

        private static void ThenTheRefreshWasRecordedAsCheaper(RefreshLog? recorded, int scanned, int fetched)
        {
            Assert.That(recorded, Is.Not.Null, "The refresh recorded nothing at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.Mode, Is.EqualTo(SyncMode.Delta));
                Assert.That(recorded.RecordsScanned, Is.EqualTo(scanned));
                Assert.That(recorded.RecordsFetched, Is.EqualTo(fetched));
                Assert.That(recorded.Success, Is.True);
            }
        }

        // --- Then: what is stored ---

        private void ThenTheParentFeatureIsStillStoredAndCurrent(string referenceId)
        {
            var parent = TheStoredFeature(referenceId);

            Assert.That(parent, Is.Not.Null, $"'{referenceId}' is a parent of Features the portfolio still holds, so a quiet cycle must not lose it.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent!.IsParentFeature, Is.True);
                Assert.That(parent.LastChangedRemote, Is.EqualTo(TheTrackersChangeStampForFeature(referenceId)),
                    "A parent whose stamp is lost is downloaded again on every later cycle.");
            }
        }

        // --- Reading the log ---

        private string TheSummaryLine()
        {
            var summaries = TheOperatorVisibleLines
                .Where(line => line.Contains(SummaryMarker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(summaries, Is.Not.Empty,
                "No update summary was written. Operator-visible lines: " + string.Join(" | ", TheOperatorVisibleLines));

            return summaries[0];
        }
    }
}
