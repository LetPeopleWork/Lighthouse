using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Validation;

using static Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear.LinearWorkTrackingConnector.LinearResponses;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector(
        ILogger<LinearWorkTrackingConnector> logger,
        ICryptoService cryptoService)
        : ILinearWorkTrackingConnector
    {
        private const string UnknownStateIdentifier = "Unknown";
        private const string IssueTypeIdentifier = "Issue";
        private const string ProjectTypeIdentifier = "Project";
        private const string InitiativeTypeIdentifier = "Initiative";

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}", team.Name);

            try
            {
                var teamName = team.DataRetrievalValue;

                if (string.IsNullOrEmpty(teamName))
                {
                    logger.LogWarning("Team {TeamName} has no Linear team identity configured", team.Name);
                    return [];
                }

                var teamId = await ResolveTeamIdByName(team.WorkTrackingSystemConnection, teamName);

                if (string.IsNullOrEmpty(teamId))
                {
                    logger.LogWarning("Could not resolve Linear team ID for team name '{TeamName}'", teamName);
                    return [];
                }

                var workItems = new List<WorkItem>();
                var allTeamIssues = await GetAllIssuesForTeam(team.WorkTrackingSystemConnection, teamId);
                var issues = FilterIssuesForStates(team, allTeamIssues);

                if (issues.Count == 0)
                {
                    logger.LogInformation("No issues found for team {TeamName}", team.Name);
                    return workItems;
                }

                var issuesLinkedToProject = 0;
                var issuesWithoutProject = 0;

                foreach (var issue in issues)
                {
                    var projectId = ResolveProjectIdForIssue(issue, allTeamIssues);
                    var workItemBase = CreateWorkItemFromIssue(issue, team, projectId);
                    workItems.Add(new WorkItem(workItemBase, team));

                    if (!string.IsNullOrEmpty(projectId))
                    {
                        issuesLinkedToProject++;
                    }
                    else
                    {
                        issuesWithoutProject++;
                        logger.LogDebug("Issue {Identifier} has no resolvable project reference", issue.Identifier);
                    }
                }

                logger.LogInformation(
                    "Hierarchy summary for team {TeamName}: {Total} issues scanned, {Linked} linked to projects, {Unlinked} without project reference",
                    team.Name, workItems.Count, issuesLinkedToProject, issuesWithoutProject);

                return workItems;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting work items for team {TeamName}", team.Name);
                return [];
            }
        }

        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            logger.LogInformation("Getting Features for Project {ProjectName} - retrieving all Linear projects as features", project.Name);

            try
            {
                var features = new List<Feature>();
                var projects = await GetAllProjects(project.WorkTrackingSystemConnection);

                if (projects.Count == 0)
                {
                    logger.LogInformation("No projects found in workspace for portfolio {ProjectName}", project.Name);
                    return features;
                }

                var states = project.AllStates.ToList();
                var featuresWithInitiative = 0;

                foreach (var linearProject in projects)
                {
                    var state = linearProject.Status?.Name ?? UnknownStateIdentifier;

                    if (states.Count > 0 && !states.Contains(state))
                    {
                        continue;
                    }

                    var feature = CreateFeatureFromProject(linearProject, project);
                    features.Add(feature);

                    if (!string.IsNullOrEmpty(feature.ParentReferenceId))
                    {
                        featuresWithInitiative++;
                    }
                }

                logger.LogInformation(
                    "Created {Count} features from Linear projects for portfolio {ProjectName} ({WithInitiative} linked to initiatives)",
                    features.Count, project.Name, featuresWithInitiative);

                return features;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Features for Project {ProjectName}", project.Name);
                return new List<Feature>();
            }
        }

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features (initiatives) for Linear portfolio {ProjectName}", project.Name);

            var parentFeatures = new List<Feature>();
            var parentIdList = parentFeatureIds.ToList();
            var initiativesHydrated = 0;
            var initiativesFailed = 0;

            foreach (var parentFeatureId in parentIdList)
            {
                try
                {
                    var initiative = await GetInitiativeById(project.WorkTrackingSystemConnection, parentFeatureId);

                    if (initiative != null)
                    {
                        var parentFeature = new Feature
                        {
                            ReferenceId = initiative.Id,
                            Name = initiative.Name,
                            Type = InitiativeTypeIdentifier,
                            State = project.MapRawStateToMappedName(initiative.Status ?? UnknownStateIdentifier),
                            StateCategory = StateCategories.Unknown,
                            Url = initiative.Url ?? string.Empty,
                            ParentReferenceId = string.Empty,
                            Order = initiative.SortOrder.ToString(),
                            CreatedDate = initiative.CreatedAt,
                            StartedDate = initiative.StartedAt,
                            ClosedDate = initiative.CompletedAt,
                        };

                        parentFeatures.Add(parentFeature);
                        initiativesHydrated++;
                    }
                    else
                    {
                        logger.LogDebug("Initiative {InitiativeId} not found or inaccessible for portfolio {ProjectName}", parentFeatureId, project.Name);
                        initiativesFailed++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to fetch initiative {InitiativeId} for portfolio {ProjectName}", parentFeatureId, project.Name);
                    initiativesFailed++;
                }
            }

            logger.LogInformation(
                "Initiative hydration for portfolio {ProjectName}: {Total} requested, {Hydrated} resolved, {Failed} failed",
                project.Name, parentIdList.Count, initiativesHydrated, initiativesFailed);

            return parentFeatures;
        }

        public async Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
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
                return ConnectionValidationResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate Linear connection");
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Could not validate the Linear connection with the provided settings.",
                    ex.Message);
            }
        }

        public async Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio)
        {
            try
            {
                logger.LogInformation("Validating Portfolio Settings for Project {ProjectName} - checking workspace projects", portfolio.Name);

                var projects = await GetAllProjects(portfolio.WorkTrackingSystemConnection);

                logger.LogInformation("Found {TotalProjects} projects in workspace for portfolio {ProjectName}",
                    projects.Count, portfolio.Name);

                if (projects.Count == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_features_found",
                        "No portfolio features were found for this configuration.",
                        "No Linear projects were returned for the current workspace settings.");
                }

                return ConnectionValidationResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Project Settings for Project {ProjectName}", portfolio.Name);
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Portfolio validation failed due to an unexpected error.",
                    ex.Message);
            }
        }

        public async Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            try
            {
                var teamName = team.DataRetrievalValue;

                if (string.IsNullOrEmpty(teamName))
                {
                    logger.LogWarning("Team {TeamName} has no Linear team identity configured. Please select a team via the wizard.", team.Name);
                    return ConnectionValidationResult.Failure(
                        "missing_team_name",
                        "No Linear team was selected for this configuration.",
                        "Select a team in the data retrieval step or re-run the team wizard.",
                        "DataRetrievalValue");
                }

                var teamId = await ResolveTeamIdByName(team.WorkTrackingSystemConnection, teamName);

                if (string.IsNullOrEmpty(teamId))
                {
                    logger.LogWarning("Could not resolve Linear team ID for team name '{TeamName}'. Please reconfigure via the team wizard.", teamName);
                    return ConnectionValidationResult.Failure(
                        "invalid_team_name",
                        "The selected Linear team could not be resolved.",
                        "Re-run the team wizard and choose a valid Linear team.",
                        "DataRetrievalValue");
                }

                logger.LogInformation("Validating Team Settings for Team {TeamName} with resolved team identity {TeamId}", team.Name, teamId);

                var issues = await GetAllIssuesForTeam(team.WorkTrackingSystemConnection, teamId);
                var filteredIssues = FilterIssuesForStates(team, issues);

                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items for team {TeamName}", filteredIssues.Count, team.Name);

                if (filteredIssues.Count == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_work_items_found",
                        "No work items were found for this team configuration.",
                        "Check selected states and team data in Linear.");
                }

                return ConnectionValidationResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Team Settings for Team {TeamName}. The configured team name may be invalid — please reconfigure via the team wizard.", team.Name);
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Team validation failed due to an unexpected error.",
                    ex.Message);
            }
        }

        private WorkItemBase CreateWorkItemFromIssue(IssueNode issue, Team team, string resolvedProjectId)
        {
            var state = issue.State?.Name ?? UnknownStateIdentifier;

            var stateCategory = team.MapStateToStateCategory(state);

            if (issue.CompletedAt != null && issue.StartedAt == null)
            {
                issue.StartedAt = issue.CompletedAt;
            }

            var workItemBase = new WorkItemBase
            {
                ReferenceId = issue.Identifier?.ToLowerInvariant() ?? $"issue-{issue.Number}",
                Name = issue.Title,
                Type = IssueTypeIdentifier,
                State = team.MapRawStateToMappedName(state),
                StateCategory = stateCategory,
                Url = issue.Url,
                ParentReferenceId = resolvedProjectId,
                Order = issue.SortOrder.ToString(),
                CreatedDate = issue.CreatedAt,
                StartedDate = issue.StartedAt,
                ClosedDate = issue.CompletedAt,
            };

            return workItemBase;
        }

        private static string ResolveProjectIdForIssue(IssueNode issue, List<IssueNode> allIssues)
        {
            if (issue.Project?.Id != null)
            {
                return issue.Project.Id;
            }

            if (issue.Parent?.Identifier == null)
            {
                return string.Empty;
            }

            var visited = new HashSet<string>();
            var currentIdentifier = issue.Parent.Identifier;

            while (currentIdentifier != null && visited.Add(currentIdentifier))
            {
                var parentIssue = allIssues.FirstOrDefault(i => i.Identifier == currentIdentifier);
                if (parentIssue == null)
                {
                    break;
                }

                if (parentIssue.Project?.Id != null)
                {
                    return parentIssue.Project.Id;
                }

                currentIdentifier = parentIssue.Parent?.Identifier;
            }

            return string.Empty;
        }

        private static Feature CreateFeatureFromProject(ProjectNode projectNode, Portfolio portfolio)
        {
            var state = projectNode.Status?.Name ?? UnknownStateIdentifier;
            var stateCategory = portfolio.MapStateToStateCategory(state);

            var initiativeId = projectNode.Initiatives?.Nodes?.FirstOrDefault()?.Id ?? string.Empty;
            
            return new Feature
            {
                ReferenceId = projectNode.Id,
                Name = projectNode.Name,
                Type = ProjectTypeIdentifier,
                State = portfolio.MapRawStateToMappedName(state),
                StateCategory = stateCategory,
                Url = projectNode.Url ?? string.Empty,
                ParentReferenceId = initiativeId,
                Order = projectNode.SortOrder.ToString(),
                CreatedDate = projectNode.CreatedAt,
                StartedDate = projectNode.StartDate,
                ClosedDate = projectNode.CompletedAt,
            };
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

            return issues;
        }

        private async Task<List<ProjectNode>> GetAllProjects(WorkTrackingSystemConnection connection)
        {
            var projects = new List<ProjectNode>();

            await GetWithPagination<ProjectsResponse>(connection, GetProjectsWithDetailsQueryTemplate, projectsResponse =>
            {
                var fetchedProjects = projectsResponse?.Projects?.Nodes ?? [];
                projects.AddRange(fetchedProjects);

                return true;
            });

            return projects;
        }

        private async Task<InitiativeNode?> GetInitiativeById(WorkTrackingSystemConnection connection, string initiativeId)
        {
            var query = GetInitiativeByIdQuery(initiativeId);
            var response = await SendQuery<InitiativeResponse>(connection, query);
            return response?.Initiative;
        }

        private async Task<string?> ResolveTeamIdByName(WorkTrackingSystemConnection connection, string teamName)
        {
            var teams = new List<TeamNode>();

            await GetWithPagination<TeamsResponse>(connection, GetTeamsQueryTemplate, teamsResponse =>
            {
                var fetchedTeams = teamsResponse?.Teams?.Nodes ?? [];
                teams.AddRange(fetchedTeams);
                return true;
            });

            var match = teams.FirstOrDefault(t => string.Equals(t.Name, teamName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                logger.LogWarning("No Linear team found with name '{TeamName}'. Available teams: {Teams}", teamName, string.Join(", ", teams.Select(t => t.Name)));
            }

            return match?.Id;
        }

        private async Task<TeamDetailNode?> GetTeamDetail(WorkTrackingSystemConnection connection, string teamId)
        {
            var query = GetTeamDetailQuery(teamId);
            var response = await SendQuery<TeamDetailResponse>(connection, query);
            return response?.Team;
        }

        private static List<IssueNode> FilterIssuesForStates(Team team, List<IssueNode> issues)
        {
            var states = team.AllStates.ToList();

            return issues.Where(i => states.Contains(i.State.Name)).ToList();
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
                BaseAddress = new Uri(LinearWorkTrackingOptionNames.ApiUrl),
            };
            client.DefaultRequestHeaders.Add("Authorization", apiKey);

            return new GraphQLHttpClient(new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(LinearWorkTrackingOptionNames.ApiUrl)
            }, new NewtonsoftJsonSerializer(), client);
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
                                    project {{
                                        id
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

        private static string GetProjectsWithDetailsQueryTemplate(string cursorParam)
        {
            return $@"
            query {{
                projects(first: 50{cursorParam}) {{
                    nodes {{
                        id
                        name
                        status {{
                            id
                            name
                            type
                            color
                        }}
                        url
                        sortOrder
                        createdAt
                        startDate
                        completedAt
                        initiatives(first: 10) {{
                            nodes {{
                                id
                                name
                            }}
                        }}
                    }}
                    pageInfo {{
                        hasNextPage
                        endCursor
                    }}
                }}
            }}";
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

        private static string GetInitiativeByIdQuery(string initiativeId)
        {
            return $@"
                    query {{
                        initiative(id: ""{initiativeId}"") {{
                            id
                            name
                            status
                            url
                            sortOrder
                            createdAt
                            startedAt
                            completedAt
                        }}
                    }}";
        }

        private static string GetTeamDetailQuery(string teamId)
        {
            return $@"
                    query {{
                        team(id: ""{teamId}"") {{
                            id
                            name
                            states {{
                                nodes {{
                                    name
                                    type
                                    position
                                }}
                            }}
                        }}
                    }}";
        }

        public Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            throw new NotSupportedException("Write-back is not supported for Linear.");
        }

        public async Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            logger.LogInformation("Getting Linear teams as boards for connection {ConnectionName}", workTrackingSystemConnection.Name);

            var boards = new List<Board>();

            await GetWithPagination<TeamsResponse>(workTrackingSystemConnection, GetTeamsQueryTemplate, teamsResponse =>
            {
                var teams = teamsResponse?.Teams?.Nodes ?? [];
                boards.AddRange(teams.Select(t => new Board { Id = t.Id, Name = t.Name }));
                return true;
            });

            return boards;
        }

        public async Task<BoardInformation> GetBoardInformation(WorkTrackingSystemConnection workTrackingSystemConnection, string boardId)
        {
            logger.LogInformation("Getting Linear team information for team {TeamId}", boardId);

            try
            {
                var teamDetail = await GetTeamDetail(workTrackingSystemConnection, boardId);

                if (teamDetail == null)
                {
                    logger.LogWarning("Could not fetch team details for team {TeamId}", boardId);
                    return new BoardInformation();
                }

                var states = teamDetail.States?.Nodes ?? [];

                var toDoStates = states
                    .Where(s => s.Type is "triage" or "backlog" or "unstarted")
                    .OrderBy(s => s.Position)
                    .Select(s => s.Name)
                    .ToList();

                var doingStates = states
                    .Where(s => s.Type is "started")
                    .OrderBy(s => s.Position)
                    .Select(s => s.Name)
                    .ToList();

                var doneStates = states
                    .Where(s => s.Type is "completed")
                    .OrderBy(s => s.Position)
                    .Select(s => s.Name)
                    .ToList();

                logger.LogInformation(
                    "Resolved team {TeamName} ({TeamId}): {ToDoCount} todo states, {DoingCount} doing states, {DoneCount} done states",
                    teamDetail.Name, boardId, toDoStates.Count, doingStates.Count, doneStates.Count);

                return new BoardInformation
                {
                    DataRetrievalValue = teamDetail.Name,
                    WorkItemTypes = [],
                    ToDoStates = toDoStates,
                    DoingStates = doingStates,
                    DoneStates = doneStates,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching team details for team {TeamId}", boardId);
                return new BoardInformation();
            }
        }
    }
}