using System.ComponentModel.DataAnnotations.Schema;
using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public abstract class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner, IConcurrencyTokenEntity
    {
        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string Name { get; set; }

        public string DataRetrievalValue { get; set; } = string.Empty;

        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// What the last cycle asked the tracker for and how it read the answer. Sync-owned, like
        /// <see cref="UpdateTime"/>: null means nobody has recorded one yet, which is how an instance
        /// upgrading into this release gets one full cycle before a cheaper one is trusted.
        /// </summary>
        public string? FetchFingerprint { get; set; }

        public abstract List<string> WorkItemTypes { get; set; }

        public List<string> ToDoStates { get; set; } = new List<string> { "New", "Proposed", "To Do" };

        public List<string> DoingStates { get; set; } = new List<string> { "Active", "Resolved", "In Progress", "Committed" };

        public List<string> DoneStates { get; set; } = new List<string> { "Done", "Closed" };

        public IEnumerable<string> AllStates => GetRawStatesForCategory(ToDoStates)
            .Union(GetRawStatesForCategory(DoingStates))
            .Union(GetRawStatesForCategory(DoneStates));

        public IEnumerable<string> OpenStates => GetRawStatesForCategory(ToDoStates)
            .Union(GetRawStatesForCategory(DoingStates));

        public int ServiceLevelExpectationProbability { get; set; } = 0;

        public int ServiceLevelExpectationRange { get; set; } = 0;

        public int SystemWIPLimit { get; set; } = 0;

        public int WorkTrackingSystemConnectionId { get; set; }

        public List<string> WaitStates { get; set; } = [];

        public string? BlockedRuleSetJson { get; set; }

        public List<StateMapping> StateMappings { get; set; } = [];

        public List<CycleTimeDefinition> CycleTimeDefinitions { get; set; } = [];

        public abstract int DoneItemsCutoffDays { get; set; }

        public virtual int StalenessThresholdDays { get; set; }

        public virtual int BlockedStalenessThresholdDays { get; set; }

        public DateTime? ProcessBehaviourChartBaselineStartDate { get; set; }

        public DateTime? ProcessBehaviourChartBaselineEndDate { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }

        /// <summary>The estimation this owner's pages read: its own bundle, or the connection's default while it follows.</summary>
        [NotMapped]
        public EstimationBundle EffectiveEstimation => Estimation.ReadWith(EstimationConnectionDefault);

        [NotMapped]
        public int? EstimationAdditionalFieldDefinitionId => EffectiveEstimation.AdditionalFieldDefinitionId;

        [NotMapped]
        public string? EstimationUnit => EffectiveEstimation.Unit;

        [NotMapped]
        public bool UseNonNumericEstimation => EffectiveEstimation.UseNonNumericEstimation;

        [NotMapped]
        public List<string> EstimationCategoryValues => [.. EffectiveEstimation.CategoryValues];

        // The columns keep their old names so that renaming what the code reads costs no schema change.
        [Column(nameof(EstimationAdditionalFieldDefinitionId))]
        public int? OwnEstimationAdditionalFieldDefinitionId { get; private set; }

        [Column(nameof(EstimationUnit))]
        public string? OwnEstimationUnit { get; private set; }

        [Column(nameof(UseNonNumericEstimation))]
        public bool OwnUseNonNumericEstimation { get; private set; }

        [Column(nameof(EstimationCategoryValues))]
        public List<string> OwnEstimationCategoryValues { get; private set; } = [];

        private SettingSource storedEstimationSource = SettingSource.ConnectionDefault;

        [NotMapped]
        public SettingSource EstimationSource => storedEstimationSource;

        [NotMapped]
        public bool FollowsEstimationDefault => Estimation.FollowsTheDefault;

        protected virtual EstimationBundle EstimationConnectionDefault => EstimationBundle.None;

        /// <summary>The field and unit of the connection's Estimation default, shown whichever source the owner chose.</summary>
        [NotMapped]
        public string? EstimationDefaultName
            => WorkTrackingSystemConnection?.DisplayNameOfField(EstimationConnectionDefault.AdditionalFieldDefinitionId) is { } fieldName
                ? EstimationConnectionDefault.NamedWith(fieldName)
                : null;

        public void FollowEstimationDefault() => Estimation = StoredEstimation.FollowingTheDefault;

        /// <param name="bundle">A bundle without a field is a deliberate None: no estimation, whatever the default says.</param>
        public void OwnEstimation(EstimationBundle bundle) => Estimation = StoredEstimation.Owning(bundle);

        /// <summary>The Estimation Field this owner would hold as its own once the sent bundle is taken; null when it would follow, or own none.</summary>
        public int? EstimationOwnedOnceTaken(SettingSource? sentSource, EstimationBundle sentBundle)
            => Estimation.OwnedOnceTaking(sentSource, sentBundle, EstimationConnectionDefault);

        public void TakeEstimationAsSent(SettingSource? sentSource, EstimationBundle sentBundle)
            => Estimation = Estimation.Taking(sentSource, sentBundle, EstimationConnectionDefault);

        [NotMapped]
        private StoredEstimation Estimation
        {
            get => new(storedEstimationSource, new EstimationBundle(OwnEstimationAdditionalFieldDefinitionId, OwnEstimationUnit, OwnUseNonNumericEstimation, OwnEstimationCategoryValues));
            set
            {
                storedEstimationSource = value.Source;
                OwnEstimationAdditionalFieldDefinitionId = value.OwnBundle.AdditionalFieldDefinitionId;
                OwnEstimationUnit = value.OwnBundle.Unit;
                OwnUseNonNumericEstimation = value.OwnBundle.UseNonNumericEstimation;
                OwnEstimationCategoryValues = [.. value.OwnBundle.CategoryValues];
            }
        }

        /// <summary>
        /// The Additional Field the connectors read an item's parent from, as this owner answers it: its
        /// own choice, or the connection's default while it follows. Null means the tracker's own parent.
        /// </summary>
        [NotMapped]
        public int? ParentOverrideAdditionalFieldDefinitionId => ParentOverride.ReadWith(ParentOverrideConnectionDefault);

        // The column keeps its old name so that renaming what the code reads costs no schema change.
        [Column(nameof(ParentOverrideAdditionalFieldDefinitionId))]
        public int? OwnParentOverrideAdditionalFieldDefinitionId { get; private set; }

        // Stored in each owner's own table, and a new owner follows the connection's default.
        private SettingSource storedParentOverrideSource = SettingSource.ConnectionDefault;

        [NotMapped]
        public SettingSource ParentOverrideSource => storedParentOverrideSource;

        [NotMapped]
        public bool FollowsParentOverrideDefault => ParentOverride.FollowsTheDefault;

        protected virtual int? ParentOverrideConnectionDefault => null;

        /// <summary>The field the connection's default names, shown whichever source the owner chose.</summary>
        [NotMapped]
        public string? ParentOverrideDefaultName => WorkTrackingSystemConnection?.DisplayNameOfField(ParentOverrideConnectionDefault);

        public void FollowParentOverrideDefault() => ParentOverride = StoredSetting.FollowingTheDefault;

        /// <param name="additionalFieldDefinitionId">Null is a deliberate None: the tracker's own parent, whatever the default says.</param>
        public void OwnParentOverride(int? additionalFieldDefinitionId) => ParentOverride = StoredSetting.Owning(additionalFieldDefinitionId);

        /// <summary>The field this owner would hold as its own once the sent setting is taken; null when it would follow, or choose None.</summary>
        public int? ParentOverrideOwnedOnceTaken(SettingSource? sentSource, int? sentField)
            => ParentOverride.OwnedOnceTaking(sentSource, sentField, ParentOverrideConnectionDefault);

        public void TakeParentOverrideAsSent(SettingSource? sentSource, int? sentField)
            => ParentOverride = ParentOverride.Taking(sentSource, sentField, ParentOverrideConnectionDefault);

        /// <summary>Whether this owner reads the given connection default; false for every default its kind is not offered.</summary>
        public virtual bool FollowsConnectionDefault(ConnectionDefaultSetting setting) => false;

        [NotMapped]
        private StoredSetting ParentOverride
        {
            get => new(storedParentOverrideSource, OwnParentOverrideAdditionalFieldDefinitionId);
            set => (storedParentOverrideSource, OwnParentOverrideAdditionalFieldDefinitionId) = (value.Source, value.OwnValue);
        }

        public StateCategories MapStateToStateCategory(string state)
        {
            if (GetRawStatesForCategory(ToDoStates).IsItemInList(state))
            {
                return StateCategories.ToDo;
            }

            if (GetRawStatesForCategory(DoingStates).IsItemInList(state))
            {
                return StateCategories.Doing;
            }

            if (GetRawStatesForCategory(DoneStates).IsItemInList(state))
            {
                return StateCategories.Done;
            }

            return StateCategories.Unknown;
        }

        public List<string> GetRawStatesForCategory(List<string> categoryStates)
        {
            var rawStates = new List<string>();

            foreach (var entry in categoryStates)
            {
                var mapping = StateMappings.FirstOrDefault(m =>
                    string.Equals(m.Name, entry, StringComparison.OrdinalIgnoreCase));

                if (mapping != null)
                {
                    rawStates.AddRange(mapping.States);
                }
                else
                {
                    rawStates.Add(entry);
                }
            }

            return rawStates;
        }

        public bool IsCycleTimeDefinitionValid(CycleTimeDefinition definition)
        {
            var states = AllStates.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var startRaw = GetRawStatesForCategory([definition.StartState]);
            var endRaw = GetRawStatesForCategory([definition.EndState]);

            return startRaw.Count > 0 && endRaw.Count > 0
                && startRaw.All(states.Contains) && endRaw.All(states.Contains);
        }

        public string MapRawStateToMappedName(string rawState)
        {
            var mapping = StateMappings.FirstOrDefault(m =>
                m.States.Any(s => string.Equals(s, rawState, StringComparison.OrdinalIgnoreCase)));

            return mapping?.Name ?? rawState;
        }

        public void ResetUpdateTime()
        {
            UpdateTime = DateTime.MinValue;
        }

        public void RefreshUpdateTime(DateTime now)
        {
            UpdateTime = now;
        }
    }
}
