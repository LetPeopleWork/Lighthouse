using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Step definitions that only this fixture needs. The shared ones are in the harness.
    /// </summary>
    public partial class Slice01FieldChangeUpdatesItsOwnersTest : ConnectionFieldChangeAcceptanceTest
    {
        /// <summary>
        /// How many times each ran once the tracker answered again: Atlas and Cobalt once more after the
        /// run they were already in or waiting for, everyone else once.
        /// </summary>
        private static readonly string[] RunsAfterTheSave =
        [
            $"{Atlas} 2", $"{Borealis} 1", $"{Cobalt} 2", $"{CustomerApps} 1", $"{DeltaOps} 1", $"{FleetPlatform} 1",
        ];

        /// <summary>
        /// Unlike the other saves, this one keeps the refresh history from before Atlas started, because
        /// the run Atlas was in the middle of belongs to the scenario.
        /// </summary>
        private Task WhenPriyaAddsFixVersionsAndSavesWhileOthersAreUpdating(string connection)
            => SaveKeepingTheHistoryAlreadySetAside(connection, FieldEdit.AddsAField);

        private async Task ThenAtlasAndCobaltRanTwiceAndEveryOneDownloadedEverythingOnce()
        {
            var history = await TheHistoryOnceEachOfThemHasRun(EveryTeamAndPortfolioOnNordwindJiraDc);

            var runs = history
                .GroupBy(entry => entry.EntityName)
                .OrderBy(owner => owner.Key, StringComparer.Ordinal)
                .Select(owner => $"{owner.Key} {owner.Count()}");
            var fullDownloads = history
                .Where(entry => entry.Mode == SyncMode.Full)
                .Select(entry => entry.EntityName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Is.EqualTo(RunsAfterTheSave),
                    $"Atlas and Cobalt run once more, everyone else once: {Describe(history)}");
                Assert.That(fullDownloads, Is.EquivalentTo(EveryTeamAndPortfolioOnNordwindJiraDc),
                    $"Every one of them downloads everything exactly once because of the save: {Describe(history)}");
            }
        }
    }
}
