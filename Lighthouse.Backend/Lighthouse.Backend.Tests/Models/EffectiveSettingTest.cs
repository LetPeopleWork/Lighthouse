using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class EffectiveSettingTest
    {
        private const int TheOwnersOwnField = 5;
        private const int TheConnectionsDefault = 9;
        private const int TheTeamsDefault = 13;
        private const int ThePortfoliosParentOverrideDefault = 17;

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, null, TheOwnersOwnField)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, TheConnectionsDefault, TheOwnersOwnField)]
        public void A_Team_reads_its_own_Parent_Override_Field_or_the_connections_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? connectionDefault, int? expected)
        {
            var team = new Team
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = connectionDefault },
            };

            team.OwnParentOverride(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                team.FollowParentOverrideDefault();
            }

            Assert.That(team.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_following_Team_built_without_its_connection_reads_no_field()
        {
            var team = new Team();

            team.FollowParentOverrideDefault();

            Assert.That(team.ParentOverrideAdditionalFieldDefinitionId, Is.Null);
        }

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, null, TheOwnersOwnField)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, TheConnectionsDefault, TheOwnersOwnField)]
        public void A_Portfolio_reads_its_own_Parent_Override_Field_or_the_connections_Portfolio_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? portfoliosDefault, int? expected)
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    ParentOverrideForTeams = TheTeamsDefault,
                    ParentOverrideForPortfolios = portfoliosDefault,
                },
            };

            portfolio.OwnParentOverride(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                portfolio.FollowParentOverrideDefault();
            }

            Assert.That(portfolio.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_Team_has_no_Portfolios_default_to_follow()
        {
            var team = new Team
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForPortfolios = TheConnectionsDefault },
            };

            team.FollowParentOverrideDefault();

            Assert.That(team.ParentOverrideAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void A_new_Portfolio_follows_the_connections_Portfolio_default()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForPortfolios = TheConnectionsDefault },
            };

            Assert.That(portfolio.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(TheConnectionsDefault));
        }

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, null, TheOwnersOwnField)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, TheConnectionsDefault, TheOwnersOwnField)]
        public void A_Portfolio_reads_its_own_Dependency_Field_or_the_connections_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? dependencyDefault, int? expected)
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    ParentOverrideForPortfolios = ThePortfoliosParentOverrideDefault,
                    DependencyFieldForPortfolios = dependencyDefault,
                },
            };

            portfolio.OwnDependencyField(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                portfolio.FollowDependencyFieldDefault();
            }

            Assert.That(portfolio.DependencyOverrideAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_new_Portfolio_follows_the_connections_Dependency_Field_default()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { DependencyFieldForPortfolios = TheConnectionsDefault },
            };

            Assert.That(portfolio.DependencyOverrideAdditionalFieldDefinitionId, Is.EqualTo(TheConnectionsDefault));
        }

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, null, TheOwnersOwnField)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, TheConnectionsDefault, TheOwnersOwnField)]
        public void A_Portfolio_reads_its_own_Size_Estimate_Field_or_the_connections_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? sizeEstimateDefault, int? expected)
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    ParentOverrideForPortfolios = ThePortfoliosParentOverrideDefault,
                    DependencyFieldForPortfolios = ThePortfoliosParentOverrideDefault,
                    SizeEstimateFieldForPortfolios = sizeEstimateDefault,
                },
            };

            portfolio.OwnSizeEstimateField(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                portfolio.FollowSizeEstimateFieldDefault();
            }

            Assert.That(portfolio.SizeEstimateAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_new_Portfolio_follows_the_connections_Size_Estimate_Field_default()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { SizeEstimateFieldForPortfolios = TheConnectionsDefault },
            };

            Assert.That(portfolio.SizeEstimateAdditionalFieldDefinitionId, Is.EqualTo(TheConnectionsDefault));
        }

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheOwnersOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, null, TheOwnersOwnField)]
        [TestCase(SettingSource.Own, TheOwnersOwnField, TheConnectionsDefault, TheOwnersOwnField)]
        public void A_Portfolio_reads_its_own_Feature_Owner_Field_or_the_connections_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? featureOwnerDefault, int? expected)
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    ParentOverrideForPortfolios = ThePortfoliosParentOverrideDefault,
                    DependencyFieldForPortfolios = ThePortfoliosParentOverrideDefault,
                    SizeEstimateFieldForPortfolios = ThePortfoliosParentOverrideDefault,
                    FeatureOwnerFieldForPortfolios = featureOwnerDefault,
                },
            };

            portfolio.OwnFeatureOwnerField(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                portfolio.FollowFeatureOwnerFieldDefault();
            }

            Assert.That(portfolio.FeatureOwnerAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_new_Portfolio_follows_the_connections_Feature_Owner_Field_default()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { FeatureOwnerFieldForPortfolios = TheConnectionsDefault },
            };

            Assert.That(portfolio.FeatureOwnerAdditionalFieldDefinitionId, Is.EqualTo(TheConnectionsDefault));
        }

        [Test]
        public void A_Portfolio_has_no_Teams_default_to_follow()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = TheConnectionsDefault },
            };

            portfolio.FollowParentOverrideDefault();

            Assert.That(portfolio.ParentOverrideAdditionalFieldDefinitionId, Is.Null);
        }
    }
}
