using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

using static Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear.LinearWorkTrackingConnector.LinearResponses;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector : IWorkTrackingConnector
    {
        private const string UnknownStateIdentifier = "Unknown";
        private const string DefaultTemplateIdentifier = "Default";

        private readonly ILogger<LinearWorkTrackingConnector> logger;
        private readonly ICryptoService cryptoService;

        public LinearWorkTrackingConnector(ILogger<LinearWorkTrackingConnector> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}", team.Name);

            try
            {
                var workItems = new List<WorkItem>();
                var issues = await GetIssuesForTeam(team);

                if (issues == null || issues.Count == 0)
                {
                    logger.LogInformation("No issues found for team {TeamName}", team.Name);
                    return workItems;
                }

                foreach (var issue in issues)
                {
                    var workItemBase = CreateWorkItemFromIssue(issue, team);
                    workItems.Add(new WorkItem(workItemBase, team));
                }

                return workItems;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting work items for team {TeamName}", team.Name);
                return [];
            }
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Features for Project {ProjectName}", project.Name);

            try
            {
                var features = new List<Feature>();
                var issues = await GetIssuesForProject(project);

                if (issues == null || issues.Count == 0)
                {
                    logger.LogInformation("No Features found for project {ProjectName}", project.Name);
                    return features;
                }

                foreach (var issue in issues)
                {
                    var workItemBase = CreateWorkItemFromIssue(issue, project);
                    features.Add(new Feature(workItemBase));
                }

                return features;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Features for Project {ProjectName}", project.Name);
                return new List<Feature>();
            }
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            var orderIndex = 0.0;

            var existingItems = existingItemsOrder
                .Select(x => double.TryParse(x, out var value) ? value : double.MaxValue)
                .Where(order => order < double.MaxValue * 0.999)
                .ToList();

            if (existingItems.Count > 0)
            {
                if (relativeOrder == RelativeOrder.Below)
                {
                    var lowestOrder = existingItems.Min();
                    orderIndex = lowestOrder - 1;
                }
                else
                {
                    var highestOrder = existingItems.Max();
                    orderIndex = highestOrder + 1;
                }
            }

            return $"{orderIndex}";
        }

        public Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            return Task.FromResult(new Dictionary<string, int>());
        }

        public Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            return Task.FromResult(new List<string>());
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                logger.LogInformation("Validating Linear connection");

                var viewerQuery = @"
                    query {
                        viewer {
                            id
                        }
                    }";

                _ = await SendQuery<ViewerResponse>(connection, viewerQuery);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate Linear connection");
                return false;
            }
        }

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", project.Name, project.WorkItemQuery);

                var issues = await GetIssuesForProject(project);

                logger.LogInformation("Found {TotalIssues} issues for project {ProjectName}",
                    issues.Count, project.Name);

                return issues.Count > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);

                var issues = await GetIssuesForTeam(team);
                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items for team {TeamName}", issues.Count, team.WorkItemQuery);

                return issues.Count > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return false;
            }
        }

        private WorkItemBase CreateWorkItemFromIssue(IssueNode issue, IWorkItemQueryOwner queryOwner)
        {
            var state = issue.State?.Name ?? UnknownStateIdentifier;

            var stateCategory = queryOwner.MapStateToStateCategory(state);

            if (issue.CompletedAt != null && issue.StartedAt == null)
            {
                issue.StartedAt = issue.CompletedAt;
            }

            var workItemBase = new WorkItemBase
            {
                ReferenceId = issue.Identifier?.ToLowerInvariant() ?? $"issue-{issue.Number}",
                Name = issue.Title,
                Type = issue.LastAppliedTemplate?.Name ?? UnknownStateIdentifier,
                State = state,
                StateCategory = stateCategory,
                Url = issue.Url,
                ParentReferenceId = issue.Parent?.Identifier?.ToLowerInvariant() ?? string.Empty,
                Order = issue.SortOrder.ToString(),
                CreatedDate = issue.CreatedAt,
                StartedDate = issue.StartedAt,
                ClosedDate = issue.CompletedAt,
            };

            return workItemBase;
        }

        private async Task<List<IssueNode>> GetAllIssuesForTeam(WorkTrackingSystemConnection connection, string teamId)
        {
            var issues = new List<IssueNode>();

            await GetWithPagination<TeamResponse>(connection, cursorParam => GetIssuesForTeamQuery(teamId, cursorParam), teamResponse =>
            {
                var fetchedIssues = teamResponse?.Team?.Issues?.Nodes ?? [];
                issues.AddRange(fetchedIssues);

                return true;
            });

            return MapIssuesWithoutTemplateToDefault(issues);
        }

        private async Task<List<IssueNode>> GetAllIssuesForProject(WorkTrackingSystemConnection connection, string projectId)
        {
            var issues = new List<IssueNode>();

            await GetWithPagination<ProjectResponse>(connection, cursorParam => GetIssuesForProjectQuery(projectId, cursorParam), projectResponse =>
            {
                var fetchedIssues = projectResponse?.Project?.Issues?.Nodes ?? [];
                issues.AddRange(fetchedIssues);

                return true;
            });

            return MapIssuesWithoutTemplateToDefault(
                    FilterIssuesThatAreChildrenOfProjectIssues(issues)
                );
        }

        private List<IssueNode> MapIssuesWithoutTemplateToDefault(List<IssueNode> issues)
        {
            foreach (var issue in issues.Where(i => i.LastAppliedTemplate == null))
            {
                issue.LastAppliedTemplate = new TemplateNode { Id = UnknownStateIdentifier, Name = DefaultTemplateIdentifier, Type = DefaultTemplateIdentifier };
            }

            return issues;
        }

        private static List<IssueNode> FilterIssuesThatAreChildrenOfProjectIssues(List<IssueNode> issues)
        {
            var issueIds = issues.Select(i => i.Identifier).ToList();
            var filteredIssues = issues.Where(i => i.Parent == null || !issueIds.Contains(i.Parent.Identifier)).ToList();

            return filteredIssues;
        }

        private static List<IssueNode> FilterIssuesForWorkItemOwner(IWorkItemQueryOwner owner, List<IssueNode> issues)
        {
            var types = owner.WorkItemTypes;
            var states = owner.AllStates.ToList();

            return issues.Where(i => types.Contains(i.LastAppliedTemplate.Name) && states.Contains(i.State.Name)).ToList();
        }

        private async Task<List<IssueNode>> GetIssuesForProject(Project project)
        {
            var projectNode = await GetProjectByName(project.WorkTrackingSystemConnection, project.WorkItemQuery);

            if (projectNode == null)
            {
                logger.LogInformation("Project with name '{ProjectName}' not found", project.WorkItemQuery);
                return [];
            }

            var projectId = projectNode.Id;
            var issues = await GetAllIssuesForProject(project.WorkTrackingSystemConnection, projectId);

            logger.LogInformation("Found a total of {Count} issues for project {ProjectName}", issues.Count, project.WorkItemQuery);
            return FilterIssuesForWorkItemOwner(project, issues);
        }

        private async Task<List<IssueNode>> GetIssuesForTeam(Team team)
        {
            var teamNode = await GetTeamByName(team.WorkTrackingSystemConnection, team.WorkItemQuery);

            if (teamNode == null)
            {
                logger.LogInformation("Team with name '{TeamName}' not found", team.WorkItemQuery);
                return [];
            }

            var teamId = teamNode.Id;
            var issues = await GetAllIssuesForTeam(team.WorkTrackingSystemConnection, teamId);

            logger.LogInformation("Found a total of {Count} issues for team {TeamName}", issues.Count, team.WorkItemQuery);
            return FilterIssuesForWorkItemOwner(team, issues);
        }

        private async Task<ProjectNode?> GetProjectByName(WorkTrackingSystemConnection connection, string projectName)
        {
            ProjectNode? projectNode = null;

            await GetWithPagination<ProjectsResponse>(connection, GetProjectsQueryTemplate, projects =>
            {
                var project = projects?.Projects?.Nodes.FirstOrDefault(t => t.Name == projectName);
                if (project != null)
                {
                    projectNode = project;
                    return false;
                }

                return true;
            });

            return projectNode;
        }

        private async Task<TeamNode?> GetTeamByName(WorkTrackingSystemConnection connection, string teamName)
        {
            TeamNode? teamNode = null;

            await GetWithPagination<TeamsResponse>(connection, GetTeamsQueryTemplate, teams =>
            {
                var team = teams?.Teams?.Nodes.FirstOrDefault(t => t.Name == teamName);
                if (team != null)
                {
                    teamNode = team;
                    return false;
                }

                return true;
            });

            return teamNode;
        }

        private async Task GetWithPagination<T>(WorkTrackingSystemConnection connection, Func<string, string> getQuery, Func<T, bool> processResult) where T : class, IPagedRespone
        {
            string? cursor = null;
            bool hasNextPage = true;

            while (hasNextPage)
            {
                var cursorParam = cursor != null ? $", after: \"{cursor}\"" : string.Empty;

                var query = getQuery(cursorParam);

                var response = await SendQuery<T>(connection, query);
                var continueToNextPage = processResult(response);

                if (!continueToNextPage)
                {
                    return;
                }

                var pageInfo = response?.GetPageInfo();
                hasNextPage = pageInfo?.HasNextPage ?? false;
                cursor = pageInfo?.EndCursor;

                if (string.IsNullOrEmpty(cursor))
                {
                    hasNextPage = false;
                }
            }
        }

        private async Task<T> SendQuery<T>(WorkTrackingSystemConnection connection, string query) where T : class
        {
            var client = GetLinearGraphQLClient(connection);

            var response = await client.SendQueryAsync<T>(query);

            foreach (var error in response.Errors ?? [])
            {
                logger.LogDebug("GraphQL Error: {ErrorMessage}", error.Message);
            }

            return response.Data;
        }

        private GraphQLHttpClient GetLinearGraphQLClient(WorkTrackingSystemConnection connection)
        {
            var encryptedApiKey = connection.GetWorkTrackingSystemConnectionOptionByKey(LinearWorkTrackingOptionNames.ApiKey);
            var apiKey = cryptoService.Decrypt(encryptedApiKey);

            var client = new HttpClient
            {
                BaseAddress = new Uri(LinearWorkTrackingOptionNames.ApiUrl)
            };
            client.DefaultRequestHeaders.Add("Authorization", apiKey);

            return new GraphQLHttpClient(new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(LinearWorkTrackingOptionNames.ApiUrl)
            }, new NewtonsoftJsonSerializer(), client);
        }

        private static string GetIssuesForProjectQuery(string projectId, string cursorParam)
        {
            return GetIssuesQueryTemplate("project", projectId, cursorParam);
        }

        private static string GetIssuesForTeamQuery(string teamId, string cursorParam)
        {
            return GetIssuesQueryTemplate("team", teamId, cursorParam);
        }

        private static string GetIssuesQueryTemplate(string parameter, string id, string cursorParam)
        {
            return $@"
                    query {{
                        {parameter}(id: ""{id}"") {{
                            id
                            name
                            issues(first: 100{cursorParam}) {{
                                nodes {{
                                    id
                                    title
                                    identifier
                                    url
                                    number
                                    sortOrder
                                    createdAt
                                    startedAt
                                    completedAt
                                    parent {{ 
                                        id 
                                        identifier
                                    }}
                                    lastAppliedTemplate {{
                                        id
                                        name
                                        type
                                    }}
                                    state {{
                                        id
                                        name
                                    }}
                                }}
                                pageInfo {{
                                    hasNextPage
                                    endCursor
                                }}
                            }}
                        }}
                    }}";
        }

        private static string GetTeamsQueryTemplate(string cursorParam)
        {
            return GetWorkItemQueryOwnersQueryTemplate("teams", cursorParam);
        }

        private static string GetProjectsQueryTemplate(string cursorParam)
        {
            return GetWorkItemQueryOwnersQueryTemplate("projects", cursorParam);
        }

        private static string GetWorkItemQueryOwnersQueryTemplate(string type, string cursorParam)
        {
            return string.Format(@"
                    query {{
                        {0}(first: 50{1}) {{
                            nodes {{
                                id
                                name
                            }}
                            pageInfo {{
                                hasNextPage
                                endCursor
                            }}
                        }}
                    }}", type, cursorParam);
        }
    }
}