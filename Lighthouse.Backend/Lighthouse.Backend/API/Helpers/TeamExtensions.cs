using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.Helpers
{
    public static class TeamExtensions
    {
        extension(Team team)
        {
            public TeamDto CreateTeamDto(List<Portfolio> allProjects, List<Feature> allFeatures)
            {
                var teamDto = new TeamDto(team);

                var projects = allProjects.Where(p => p.Teams.Any(t => t.Id == team.Id)).Select(t => new EntityReferenceDto(t.Id, t.Name));
                var features = allFeatures.Where(f => f.FeatureWork.Exists(rw => rw.TeamId == team.Id)).Select(f => new EntityReferenceDto(f.Id, f.Name));

                teamDto.Portfolios.AddRange(projects);
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
                team.Tags = teamSetting.Tags;
                team.SystemWIPLimit = teamSetting.SystemWIPLimit;
                team.ProcessBehaviourChartBaselineStartDate = teamSetting.ProcessBehaviourChartBaselineStartDate.HasValue ? DateTime.SpecifyKind(teamSetting.ProcessBehaviourChartBaselineStartDate.Value, DateTimeKind.Utc) : null;
                team.ProcessBehaviourChartBaselineEndDate = teamSetting.ProcessBehaviourChartBaselineEndDate.HasValue ? DateTime.SpecifyKind(teamSetting.ProcessBehaviourChartBaselineEndDate.Value, DateTimeKind.Utc) : null;

                SyncStates(team, teamSetting);
                SyncServiceLevelExpectation(team, teamSetting);
                SyncBlockedItems(team, teamSetting);
            }

            public bool WorkItemRelatedSettingsChanged(TeamSettingDto teamSetting)
            {
                var queryChanged = team.DataRetrievalValue != teamSetting.DataRetrievalValue;
                var connectionChanged = team.WorkTrackingSystemConnectionId != teamSetting.WorkTrackingSystemConnectionId;
                var workItemTypesChanged = !team.WorkItemTypes.OrderBy(x => x).SequenceEqual(teamSetting.WorkItemTypes.OrderBy(x => x));
                var statesChanged =
                    !team.ToDoStates.OrderBy(x => x).SequenceEqual(teamSetting.ToDoStates.OrderBy(x => x)) ||
                    !team.DoingStates.OrderBy(x => x).SequenceEqual(teamSetting.DoingStates.OrderBy(x => x)) ||
                    !team.DoneStates.OrderBy(x => x).SequenceEqual(teamSetting.DoneStates.OrderBy(x => x));

                return queryChanged || connectionChanged || workItemTypesChanged || statesChanged;
            }
        }

        private static void SyncStates(Team team, TeamSettingDto teamSetting)
        {
            team.ToDoStates = TrimListEntries(teamSetting.ToDoStates);
            team.DoingStates = TrimListEntries(teamSetting.DoingStates);
            team.DoneStates = TrimListEntries(teamSetting.DoneStates);
        }

        private static void SyncBlockedItems(Team team, TeamSettingDto teamSetting)
        {
            team.BlockedStates = TrimListEntries(teamSetting.BlockedStates);
            team.BlockedTags = TrimListEntries(teamSetting.BlockedTags);
        }

        private static void SyncServiceLevelExpectation(Team team, TeamSettingDto teamSetting)
        {
            team.ServiceLevelExpectationProbability = teamSetting.ServiceLevelExpectationProbability;
            team.ServiceLevelExpectationRange = teamSetting.ServiceLevelExpectationRange;
        }

        private static List<string> TrimListEntries(List<string> list)
        {
            return list.Select(s => s.Trim()).ToList();
        }
    }
}