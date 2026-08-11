using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;

namespace Lighthouse.Backend.API.Helpers
{
    public static class TeamExtensions
    {
        extension(Team team)
        {
            public TeamDto CreateTeamDto(List<Portfolio> allPortfolios, DateOnly today, ISet<int>? readablePortfolioIds = null)
            {
                var teamDto = new TeamDto(team, today);

                var portfolios = allPortfolios.Where(p => p.Teams.Any(t => t.Id == team.Id));
                if (readablePortfolioIds is not null)
                {
                    portfolios = portfolios.Where(p => readablePortfolioIds.Contains(p.Id));
                }

                var visiblePortfolios = portfolios.ToList();
                var portfolioReferences = visiblePortfolios.Select(t => new EntityReferenceDto(t.Id, t.Name));
                var features = visiblePortfolios
                    .SelectMany(f => f.Features)
                    .Where(f => f.FeatureWork.Exists(rw => rw.TeamId == team.Id))
                    .Select(f => new EntityReferenceDto(f.Id, f.Name));

                teamDto.Portfolios.AddRange(portfolioReferences);
                teamDto.Features.AddRange(features);
                return teamDto;
            }

            public void SyncTeamWithTeamSettings(TeamSettingDto teamSetting)
            {
                team.Name = teamSetting.Name;
                team.DataRetrievalValue = teamSetting.DataRetrievalValue;
                team.ParentOverrideAdditionalFieldDefinitionId = teamSetting.ParentOverrideAdditionalFieldDefinitionId;
                team.FeatureWIP = teamSetting.FeatureWIP;
                team.UseFixedDatesForThroughput = teamSetting.UseFixedDatesForThroughput;
                team.ThroughputHistory = teamSetting.ThroughputHistory;
                team.ThroughputHistoryStartDate = teamSetting.ThroughputHistoryStartDate.HasValue ? DateTime.SpecifyKind(teamSetting.ThroughputHistoryStartDate.Value, DateTimeKind.Utc) : null;
                team.ThroughputHistoryEndDate = teamSetting.ThroughputHistoryEndDate.HasValue ? DateTime.SpecifyKind(teamSetting.ThroughputHistoryEndDate.Value, DateTimeKind.Utc) : null;
                team.WorkItemTypes = teamSetting.WorkItemTypes;
                team.WorkTrackingSystemConnectionId = teamSetting.WorkTrackingSystemConnectionId;
                team.AutomaticallyAdjustFeatureWIP = teamSetting.AutomaticallyAdjustFeatureWIP;
                team.DoneItemsCutoffDays = teamSetting.DoneItemsCutoffDays;
                team.EstimationAdditionalFieldDefinitionId = teamSetting.EstimationAdditionalFieldDefinitionId;
                team.EstimationUnit = teamSetting.EstimationUnit;
                team.UseNonNumericEstimation = teamSetting.UseNonNumericEstimation;
                team.EstimationCategoryValues = teamSetting.EstimationCategoryValues;
                team.SystemWIPLimit = teamSetting.SystemWIPLimit;
                team.ProcessBehaviourChartBaselineStartDate = teamSetting.ProcessBehaviourChartBaselineStartDate.HasValue ? DateTime.SpecifyKind(teamSetting.ProcessBehaviourChartBaselineStartDate.Value, DateTimeKind.Utc) : null;
                team.ProcessBehaviourChartBaselineEndDate = teamSetting.ProcessBehaviourChartBaselineEndDate.HasValue ? DateTime.SpecifyKind(teamSetting.ProcessBehaviourChartBaselineEndDate.Value, DateTimeKind.Utc) : null;
                team.ForecastFilterRuleSetJson = teamSetting.ForecastFilterRuleSetJson;
                team.StalenessThresholdDays = teamSetting.StalenessThresholdDays;
                team.BlockedStalenessThresholdDays = teamSetting.BlockedStalenessThresholdDays;

                SyncStates(team, teamSetting);
                SyncStateMappings(team, teamSetting);
                SyncServiceLevelExpectation(team, teamSetting);
                SyncBlockedItems(team, teamSetting);
                SyncWaitStates(team, teamSetting);
                SyncCycleTimeDefinitions(team, teamSetting);
            }

            /// <summary>
            /// Whether this edit means the team has to start from nothing. Driven by
            /// <see cref="FetchFingerprint.PropertiesThatAlsoCostAFreshStart"/> so the save path and the
            /// fingerprint read one list rather than two that drift apart.
            /// </summary>
            public bool WorkItemRelatedSettingsChanged(TeamSettingDto teamSetting)
                => FetchFingerprint.PropertiesThatAlsoCostAFreshStart.Any(property => TheEditChanges(team, property, teamSetting));
        }

        /// <summary>A property nobody registered purges anyway: when the answer is unknown, take the expensive one.</summary>
        private static bool TheEditChanges(Team team, string property, TeamSettingDto teamSetting) => property switch
        {
            nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId)
                => team.WorkTrackingSystemConnectionId != teamSetting.WorkTrackingSystemConnectionId,
            _ => true,
        };

        private static void SyncStates(Team team, TeamSettingDto teamSetting)
        {
            team.ToDoStates = TrimListEntries(teamSetting.ToDoStates);
            team.DoingStates = TrimListEntries(teamSetting.DoingStates);
            team.DoneStates = TrimListEntries(teamSetting.DoneStates);
        }

        private static void SyncBlockedItems(Team team, TeamSettingDto teamSetting)
        {
            team.BlockedRuleSetJson = teamSetting.BlockedRuleSetJson;
        }

        private static void SyncWaitStates(Team team, TeamSettingDto teamSetting)
        {
            team.WaitStates = TrimListEntries(teamSetting.WaitStates);
        }

        private static void SyncServiceLevelExpectation(Team team, TeamSettingDto teamSetting)
        {
            team.ServiceLevelExpectationProbability = teamSetting.ServiceLevelExpectationProbability;
            team.ServiceLevelExpectationRange = teamSetting.ServiceLevelExpectationRange;
        }

        private static void SyncStateMappings(Team team, TeamSettingDto teamSetting)
        {
            team.StateMappings = teamSetting.StateMappings
                .Select(dto => new StateMapping
                {
                    Name = dto.Name.Trim(),
                    States = dto.States.Select(s => s.Trim()).ToList()
                })
                .ToList();
        }

        private static void SyncCycleTimeDefinitions(Team team, TeamSettingDto teamSetting)
        {
            var existingIds = team.CycleTimeDefinitions.Select(definition => definition.Id).ToHashSet();
            var nextId = existingIds.Count == 0 ? 1 : existingIds.Max() + 1;

            team.CycleTimeDefinitions = teamSetting.CycleTimeDefinitions
                .Select(dto => new CycleTimeDefinition
                {
                    Id = dto.Id > 0 && existingIds.Contains(dto.Id) ? dto.Id : nextId++,
                    Name = dto.Name.Trim(),
                    StartState = dto.StartState.Trim(),
                    EndState = dto.EndState.Trim(),
                })
                .ToList();
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }
    }
}