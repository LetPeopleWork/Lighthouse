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

        public int? SizeEstimateAdditionalFieldDefinitionId { get; set; }

        /// <summary>
        /// The Additional Field (or link type) the connectors read a Feature's dependencies from, as this Portfolio
        /// answers it: its own choice, or the connection's default while it follows. Null means the tracker's own link.
        /// </summary>
        [NotMapped]
        public int? DependencyOverrideAdditionalFieldDefinitionId
            => EffectiveSetting.Of(DependencyFieldSource, OwnDependencyOverrideAdditionalFieldDefinitionId, WorkTrackingSystemConnection?.DependencyFieldForPortfolios);

        // The column keeps its old name so that renaming what the code reads costs no schema change.
        [Column(nameof(DependencyOverrideAdditionalFieldDefinitionId))]
        public int? OwnDependencyOverrideAdditionalFieldDefinitionId { get; private set; }

        private SettingSource storedDependencyFieldSource = SettingSource.ConnectionDefault;

        [NotMapped]
        public SettingSource DependencyFieldSource => storedDependencyFieldSource;

        [NotMapped]
        public bool FollowsDependencyFieldDefault => DependencyFieldSource == SettingSource.ConnectionDefault;

        /// <summary>The field or link the connection's default names, shown whichever source the Portfolio chose.</summary>
        [NotMapped]
        public string? DependencyFieldDefaultName => WorkTrackingSystemConnection?.DisplayNameOfField(WorkTrackingSystemConnection.DependencyFieldForPortfolios);

        public bool IgnoreDependencies { get; set; }

        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        public int? PercentileHistoryInDays { get; set; } = DefaultPercentileHistoryInDays;

        public int DefaultWorkItemPercentile { get; set; } = 85;

        public override int DoneItemsCutoffDays { get; set; } = 365;

        public override int StalenessThresholdDays { get; set; }

        public override int BlockedStalenessThresholdDays { get; set; }

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForPortfolios;

        public void FollowDependencyFieldDefault()
        {
            storedDependencyFieldSource = SettingSource.ConnectionDefault;
            OwnDependencyOverrideAdditionalFieldDefinitionId = null;
        }

        /// <param name="additionalFieldDefinitionId">Null is a deliberate None: the tracker's own link, whatever the default says.</param>
        public void OwnDependencyField(int? additionalFieldDefinitionId)
        {
            storedDependencyFieldSource = SettingSource.Own;
            OwnDependencyOverrideAdditionalFieldDefinitionId = additionalFieldDefinitionId;
        }

        /// <summary>The field this Portfolio would hold as its own once the sent setting is taken; null when it would follow, or choose None.</summary>
        public int? DependencyFieldOwnedOnceTaken(SettingSource? sentSource, int? sentField)
        {
            var applied = DependencyFieldAsSent(sentSource, sentField);
            return applied.Source == SettingSource.Own ? applied.OwnValue : null;
        }

        public void TakeDependencyFieldAsSent(SettingSource? sentSource, int? sentField)
        {
            var applied = DependencyFieldAsSent(sentSource, sentField);

            if (applied == StoredDependencyField)
            {
                return;
            }

            if (applied.Source == SettingSource.ConnectionDefault)
            {
                FollowDependencyFieldDefault();
            }
            else
            {
                OwnDependencyField(applied.OwnValue);
            }
        }

        private StoredSetting StoredDependencyField => new(DependencyFieldSource, OwnDependencyOverrideAdditionalFieldDefinitionId);

        private StoredSetting DependencyFieldAsSent(SettingSource? sentSource, int? sentField)
            => IncomingSetting.AppliedTo(StoredDependencyField, DependencyOverrideAdditionalFieldDefinitionId, sentSource, sentField);

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
