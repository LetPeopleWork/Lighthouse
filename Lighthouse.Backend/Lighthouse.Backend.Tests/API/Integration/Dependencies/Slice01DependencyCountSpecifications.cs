using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Step definitions for the first dependency slice. Backend-observable contract: a refresh records
    /// what each Feature waits on as the tracker's own id strings, keyed to the Feature that waits, and
    /// the stored set is whatever the tracker last said - never a merge with what was there before.
    /// </summary>
    public partial class Slice01DependencyCountTest : DependenciesAcceptanceTest
    {
        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private static TrackedFeature AFeatureTheTrackerHolds(string referenceId, string name)
            => new(referenceId, name, []);

        private static TrackedFeature AFeatureWaitingOn(string referenceId, string name, string[] waitsOn)
            => new(referenceId, name, waitsOn);

        private static Feature AStoredFeature(int id, string referenceId)
            => new() { Id = id, ReferenceId = referenceId, Name = referenceId };

        /// <summary>
        /// A reference built the way a connector builds one: against a Feature it has not saved, so the
        /// Feature it names is nought rather than the row the reference will end up on.
        /// </summary>
        private static FeatureDependencyReference AReferenceTheConnectorBuiltBeforeSaving(string waitsOn)
            => new(0, waitsOn, DependencySource.TrackerLink);

        // --- When ---

        private Task WhenARefreshRuns(int portfolioId, params TrackedFeature[] rowsFromTheTracker)
            => DriveAPortfolioRefresh(portfolioId, rowsFromTheTracker);

        /// <summary>
        /// A refresh that also hands back the two Features every scenario here points at, so a scenario
        /// about the waiting end says nothing about the far end being there.
        /// </summary>
        private Task WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(int portfolioId, TrackedFeature theFeatureUnderTest)
            => WhenARefreshRuns(
                portfolioId,
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"),
                theFeatureUnderTest);

        private IDependencyReconciler WhenTheHostIsAskedForTheReconciler() => TheReconcilerTheHostResolves();

        // --- Then ---

        private void ThenTheFeatureWaitsOnExactly(string featureReferenceId, string[] expectedTargets)
        {
            var stored = ReadStoredDependencies()
                .Where(dependency => dependency.FeatureReferenceId == featureReferenceId)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Select(dependency => dependency.WaitsOnReferenceId).Order().ToArray(),
                    Is.EqualTo(expectedTargets.Order().ToArray()),
                    $"A refresh must store exactly what the tracker says {featureReferenceId} waits on. Stored: {Describe(stored)}");
                Assert.That(stored.Select(dependency => dependency.Source), Has.All.EqualTo(DependencySource.TrackerLink),
                    $"Everything read off a tracker link must say so, or a later override cannot be told apart from it. Stored: {Describe(stored)}");
            }
        }

        private void ThenTheFeatureWaitsOnNothing(string featureReferenceId)
        {
            var stored = ReadStoredDependencies()
                .Where(dependency => dependency.FeatureReferenceId == featureReferenceId)
                .ToList();

            Assert.That(stored, Is.Empty,
                $"A tracker that names no link for {featureReferenceId} leaves it waiting on nothing. Stored: {Describe(stored)}");
        }

        /// <summary>
        /// The invariant everything else here leans on: a stored reference names the Feature it hangs off.
        /// Connectors build references against a Feature that has no id yet, so anything reading the id
        /// off a reference - the deduplication key included - is wrong unless the write re-keys them.
        /// </summary>
        private void ThenEveryStoredReferenceNamesTheFeatureThatWaits()
        {
            var stored = ReadStoredDependencies();
            var mismatched = stored
                .Where(dependency => dependency.KeyedToFeatureId != dependency.OwningFeatureId)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored, Is.Not.Empty, "The scenario must store references for this to judge.");
                Assert.That(mismatched, Is.Empty,
                    $"Every reference must name the Feature it hangs off. Mismatched: {Describe(mismatched)}");
            }
        }

        private static void ThenEveryReferenceNames(Feature feature)
        {
            Assert.That(feature.DependsOnReferences.Select(reference => reference.FeatureId), Has.All.EqualTo(feature.Id),
                "Reconciling must key each reference to the Feature that waits, whatever the caller built it against.");
        }

        private static void ThenTheFeatureStillWaitsOn(Feature feature, string[] expectedTargets)
        {
            Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId).Order().ToArray(),
                Is.EqualTo(expectedTargets.Order().ToArray()),
                "Reconciling reads what it was given before it clears what is there, so handing it a Feature's own references keeps them.");
        }

        private static string Describe(IEnumerable<StoredDependency> dependencies)
            => string.Join(", ", dependencies.Select(dependency =>
                $"{dependency.FeatureReferenceId}(#{dependency.OwningFeatureId}) waits on {dependency.WaitsOnReferenceId} keyed to #{dependency.KeyedToFeatureId}"));
    }
}
