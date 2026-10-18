using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        // An older client reads the settings and sends them back, either echoing every property or dropping
        // the ones it does not know. Neither may move the Dependency Field off what the Portfolio reads.
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void An_older_clients_round_trip_keeps_the_Dependency_Fields_source_and_value(bool follows, bool echoesEveryProperty)
        {
            var portfolio = APortfolioFollowingTheDefault();
            if (!follows)
            {
                portfolio.OwnDependencyField(ItsOwnField);
            }

            var before = (portfolio.DependencyFieldSource, portfolio.DependencyOverrideAdditionalFieldDefinitionId);
            var read = JsonSerializer.SerializeToNode(new PortfolioSettingDto(portfolio), AsTheApiWritesIt)!.AsObject();
            if (!echoesEveryProperty)
            {
                read.Remove("dependencyFieldSource");
                read.Remove("dependencyFieldDefaultName");
            }

            portfolio.SyncWithPortfolioSettings(read.Deserialize<PortfolioSettingDto>(AsTheApiWritesIt)!, Teams());

            Assert.That((portfolio.DependencyFieldSource, portfolio.DependencyOverrideAdditionalFieldDefinitionId), Is.EqualTo(before));
        }

        [Test]
        public void A_Dependency_Field_owner_sent_as_following_drops_its_own_field()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.OwnDependencyField(ItsOwnField);
            var settings = SettingsNaming(null);
            settings.DependencyFieldSource = SettingSource.ConnectionDefault;
            settings.DependencyOverrideAdditionalFieldDefinitionId = ItsOwnField;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.DependencyFieldSource, portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId, portfolio.DependencyOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)null, (int?)TheConnectionsDefault)));
        }

        [Test]
        public void A_Dependency_Field_follower_sent_None_as_its_own_chooses_None()
        {
            var portfolio = APortfolioFollowingTheDefault();
            var settings = SettingsNaming(null);
            settings.DependencyFieldSource = SettingSource.Own;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.DependencyFieldSource, portfolio.DependencyOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)null)));
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void An_older_clients_round_trip_keeps_the_Size_Estimate_Fields_source_and_value(bool follows, bool echoesEveryProperty)
        {
            var portfolio = APortfolioFollowingTheDefault();
            if (!follows)
            {
                portfolio.OwnSizeEstimateField(ItsOwnField);
            }

            var before = (portfolio.SizeEstimateFieldSource, portfolio.SizeEstimateAdditionalFieldDefinitionId);
            var read = JsonSerializer.SerializeToNode(new PortfolioSettingDto(portfolio), AsTheApiWritesIt)!.AsObject();
            if (!echoesEveryProperty)
            {
                read.Remove("sizeEstimateSource");
                read.Remove("sizeEstimateDefaultName");
            }

            portfolio.SyncWithPortfolioSettings(read.Deserialize<PortfolioSettingDto>(AsTheApiWritesIt)!, Teams());

            Assert.That((portfolio.SizeEstimateFieldSource, portfolio.SizeEstimateAdditionalFieldDefinitionId), Is.EqualTo(before));
        }

        [Test]
        public void A_Size_Estimate_Field_owner_sent_as_following_drops_its_own_field()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.OwnSizeEstimateField(ItsOwnField);
            var settings = SettingsNaming(null);
            settings.SizeEstimateSource = SettingSource.ConnectionDefault;
            settings.SizeEstimateAdditionalFieldDefinitionId = ItsOwnField;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.SizeEstimateFieldSource, portfolio.OwnSizeEstimateAdditionalFieldDefinitionId, portfolio.SizeEstimateAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)null, (int?)TheConnectionsDefault)));
        }

        [Test]
        public void A_Size_Estimate_Field_follower_sent_None_as_its_own_chooses_None()
        {
            var portfolio = APortfolioFollowingTheDefault();
            var settings = SettingsNaming(null);
            settings.SizeEstimateSource = SettingSource.Own;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.SizeEstimateFieldSource, portfolio.SizeEstimateAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)null)));
        }

        private static readonly JsonSerializerOptions AsTheApiWritesIt = new(JsonSerializerDefaults.Web);

        private static Portfolio APortfolioFollowingTheDefault() => new()
        {
            WorkTrackingSystemConnectionId = 1,
            WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1, ParentOverrideForPortfolios = TheConnectionsDefault, DependencyFieldForPortfolios = TheConnectionsDefault, SizeEstimateFieldForPortfolios = TheConnectionsDefault },
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
