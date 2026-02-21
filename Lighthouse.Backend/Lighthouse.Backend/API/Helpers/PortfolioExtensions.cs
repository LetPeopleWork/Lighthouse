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
            project.SizeEstimateAdditionalFieldDefinitionId = portfolioSetting.SizeEstimateAdditionalFieldDefinitionId;
            project.OverrideRealChildCountStates = portfolioSetting.OverrideRealChildCountStates;
            project.DoneItemsCutoffDays = portfolioSetting.DoneItemsCutoffDays;

            project.WorkTrackingSystemConnectionId = portfolioSetting.WorkTrackingSystemConnectionId;
            project.Tags = portfolioSetting.Tags;
            project.FeatureOwnerAdditionalFieldDefinitionId = portfolioSetting.FeatureOwnerAdditionalFieldDefinitionId;
            project.EstimationAdditionalFieldDefinitionId = portfolioSetting.EstimationAdditionalFieldDefinitionId;
            project.ParentOverrideAdditionalFieldDefinitionId = portfolioSetting.ParentOverrideAdditionalFieldDefinitionId;
            project.SystemWIPLimit = portfolioSetting.SystemWIPLimit;
            project.ProcessBehaviourChartBaselineStartDate = portfolioSetting.ProcessBehaviourChartBaselineStartDate.HasValue ? DateTime.SpecifyKind(portfolioSetting.ProcessBehaviourChartBaselineStartDate.Value, DateTimeKind.Utc) : null;
            project.ProcessBehaviourChartBaselineEndDate = portfolioSetting.ProcessBehaviourChartBaselineEndDate.HasValue ? DateTime.SpecifyKind(portfolioSetting.ProcessBehaviourChartBaselineEndDate.Value, DateTimeKind.Utc) : null;

            SyncStates(project, portfolioSetting);
            SyncTeams(project, portfolioSetting, teamRepo);
            SyncServiceLevelExpectation(project, portfolioSetting);
            SyncBlockedItems(project, portfolioSetting);
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
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }

        private static void SyncTeams(Portfolio project, PortfolioSettingDto portfolioSetting, IRepository<Team> teamRepo)
        {
            var teams = new List<Team>();
            foreach (var teamDto in portfolioSetting.InvolvedTeams)
            {
                var team = teamRepo.GetById(teamDto.Id);
                if (team != null)
                {
                    teams.Add(team);
                }
            }

            project.UpdateTeams(teams);

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