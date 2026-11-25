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

        private static readonly string[] TheBacklog = ["Backlog"];
        private static readonly string[] TheActive = ["Active"];
        private static readonly string[] TheClosed = ["Closed"];
        private static readonly string[] TheWaiting = ["Waiting"];
        private static readonly string[] TheReview = ["Review"];
        private static readonly string[] ReviewStates = ["Code Review", "QA"];
        private static readonly int[] KeptThenNumbered = [5, 6, 7];
        private static readonly int[] OneThenTwo = [1, 2];
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

        // The same older-client round trip for the Feature Owner Field.
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void An_older_clients_round_trip_keeps_the_Feature_Owner_Fields_source_and_value(bool follows, bool echoesEveryProperty)
        {
            var portfolio = APortfolioFollowingTheDefault();
            if (!follows)
            {
                portfolio.OwnFeatureOwnerField(ItsOwnField);
            }

            var before = (portfolio.FeatureOwnerFieldSource, portfolio.FeatureOwnerAdditionalFieldDefinitionId);
            var read = JsonSerializer.SerializeToNode(new PortfolioSettingDto(portfolio), AsTheApiWritesIt)!.AsObject();
            if (!echoesEveryProperty)
            {
                read.Remove("featureOwnerSource");
                read.Remove("featureOwnerDefaultName");
            }

            portfolio.SyncWithPortfolioSettings(read.Deserialize<PortfolioSettingDto>(AsTheApiWritesIt)!, Teams());

            Assert.That((portfolio.FeatureOwnerFieldSource, portfolio.FeatureOwnerAdditionalFieldDefinitionId), Is.EqualTo(before));
        }

        [Test]
        public void A_Feature_Owner_Field_owner_sent_as_following_drops_its_own_field()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.OwnFeatureOwnerField(ItsOwnField);
            var settings = SettingsNaming(null);
            settings.FeatureOwnerSource = SettingSource.ConnectionDefault;
            settings.FeatureOwnerAdditionalFieldDefinitionId = ItsOwnField;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.FeatureOwnerFieldSource, portfolio.OwnFeatureOwnerAdditionalFieldDefinitionId, portfolio.FeatureOwnerAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)null, (int?)TheConnectionsDefault)));
        }

        [Test]
        public void A_Feature_Owner_Field_follower_sent_None_as_its_own_chooses_None()
        {
            var portfolio = APortfolioFollowingTheDefault();
            var settings = SettingsNaming(null);
            settings.FeatureOwnerSource = SettingSource.Own;

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(
                (portfolio.FeatureOwnerFieldSource, portfolio.FeatureOwnerAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)null)));
        }

        [Test]
        public void A_save_following_the_Feature_Owner_Field_default_keeps_the_Portfolios_own_Owning_Team()
        {
            var ledger = new Team { Id = 4, Name = "Ledger" };
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.OwningTeam = ledger;
            var settings = SettingsNaming(null);
            settings.FeatureOwnerSource = SettingSource.ConnectionDefault;
            settings.OwningTeam = new EntityReferenceDto(ledger.Id, ledger.Name);
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(repository => repository.GetById(ledger.Id)).Returns(ledger);

            portfolio.SyncWithPortfolioSettings(settings, teams.Object);

            Assert.That(
                (portfolio.OwningTeam, portfolio.FeatureOwnerFieldSource, portfolio.FeatureOwnerAdditionalFieldDefinitionId),
                Is.EqualTo((ledger, SettingSource.ConnectionDefault, (int?)TheConnectionsDefault)));
        }

        private static readonly JsonSerializerOptions AsTheApiWritesIt = new(JsonSerializerDefaults.Web);

        [Test]
        public void A_script_saving_back_the_settings_without_Ignore_Dependencies_leaves_a_Portfolio_that_ignores_them_ignoring_them()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.IgnoreDependencies = true;

            portfolio.SyncWithPortfolioSettings(new PortfolioSettingDto { WorkTrackingSystemConnectionId = 1, IgnoreDependencies = null }, Teams());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.IgnoreDependencies, Is.True, "The connection holds no Ignore Dependencies default to take it from.");
                Assert.That(portfolio.DependencyFieldSource, Is.EqualTo(SettingSource.ConnectionDefault));
            }
        }

        [Test]
        public void Saving_settings_stores_the_baseline_dates_as_UTC_and_the_state_lists_trimmed()
        {
            var portfolio = APortfolioFollowingTheDefault();
            var settings = SettingsNaming(null);
            settings.ProcessBehaviourChartBaselineStartDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
            settings.ProcessBehaviourChartBaselineEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);
            settings.ToDoStates = [" Backlog "];
            settings.DoingStates = [" Active "];
            settings.DoneStates = [" Closed "];
            settings.WaitStates = [" Waiting "];
            settings.BlockedRuleSetJson = "{\"rules\":[]}";
            settings.StateMappings = [new StateMappingDto { Name = " Review ", States = [" Code Review ", " QA "] }];

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(portfolio.ToDoStates, Is.EqualTo(TheBacklog));
                Assert.That(portfolio.DoingStates, Is.EqualTo(TheActive));
                Assert.That(portfolio.DoneStates, Is.EqualTo(TheClosed));
                Assert.That(portfolio.WaitStates, Is.EqualTo(TheWaiting));
                Assert.That(portfolio.BlockedRuleSetJson, Is.EqualTo("{\"rules\":[]}"));
                Assert.That(portfolio.StateMappings.Select(mapping => mapping.Name), Is.EqualTo(TheReview));
                Assert.That(portfolio.StateMappings.Single().States, Is.EqualTo(ReviewStates));
            }
        }

        [Test]
        public void Saving_settings_without_baseline_dates_leaves_them_unset()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.ProcessBehaviourChartBaselineStartDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.ProcessBehaviourChartBaselineEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            portfolio.SyncWithPortfolioSettings(SettingsNaming(null), Teams());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.Null);
                Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.Null);
            }
        }

        [Test]
        public void Saving_cycle_time_definitions_keeps_known_ids_and_numbers_the_rest_after_the_highest()
        {
            var portfolio = APortfolioFollowingTheDefault();
            portfolio.CycleTimeDefinitions =
            [
                new CycleTimeDefinition { Id = 3, Name = "Old", StartState = "A", EndState = "B" },
                new CycleTimeDefinition { Id = 5, Name = "Kept", StartState = "A", EndState = "B" },
            ];
            var settings = SettingsNaming(null);
            settings.CycleTimeDefinitions =
            [
                new CycleTimeDefinitionDto { Id = 5, Name = " Kept ", StartState = " Active ", EndState = " Closed " },
                new CycleTimeDefinitionDto { Id = 0, Name = "New", StartState = "A", EndState = "B" },
                new CycleTimeDefinitionDto { Id = 9, Name = "Unknown id", StartState = "A", EndState = "B" },
            ];

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.CycleTimeDefinitions.Select(definition => definition.Id), Is.EqualTo(KeptThenNumbered));
                Assert.That(portfolio.CycleTimeDefinitions[0].Name, Is.EqualTo("Kept"));
                Assert.That(portfolio.CycleTimeDefinitions[0].StartState, Is.EqualTo("Active"));
                Assert.That(portfolio.CycleTimeDefinitions[0].EndState, Is.EqualTo("Closed"));
            }
        }

        [Test]
        public void Saving_cycle_time_definitions_to_a_portfolio_with_none_numbers_them_from_one()
        {
            var portfolio = APortfolioFollowingTheDefault();
            var settings = SettingsNaming(null);
            settings.CycleTimeDefinitions =
            [
                new CycleTimeDefinitionDto { Id = 0, Name = "First", StartState = "A", EndState = "B" },
                new CycleTimeDefinitionDto { Id = 0, Name = "Second", StartState = "A", EndState = "B" },
            ];

            portfolio.SyncWithPortfolioSettings(settings, Teams());

            Assert.That(portfolio.CycleTimeDefinitions.Select(definition => definition.Id), Is.EqualTo(OneThenTwo));
        }

        private static Portfolio APortfolioFollowingTheDefault() => new()
        {
            WorkTrackingSystemConnectionId = 1,
            WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1, ParentOverrideForPortfolios = TheConnectionsDefault, DependencyFieldForPortfolios = TheConnectionsDefault, SizeEstimateFieldForPortfolios = TheConnectionsDefault, FeatureOwnerFieldForPortfolios = TheConnectionsDefault },
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
