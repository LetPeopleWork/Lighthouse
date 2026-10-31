using System.Net;
using System.Text;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A Jira instance as one account sees it: the status list, the issue type list and the pages a search
    /// answers with, plus a record of every request the connector made. Each list answers with whatever
    /// status and body the fixture sets, which is how an unreadable list is staged.
    /// </summary>
    public sealed class JiraInstanceStub
    {
        private readonly List<string> paths = [];

        private readonly List<string> searchBodies = [];

        private int pagesServed;

        public JiraInstanceStub(string deployment = "Cloud")
        {
            Deployment = deployment;
            StatusList = (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(JiraWireFormat.AnEnglishStatuses));
            IssueTypeList = (HttpStatusCode.OK, JiraWireFormat.TheIssueTypeListOf(JiraWireFormat.AnEnglishIssueTypes));
        }

        public string Deployment { get; }

        public (HttpStatusCode Status, string Body) StatusList { get; set; }

        public (HttpStatusCode Status, string Body) IssueTypeList { get; set; }

        /// <summary>The pages a search hands back in turn; every page but the last carries a token for the next.</summary>
        public List<string[]> SearchPages { get; } = [];

        /// <summary>When set, the instance also serves this board: its columns hold <see cref="BoardStatusIds"/>, its issues <see cref="BoardIssues"/>.</summary>
        public string? BoardId { get; set; }

        public string[] BoardStatusIds { get; set; } = [];

        public (string Id, string Name)[] BoardIssues { get; set; } = [];

        public IReadOnlyList<string> Paths => paths;

        public IReadOnlyList<string> SearchBodies => searchBodies;

        public int ReadsOf(string route) => paths.Count(path => path.EndsWith(route, StringComparison.Ordinal));

        public JiraInstanceStub ReadBy(IEnumerable<string> statuses, IEnumerable<string> issueTypes)
        {
            StatusList = (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(statuses));
            IssueTypeList = (HttpStatusCode.OK, JiraWireFormat.TheIssueTypeListOf(issueTypes));

            return this;
        }

        public JiraInstanceStub Serving(params string[] issues)
        {
            SearchPages.Clear();
            SearchPages.Add(issues);

            return this;
        }

        public HttpMessageHandler Handler()
            => StubTransport.AnsweringWith(request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                paths.Add(path);

                return path switch
                {
                    _ when path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) => ServerInfoAnswer(),
                    _ when path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal) => Answer(HttpStatusCode.OK, "[]"),
                    _ when path.EndsWith(JiraWireFormat.StatusListEndpoint, StringComparison.Ordinal) => AnswerOfTheList(JiraWireFormat.StatusListEndpoint, StatusList, StatusListOnReread),
                    _ when path.EndsWith(JiraWireFormat.IssueTypeListEndpoint, StringComparison.Ordinal) => AnswerOfTheList(JiraWireFormat.IssueTypeListEndpoint, IssueTypeList, IssueTypeListOnReread),
                    _ when BoardId is not null && path.EndsWith($"board/{BoardId}/configuration", StringComparison.Ordinal)
                        => Answer(HttpStatusCode.OK, "{\"columnConfig\":{\"columns\":[" + string.Join(",", BoardStatusIds.Select(id => "{\"statuses\":[{\"id\":\"" + id + "\"}]}")) + "]}}"),
                    _ when BoardId is not null && path.EndsWith($"board/{BoardId}/issue", StringComparison.Ordinal)
                        => Answer(HttpStatusCode.OK, "{\"issues\":[" + string.Join(",", BoardIssues.Select(
                            issue => "{\"fields\":{\"issuetype\":{\"id\":\"" + issue.Id + "\",\"name\":\"" + issue.Name + "\"}}}")) + "]}"),
                    _ when path.Contains("/search", StringComparison.Ordinal) => Answer(HttpStatusCode.OK, ASearchPage(JiraConnectorTestSetup.BodyOf(request))),
                    _ => Answer(HttpStatusCode.OK, "{}"),
                };
            });

        /// <summary>What the status list answers from its second read on, for an instance that gained a status in between.</summary>
        public (HttpStatusCode Status, string Body)? StatusListOnReread { get; set; }

        /// <summary>What the issue type list answers from its second read on.</summary>
        public (HttpStatusCode Status, string Body)? IssueTypeListOnReread { get; set; }

        private HttpResponseMessage ServerInfoAnswer()
            => Deployment == "Unknown"
                ? Answer(HttpStatusCode.InternalServerError, "{}")
                : Answer(HttpStatusCode.OK, $"{{\"deploymentType\":\"{Deployment}\"}}");

        private HttpResponseMessage AnswerOfTheList(
            string route, (HttpStatusCode Status, string Body) firstAnswer, (HttpStatusCode Status, string Body)? laterAnswer)
        {
            var answer = ReadsOf(route) >= 2 && laterAnswer is { } later ? later : firstAnswer;

            return Answer(answer.Status, answer.Body);
        }

        private string ASearchPage(string requestBody)
        {
            searchBodies.Add(requestBody);

            var pages = SearchPages.Count == 0 ? [[]] : SearchPages;
            var page = pages[Math.Min(pagesServed, pages.Count - 1)];
            var isLast = pagesServed >= pages.Count - 1;
            pagesServed++;

            var issues = string.Join(",", page);

            if (Deployment == "Cloud")
            {
                var token = isLast ? string.Empty : ",\"nextPageToken\":\"next\"";

                return "{\"issues\":[" + issues + "]" + token + (isLast ? ",\"isLast\":true" : string.Empty) + "}";
            }

            return "{\"startAt\":0,\"maxResults\":50,\"total\":" + page.Length + ",\"issues\":[" + issues + "]}";
        }

        private static HttpResponseMessage Answer(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
