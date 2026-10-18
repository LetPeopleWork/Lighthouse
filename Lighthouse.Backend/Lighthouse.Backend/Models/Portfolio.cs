using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class Portfolio : WorkTrackingSystemOptionsOwner
    {
        public const int DefaultPercentileHistoryInDays = 90;

        public override List<string> WorkItemTypes { get; set; } = ["Epic"];

        public List<Team> Teams => GetTeams();

        public List<Feature> Features { get; } = [];

        public int DefaultAmountOfWorkItemsPerFeature { get; set; } = 25;

        public int? OwningTeamId { get; set; }

        public Team? OwningTeam { get; set; }

        public int? FeatureOwnerAdditionalFieldDefinitionId { get; set; }

        /// <summary>The Additional Field the connectors read a Feature's estimated size from, as this Portfolio answers it.</summary>
        [NotMapped]
        public int? SizeEstimateAdditionalFieldDefinitionId => SizeEstimateField.ReadWith(SizeEstimateFieldConnectionDefault);

        [Column(nameof(SizeEstimateAdditionalFieldDefinitionId))]
        public int? OwnSizeEstimateAdditionalFieldDefinitionId { get; private set; }

        private SettingSource storedSizeEstimateFieldSource = SettingSource.ConnectionDefault;

        [NotMapped]
        public SettingSource SizeEstimateFieldSource => storedSizeEstimateFieldSource;

        [NotMapped]
        public bool FollowsSizeEstimateFieldDefault => SizeEstimateField.FollowsTheDefault;

        /// <summary>
        /// The Additional Field (or link type) the connectors read a Feature's dependencies from, as this Portfolio
        /// answers it: its own choice, or the connection's default while it follows. Null means the tracker's own link.
        /// </summary>
        [NotMapped]
        public int? DependencyOverrideAdditionalFieldDefinitionId => DependencyField.ReadWith(DependencyFieldConnectionDefault);

        // The column keeps its old name so that renaming what the code reads costs no schema change.
        [Column(nameof(DependencyOverrideAdditionalFieldDefinitionId))]
        public int? OwnDependencyOverrideAdditionalFieldDefinitionId { get; private set; }

        private SettingSource storedDependencyFieldSource = SettingSource.ConnectionDefault;

        [NotMapped]
        public SettingSource DependencyFieldSource => storedDependencyFieldSource;

        [NotMapped]
        public bool FollowsDependencyFieldDefault => DependencyField.FollowsTheDefault;

        /// <summary>The field or link the connection's default names, shown whichever source the Portfolio chose.</summary>
        [NotMapped]
        public string? DependencyFieldDefaultName => WorkTrackingSystemConnection?.DisplayNameOfField(DependencyFieldConnectionDefault);

        public bool IgnoreDependencies { get; set; }

        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        public int? PercentileHistoryInDays { get; set; } = DefaultPercentileHistoryInDays;

        public int DefaultWorkItemPercentile { get; set; } = 85;

        public override int DoneItemsCutoffDays { get; set; } = 365;

        public override int StalenessThresholdDays { get; set; }

        public override int BlockedStalenessThresholdDays { get; set; }

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForPortfolios;

        public override bool FollowsConnectionDefault(ConnectionDefaultSetting setting) => setting switch
        {
            ConnectionDefaultSetting.ParentOverrideForPortfolios => FollowsParentOverrideDefault,
            ConnectionDefaultSetting.DependencyFieldForPortfolios => FollowsDependencyFieldDefault,
            ConnectionDefaultSetting.SizeEstimateFieldForPortfolios => FollowsSizeEstimateFieldDefault,
            _ => false,
        };

        public void FollowDependencyFieldDefault() => DependencyField = StoredSetting.FollowingTheDefault;

        /// <param name="additionalFieldDefinitionId">Null is a deliberate None: the tracker's own link, whatever the default says.</param>
        public void OwnDependencyField(int? additionalFieldDefinitionId) => DependencyField = StoredSetting.Owning(additionalFieldDefinitionId);

        /// <summary>The field this Portfolio would hold as its own once the sent setting is taken; null when it would follow, or choose None.</summary>
        public int? DependencyFieldOwnedOnceTaken(SettingSource? sentSource, int? sentField)
            => DependencyField.OwnedOnceTaking(sentSource, sentField, DependencyFieldConnectionDefault);

        public void TakeDependencyFieldAsSent(SettingSource? sentSource, int? sentField)
            => DependencyField = DependencyField.Taking(sentSource, sentField, DependencyFieldConnectionDefault);

        private int? DependencyFieldConnectionDefault => WorkTrackingSystemConnection?.DependencyFieldForPortfolios;

        public void FollowSizeEstimateFieldDefault() => SizeEstimateField = StoredSetting.FollowingTheDefault;

        /// <param name="additionalFieldDefinitionId">Null is a deliberate None: no size estimate is read, whatever the default says.</param>
        public void OwnSizeEstimateField(int? additionalFieldDefinitionId) => SizeEstimateField = StoredSetting.Owning(additionalFieldDefinitionId);

        public void TakeSizeEstimateFieldAsSent(SettingSource? sentSource, int? sentField)
            => SizeEstimateField = SizeEstimateField.Taking(sentSource, sentField, SizeEstimateFieldConnectionDefault);

        private int? SizeEstimateFieldConnectionDefault => WorkTrackingSystemConnection?.SizeEstimateFieldForPortfolios;

        [NotMapped]
        private StoredSetting SizeEstimateField
        {
            get => new(storedSizeEstimateFieldSource, OwnSizeEstimateAdditionalFieldDefinitionId);
            set => (storedSizeEstimateFieldSource, OwnSizeEstimateAdditionalFieldDefinitionId) = (value.Source, value.OwnValue);
        }

        [NotMapped]
        private StoredSetting DependencyField
        {
            get => new(storedDependencyFieldSource, OwnDependencyOverrideAdditionalFieldDefinitionId);
            set => (storedDependencyFieldSource, OwnDependencyOverrideAdditionalFieldDefinitionId) = (value.Source, value.OwnValue);
        }

        public void UpdateFeatures(IEnumerable<Feature> features)
        {
            Features.Clear();
            Features.AddRange(features);
        }

        public IEnumerable<Feature> GetFeaturesToExtrapolate()
        {
            return Features.Where(feature => feature.StateCategory != StateCategories.Done && feature.FeatureWork.Sum(x => x.TotalWorkItems) == 0);
        }

        public IEnumerable<Feature> GetFeaturesToOverrideWithDefaultSize()
        {
            return Features.Where(f => OverrideRealChildCountStates.Contains(f.State));
        }

        private List<Team> GetTeams() => Features
            .SelectMany(f => f.FeatureWork)
            .Select(fw => fw.Team)
            .Where(t => t != null)
            .DistinctBy(t => t.Id)
            .ToList();
    }
}
