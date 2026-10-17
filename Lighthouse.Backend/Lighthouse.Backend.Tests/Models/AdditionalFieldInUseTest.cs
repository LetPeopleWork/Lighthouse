using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    public class AdditionalFieldInUseTest
    {
        private const int CausedBy = 5;
        private const int ParentLink = 6;
        private const string TeamsParentOverride = "Teams' Parent Override Field";
        private const string PortfoliosDependency = "Portfolios' Dependency Field";

        private static readonly RemovedAdditionalField RemovedCausedBy = new(CausedBy, "Caused by");
        private static readonly RemovedAdditionalField RemovedParentLink = new(ParentLink, "Parent Link");

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_field_a_default_and_an_owner_use_is_refused_naming_both()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [new DefaultNamingAField(TeamsParentOverride, CausedBy)],
                [Owner("Ledger", CausedBy)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default, and Ledger uses it as its own. Nothing was saved."));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_field_only_an_owner_uses_is_refused_naming_only_the_owner()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [new DefaultNamingAField(TeamsParentOverride, ParentLink)],
                [Owner("Ledger", CausedBy)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. Ledger uses it as its own. Nothing was saved."));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_field_only_a_default_names_is_refused_naming_only_the_default()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [new DefaultNamingAField(TeamsParentOverride, CausedBy)],
                [Owner("Ledger", ParentLink)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default. Nothing was saved."));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void A_field_nobody_uses_is_not_in_use()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [new DefaultNamingAField(TeamsParentOverride, null)],
                [Owner("Ledger", null, ParentLink)]);

            Assert.That(inUse, Is.Empty);
        }

        [Test]
        public void Several_owners_are_listed_in_the_order_given()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [],
                [Owner("Ledger", CausedBy), Owner("Ops Tooling", CausedBy), Owner("Merchant Onboarding", null, CausedBy)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. Ledger, Ops Tooling and Merchant Onboarding use it as their own. Nothing was saved."));
        }

        [Test]
        public void Several_defaults_and_two_owners_are_all_named()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy],
                [new DefaultNamingAField(TeamsParentOverride, CausedBy), new DefaultNamingAField(PortfoliosDependency, CausedBy)],
                [Owner("Ledger", CausedBy), Owner("Card Issuing", CausedBy)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default and the Portfolios' Dependency Field default, and Ledger and Card Issuing use it as their own. Nothing was saved."));
        }

        [Test]
        public void An_owner_naming_the_field_for_several_settings_is_named_once()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved([RemovedCausedBy], [], [Owner("Merchant Onboarding", CausedBy, CausedBy)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. Merchant Onboarding uses it as its own. Nothing was saved."));
        }

        [Test]
        public void Each_removed_field_in_use_gets_its_own_sentence_and_one_that_is_not_gets_none()
        {
            var inUse = AdditionalFieldInUse.AmongTheRemoved(
                [RemovedCausedBy, new RemovedAdditionalField(7, "Release Train"), RemovedParentLink],
                [new DefaultNamingAField(TeamsParentOverride, CausedBy)],
                [Owner("Merchant Onboarding", ParentLink)]);

            Assert.That(AdditionalFieldInUse.Refusal(inUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default. \"Parent Link\" cannot be removed. Merchant Onboarding uses it as its own. Nothing was saved."));
        }

        private static OwnerNamingFields Owner(string name, params int?[] ownFieldIds) => new(name, ownFieldIds);
    }
}
