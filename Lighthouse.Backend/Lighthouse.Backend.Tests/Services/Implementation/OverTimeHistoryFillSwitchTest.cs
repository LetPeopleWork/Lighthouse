using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [Category("story-6053-reconstruct-over-time-history")]
    public class OverTimeHistoryFillSwitchTest
    {
        [Test]
        public void TheFillFeatureTurnedOn_SwitchesTheFillOn()
        {
            var fillSwitch = SwitchOver(Stored(OptionalFeatureKeys.OverTimeHistoryFillKey, enabled: true));

            Assert.That(fillSwitch.IsSwitchedOn(), Is.True);
        }

        [Test]
        public void TheFillFeatureLeftOff_LeavesTheFillOff_WhateverElseIsOn()
        {
            var fillSwitch = SwitchOver(
                Stored(OptionalFeatureKeys.UsageDataKey, enabled: true),
                Stored(OptionalFeatureKeys.OverTimeHistoryFillKey, enabled: false));

            Assert.That(fillSwitch.IsSwitchedOn(), Is.False);
        }

        /// <summary>
        /// An instance whose seeder has not run yet has no row, and must not start writing days
        /// nobody opted in to.
        /// </summary>
        [Test]
        public void NoStoredFillFeature_LeavesTheFillOff_EvenWhenAnotherFeatureIsOn()
        {
            var fillSwitch = SwitchOver(Stored(OptionalFeatureKeys.UsageDataKey, enabled: true));

            Assert.That(fillSwitch.IsSwitchedOn(), Is.False);
        }

        /// <summary>Every stored optional feature carries the same Id, which is why the switch looks it up by key.</summary>
        private static OptionalFeature Stored(string key, bool enabled) => new() { Id = 0, Key = key, Enabled = enabled };

        private static OverTimeHistoryFillSwitch SwitchOver(params OptionalFeature[] stored)
        {
            var features = new Mock<IRepository<OptionalFeature>>();
            features
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>()))
                .Returns((Func<OptionalFeature, bool> predicate) => Array.Find(stored, feature => predicate(feature)));

            return new OverTimeHistoryFillSwitch(features.Object);
        }
    }
}
