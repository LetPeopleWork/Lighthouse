using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A Jira Data Center or Server instance, answering the way Atlassian's Server REST reference (7.13 to 9.17)
    /// documents it rather than the way Cloud does. The differences are the point of this fake:
    /// there is no rest/api/3 at all, there is no project/search even under rest/api/2 (Data Center reads
    /// "search" as a project key and finds no such project), the project list is a bare array served in one
    /// answer, and the paged version list has no status parameter, so it hands back every version whatever
    /// was asked for.
    ///
    /// Anything it does not serve answers 404, as Data Center does, so a call to a route only Cloud knows
    /// fails here exactly the way it fails on a customer's instance.
    /// </summary>
    public sealed class FakeJiraDataCenter
    {
        private const string ProjectListPath = "/rest/api/2/project";
        private const string VersionPathPrefix = "/rest/api/2/version/";

        private readonly List<FakeProject> projects = [];
        private readonly ConcurrentDictionary<string, FakeVersion> versionsById = new(StringComparer.Ordinal);
        private int nextProjectId = 10000;

        public FakeJiraDataCenter()
        {
            Handler = new Transport(this);
        }

        public HttpMessageHandler Handler { get; }

        /// <summary>Every url the connector asked for, in the order it asked. A queue, because project reads run in parallel.</summary>
        public ConcurrentQueue<Uri> Requests { get; } = new();

        /// <summary>Every write to a version, as the id it was addressed to and the body it carried.</summary>
        public ConcurrentQueue<(string VersionId, string Body)> VersionWrites { get; } = new();

        /// <summary>How many versions one page of a project's version list carries, so paging can be provoked with two.</summary>
        public int VersionsPerPage { get; set; } = 50;

        /// <summary>A server that will not say what it is, which leaves the connector not knowing its deployment.</summary>
        public bool HidesItsDeployment { get; set; }

        public IReadOnlyList<Uri> VersionListRequests =>
            [.. Requests.Where(uri => uri.AbsolutePath.StartsWith(ProjectListPath + "/", StringComparison.Ordinal)
                && uri.AbsolutePath.EndsWith("/version", StringComparison.Ordinal))];

        /// <summary>
        /// A connection to this instance. Its url is one nobody else uses, because the connector remembers each
        /// url's deployment for the life of the process.
        /// </summary>
        public static WorkTrackingSystemConnection AConnection()
        {
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;
            connection.AuthenticationMethodKey = AuthenticationMethodKeys.JiraDataCenter;
            connection.Name = $"Jira Data Center {connection.Id}";

            return connection;
        }

        public FakeJiraDataCenter WithProject(string key, string name)
        {
            projects.Add(new FakeProject(nextProjectId++.ToString(CultureInfo.InvariantCulture), key, name));

            return this;
        }

        public FakeJiraDataCenter WithReleaseIn(string projectKey, string id, string name, string? releaseDate)
            => WithVersionIn(projectKey, id, name, releaseDate, archived: false, released: false);

        public FakeJiraDataCenter WithShippedReleaseIn(string projectKey, string id, string name, string releaseDate)
            => WithVersionIn(projectKey, id, name, releaseDate, archived: false, released: true);

        public FakeJiraDataCenter WithArchivedReleaseIn(string projectKey, string id, string name, string releaseDate)
            => WithVersionIn(projectKey, id, name, releaseDate, archived: true, released: true);

        /// <summary>The description a version carries now, after any write the connector made to it.</summary>
        public string? DescriptionOf(string versionId)
            => versionsById.TryGetValue(versionId, out var version) ? version.Description : null;

        private FakeJiraDataCenter WithVersionIn(string projectKey, string id, string name, string? releaseDate, bool archived, bool released)
        {
            var project = projects.Single(candidate => candidate.Key == projectKey);
            versionsById[id] = new FakeVersion(id, name, archived, released, releaseDate, project.Id, Order: versionsById.Count);

            return this;
        }

        private HttpResponseMessage Respond(HttpRequestMessage request)
        {
            var uri = request.RequestUri ?? new Uri("https://unreached.invalid/");
            Requests.Enqueue(uri);

            var path = uri.AbsolutePath.TrimEnd('/');

            if (path.EndsWith("/rest/api/2/serverInfo", StringComparison.Ordinal))
            {
                return HidesItsDeployment
                    ? NotFound()
                    : Ok("{\"baseUrl\":\"" + uri.GetLeftPart(UriPartial.Authority) + "\",\"version\":\"9.12.0\",\"deploymentType\":\"Server\"}");
            }

            if (path.EndsWith(ProjectListPath, StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                return Ok($"[{string.Join(",", projects.Select(ProjectJson))}]");
            }

            var projectsAt = path.IndexOf(ProjectListPath + "/", StringComparison.Ordinal);
            if (projectsAt >= 0 && path.EndsWith("/version", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            {
                var projectKey = Uri.UnescapeDataString(path[(projectsAt + ProjectListPath.Length + 1)..^"/version".Length]);
                return RespondWithVersionsOf(projectKey, uri);
            }

            var versionAt = path.IndexOf(VersionPathPrefix, StringComparison.Ordinal);
            if (versionAt >= 0)
            {
                return RespondToVersion(request, path[(versionAt + VersionPathPrefix.Length)..]);
            }

            return NotFound();
        }

        /// <summary>
        /// The paged envelope Data Center wraps a project's versions in. Any status in the query is ignored,
        /// because the route has no such parameter.
        /// </summary>
        private HttpResponseMessage RespondWithVersionsOf(string projectKey, Uri uri)
        {
            var project = projects.SingleOrDefault(candidate => candidate.Key == projectKey);
            if (project is null)
            {
                return NotFound();
            }

            var startAt = int.TryParse(QueryValue(uri, "startAt"), CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

            var all = versionsById.Values
                .Where(version => version.ProjectId == project.Id)
                .OrderBy(version => version.Order)
                .ToList();

            var page = all.Skip(startAt).Take(VersionsPerPage).ToList();
            var isLast = startAt + page.Count >= all.Count;

            return Ok(
                $"{{\"self\":\"{uri}\",\"maxResults\":{VersionsPerPage},\"startAt\":{startAt},\"total\":{all.Count}," +
                $"\"isLast\":{Flag(isLast)},\"values\":[{string.Join(",", page.Select(version => VersionJson(version, uri)))}]}}");
        }

        private HttpResponseMessage RespondToVersion(HttpRequestMessage request, string versionId)
        {
            if (!versionsById.TryGetValue(versionId, out var version))
            {
                return NotFound();
            }

            if (request.Method == HttpMethod.Put)
            {
                var body = JiraConnectorTestSetup.BodyOf(request);
                VersionWrites.Enqueue((versionId, body));

                version = version with { Description = DescriptionIn(body) ?? version.Description };
                versionsById[versionId] = version;
            }
            else if (request.Method != HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }

            return Ok(VersionJson(version, request.RequestUri!));
        }

        private static string? DescriptionIn(string body)
        {
            using var payload = JsonDocument.Parse(body);

            return payload.RootElement.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String
                ? description.GetString()
                : null;
        }

        private static string ProjectJson(FakeProject project)
            => $"{{\"id\":\"{project.Id}\",\"key\":\"{project.Key}\",\"name\":\"{project.Name}\"}}";

        private static string VersionJson(FakeVersion version, Uri askedAt)
        {
            var self = $"{askedAt.GetLeftPart(UriPartial.Authority)}{VersionPathPrefix}{version.Id}";
            var date = version.ReleaseDate is null ? string.Empty : $",\"releaseDate\":\"{version.ReleaseDate}\"";
            var description = version.Description is null ? string.Empty : $",\"description\":{JsonSerializer.Serialize(version.Description)}";

            return $"{{\"self\":\"{self}\",\"id\":\"{version.Id}\"{description},\"name\":\"{version.Name}\"," +
                $"\"archived\":{Flag(version.Archived)},\"released\":{Flag(version.Released)}{date},\"projectId\":{version.ProjectId}}}";
        }

        private static string QueryValue(Uri uri, string name)
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var match = Array.Find(pairs, pair => pair.StartsWith($"{name}=", StringComparison.Ordinal));

            return match is null ? string.Empty : Uri.UnescapeDataString(match[(name.Length + 1)..]);
        }

        private static string Flag(bool value) => value ? "true" : "false";

        private static HttpResponseMessage NotFound()
            => new(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"errorMessages\":[\"Not Found\"],\"errors\":{}}", Encoding.UTF8, "application/json"),
            };

        private static HttpResponseMessage Ok(string body)
            => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private sealed record FakeProject(string Id, string Key, string Name);

        private sealed record FakeVersion(
            string Id, string Name, bool Archived, bool Released, string? ReleaseDate, string ProjectId, int Order)
        {
            public string? Description { get; init; }
        }

        private sealed class Transport(FakeJiraDataCenter jira) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(jira.Respond(request));
        }
    }
}
