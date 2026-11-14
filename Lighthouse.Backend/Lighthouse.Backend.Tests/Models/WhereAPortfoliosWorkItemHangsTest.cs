using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Where a Portfolio's Feature hangs once the Portfolio's own choice and the connection's Parent defaults have
    /// met. A Portfolio reads only the Portfolios' default, never the Teams' one. The item holds a value in every
    /// field and under its tracker parent, so the place it ends up under says which list the Portfolio read.
    /// </summary>
    [TestFixture]
    public class WhereAPortfoliosWorkItemHangsTest
    {
        private const int ThePortfoliosOwnField = 5;
        private const int TheTeamsDefault = 8;
        private const int ThePortfoliosDefault = 9;

        private const string UnderThePortfoliosOwnField = "OWN-1";
        private const string UnderTheTeamsDefaultsField = "TEAMS-DEFAULT-1";
        private const string UnderThePortfoliosDefaultsField = "PORTFOLIOS-DEFAULT-1";
        private const string UnderTheTrackersOwnParent = "TRACKER-1";

        [Test]
        public void A_Portfolio_that_follows_a_Portfolios_default_naming_nothing_hangs_its_item_under_the_trackers_own_parent_whatever_the_Teams_default_names()
        {
            var portfolio = APortfolioOnAConnectionWhoseDefaultsName(teams: TheTeamsDefault, portfolios: null);
            portfolio.FollowParentOverrideDefault();

            Assert.That(WhereTheItemHangs(portfolio), Is.EqualTo(UnderTheTrackersOwnParent));
        }

        [Test]
        public void A_Portfolio_that_follows_the_default_hangs_its_item_under_the_field_the_Portfolios_default_names()
        {
            var portfolio = APortfolioOnAConnectionWhoseDefaultsName(teams: TheTeamsDefault, portfolios: ThePortfoliosDefault);
            portfolio.FollowParentOverrideDefault();

            Assert.That(WhereTheItemHangs(portfolio), Is.EqualTo(UnderThePortfoliosDefaultsField));
        }

        [Test]
        public void A_Portfolio_with_its_own_field_hangs_its_item_under_that_field_whatever_the_Portfolios_default_names()
        {
            var portfolio = APortfolioOnAConnectionWhoseDefaultsName(teams: TheTeamsDefault, portfolios: ThePortfoliosDefault);
            portfolio.OwnParentOverride(ThePortfoliosOwnField);

            Assert.That(WhereTheItemHangs(portfolio), Is.EqualTo(UnderThePortfoliosOwnField));
        }

        private static Portfolio APortfolioOnAConnectionWhoseDefaultsName(int? teams, int? portfolios)
            => new() { WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = teams, ParentOverrideForPortfolios = portfolios } };

        /// <summary>Reads each entry of the Portfolio's effective list the way a connector does, then lets the first place with a parent win.</summary>
        private static string WhereTheItemHangs(Portfolio portfolio)
        {
            var readings = ParentReadings.Of([.. portfolio.TheEffectiveParentSources().Entries.Select(entry => new ParentReadingAt(entry, WhatItHoldsAt(entry)))]);
            var item = new WorkItemBase { ParentReferenceId = string.Empty };

            ParentSourceSelector.HangUnder(item, readings, ParentSourceSelector.Choose(readings, new StoredFeatures(new Dictionary<string, DateTime?>()), ParentNarrowing.Narrow));

            return item.ParentReferenceId;
        }

        private static ParentReading WhatItHoldsAt(ParentSourceEntry entry)
            => entry.AdditionalFieldDefinitionId switch
            {
                ThePortfoliosOwnField => ParentReading.FieldValue(UnderThePortfoliosOwnField),
                TheTeamsDefault => ParentReading.FieldValue(UnderTheTeamsDefaultsField),
                ThePortfoliosDefault => ParentReading.FieldValue(UnderThePortfoliosDefaultsField),
                _ => ParentReading.TrackersOwn(UnderTheTrackersOwnParent),
            };
    }
}
