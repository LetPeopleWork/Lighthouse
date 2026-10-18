using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class PortfolioExtensionsTest
    {
        private const int TheConnectionsDefault = 9;
        private const int ItsOwnField = 5;
        private const int AnotherField = 7;

        [Test]
        public void A_follower_saved_back_without_its_source_or_a_field_keeps_following()
        {
            var portfolio = APortfolioFollowingTheDefault();

            portfolio.SyncWithPortfolioSettings(SettingsNaming(null), Teams());

            Assert.That(
                (portfolio.ParentOverrideSource, portfolio.ParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)TheConnectionsDefault)));
        }

        [Test]
        public void A_follower_saved_back_without_its_source_echoing_the_default_keeps_following()
        {
            var portfolio = APortfolioFollowingTheDefault();

            portfolio.SyncWithPortfolioSettings(SettingsNaming(TheConnectionsDefault), Teams());

            Assert.That(portfolio.ParentOverrideSource, Is.EqualTo(SettingSource.ConnectionDefault));
        }

        [Test]
        public void An_owner_saved_back_without_its_source_or_a_field_keeps_its_own_field()
        {
            var portfolio = APortfolioOwning(ItsOwnField);

            portfolio.SyncWithPortfolioSettings(SettingsNaming(null), Teams());

            Assert.That(
                (portfolio.ParentOverrideSource, portfolio.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)ItsOwnField)));
        }

        [Test]
        public void A_follower_sent_another_field_without_a_source_owns_it()
        {
            var portfolio = APortfolioFollowingTheDefault();

            portfolio.SyncWithPortfolioSettings(SettingsNaming(AnotherField), Teams());

            Assert.That(
                (portfolio.ParentOverrideSource, portfolio.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)AnotherField)));
        }

        [Test]
        public void An_owner_sent_as_following_drops_its_own_field()
        {
            var portfolio = APortfolioOwning(ItsOwnField);
            var settings = SettingsNaming(ItsOwnField);
            settings.ParentOverrideSource = SettingSource.ConnectionDefault;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.ParentOverrideSource, portfolio.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)null)));
        }

        [Test]
        public void An_owner_sent_None_as_its_own_chooses_None()
        {
            var portfolio = APortfolioOwning(ItsOwnField);
            var settings = SettingsNaming(null);
            settings.ParentOverrideSource = SettingSource.Own;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.ParentOverrideSource, portfolio.ParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)null)));
        }

        private static Portfolio APortfolioFollowingTheDefault() => new()
        {
            WorkTrackingSystemConnectionId = 1,
            WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1, ParentOverrideForPortfolios = TheConnectionsDefault },
        };

        private static Portfolio APortfolioOwning(int field)
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.OwnParentOverride(field);
            return portfolio;
        }

        private static PortfolioSettingDto SettingsNaming(int? field) => new()
        {
            ParentOverrideAdditionalFieldDefinitionId = field,
            WorkTrackingSystemConnectionId = 1,
        };

        private static IRepository<Team> Teams() => new Mock<IRepository<Team>>().Object;
    }
}
