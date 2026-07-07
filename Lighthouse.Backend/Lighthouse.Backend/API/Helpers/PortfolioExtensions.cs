using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.API.Helpers
{
    public static class PortfolioExtensions
    {
        public static void SyncWithPortfolioSettings(this Portfolio project, PortfolioSettingDto portfolioSetting, IRepository<Team> teamRepo)
        {
            project.Name = portfolioSetting.Name;
            project.WorkItemTypes = portfolioSetting.WorkItemTypes;
            project.DataRetrievalValue = portfolioSetting.DataRetrievalValue;

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = portfolioSetting.UsePercentileToCalculateDefaultAmountOfWorkItems;
            project.DefaultAmountOfWorkItemsPerFeature = portfolioSetting.DefaultAmountOfWorkItemsPerFeature;
            project.DefaultWorkItemPercentile = portfolioSetting.DefaultWorkItemPercentile;
            project.PercentileHistoryInDays = portfolioSetting.PercentileHistoryInDays;
            project.StalenessThresholdDays = portfolioSetting.StalenessThresholdDays;
            project.BlockedStalenessThresholdDays = portfolioSetting.BlockedStalenessThresholdDays;
            project.SizeEstimateAdditionalFieldDefinitionId = portfolioSetting.SizeEstimateAdditionalFieldDefinitionId;
            project.OverrideRealChildCountStates = portfolioSetting.OverrideRealChildCountStates;
            project.DoneItemsCutoffDays = portfolioSetting.DoneItemsCutoffDays;

            project.WorkTrackingSystemConnectionId = portfolioSetting.WorkTrackingSystemConnectionId;
            project.FeatureOwnerAdditionalFieldDefinitionId = portfolioSetting.FeatureOwnerAdditionalFieldDefinitionId;
            project.EstimationAdditionalFieldDefinitionId = portfolioSetting.EstimationAdditionalFieldDefinitionId;
            project.EstimationUnit = portfolioSetting.EstimationUnit;
            project.UseNonNumericEstimation = portfolioSetting.UseNonNumericEstimation;
            project.EstimationCategoryValues = portfolioSetting.EstimationCategoryValues;
            project.ParentOverrideAdditionalFieldDefinitionId = portfolioSetting.ParentOverrideAdditionalFieldDefinitionId;
            project.SystemWIPLimit = portfolioSetting.SystemWIPLimit;
            project.ProcessBehaviourChartBaselineStartDate = portfolioSetting.ProcessBehaviourChartBaselineStartDate.HasValue ? DateTime.SpecifyKind(portfolioSetting.ProcessBehaviourChartBaselineStartDate.Value, DateTimeKind.Utc) : null;
            project.ProcessBehaviourChartBaselineEndDate = portfolioSetting.ProcessBehaviourChartBaselineEndDate.HasValue ? DateTime.SpecifyKind(portfolioSetting.ProcessBehaviourChartBaselineEndDate.Value, DateTimeKind.Utc) : null;

            SyncStates(project, portfolioSetting);
            SyncStateMappings(project, portfolioSetting);
            SyncCycleTimeDefinitions(project, portfolioSetting);
            SyncTeams(project, portfolioSetting, teamRepo);
            SyncServiceLevelExpectation(project, portfolioSetting);
            SyncBlockedItems(project, portfolioSetting);
            SyncWaitStates(project, portfolioSetting);
        }

        private static void SyncStates(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.ToDoStates = TrimListEntries(portfolioSetting.ToDoStates);
            project.DoingStates = TrimListEntries(portfolioSetting.DoingStates);
            project.DoneStates = TrimListEntries(portfolioSetting.DoneStates);
        }

        private static void SyncBlockedItems(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.BlockedStates = TrimListEntries(portfolioSetting.BlockedStates);
            project.BlockedTags = portfolioSetting.BlockedTags;
            project.BlockedRuleSetJson = portfolioSetting.BlockedRuleSetJson;
        }

        private static void SyncWaitStates(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.WaitStates = TrimListEntries(portfolioSetting.WaitStates);
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }

        private static void SyncStateMappings(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.StateMappings = portfolioSetting.StateMappings
                .Select(dto => new StateMapping
                {
                    Name = dto.Name.Trim(),
                    States = dto.States.Select(s => s.Trim()).ToList()
                })
                .ToList();
        }

        private static void SyncCycleTimeDefinitions(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            var existingIds = project.CycleTimeDefinitions.Select(definition => definition.Id).ToHashSet();
            var nextId = existingIds.Count == 0 ? 1 : existingIds.Max() + 1;

            project.CycleTimeDefinitions = portfolioSetting.CycleTimeDefinitions
                .Select(dto => new CycleTimeDefinition
                {
                    Id = dto.Id > 0 && existingIds.Contains(dto.Id) ? dto.Id : nextId++,
                    Name = dto.Name.Trim(),
                    StartState = dto.StartState.Trim(),
                    EndState = dto.EndState.Trim(),
                })
                .ToList();
        }

        private static void SyncTeams(Portfolio project, PortfolioSettingDto portfolioSetting, IRepository<Team> teamRepo)
        {
            project.OwningTeam = null;
            if (portfolioSetting.OwningTeam != null)
            {
                project.OwningTeam = teamRepo.GetById(portfolioSetting.OwningTeam.Id);
            }
        }

        private static void SyncServiceLevelExpectation(Portfolio project, PortfolioSettingDto portfolioSetting)
        {
            project.ServiceLevelExpectationProbability = portfolioSetting.ServiceLevelExpectationProbability;
            project.ServiceLevelExpectationRange = portfolioSetting.ServiceLevelExpectationRange;
        }
    }
}