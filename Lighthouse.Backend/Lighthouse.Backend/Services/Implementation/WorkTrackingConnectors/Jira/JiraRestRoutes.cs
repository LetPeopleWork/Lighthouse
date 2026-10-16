namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    internal enum JiraDeployment { Unknown, Cloud, DataCenter }

    /// <summary>
    /// Every Jira REST route the connector calls, split by the kind of Jira it talks to. Jira Data Center and
    /// Server serve no rest/api/3 and no paged project search, and their project list and version list differ
    /// in shape from Cloud's, so a route that works against Cloud is no evidence it works anywhere else. Each
    /// capability that can differ is abstract here, which means a new one does not compile until both Cloud
    /// and Data Center have named their route. A route both serve identically is written once, in this class.
    /// </summary>
    internal abstract class JiraRestRoutes
    {
        private static readonly JiraRestRoutes CloudRoutes = new Cloud();

        private static readonly JiraRestRoutes DataCenterRoutes = new DataCenter();

        private static readonly JiraRestRoutes UnidentifiedRoutes = new Unidentified();

        // Asked to find out which kind of Jira this is, so it cannot depend on the answer.
        public const string ServerInfo = "rest/api/2/serverInfo";

        public const string Myself = "rest/api/2/myself";

        public const string Field = "rest/api/latest/field";

        public const string Status = "rest/api/latest/status";

        public const string IssueLinkType = "rest/api/latest/issueLinkType";

        public const string ProjectList = "rest/api/2/project";

        public static string Filter(string filterId) => $"rest/api/2/filter/{filterId}";

        public static string Issue(string issueKey, bool silently)
            => silently
                ? $"rest/api/latest/issue/{issueKey}?notifyUsers=false"
                : $"rest/api/latest/issue/{issueKey}";

        public static string Changelog(string issueId, int startAt, int maxResults)
            => $"rest/api/latest/issue/{issueId}/changelog?startAt={startAt}&maxResults={maxResults}";

        /// <summary>
        /// A Jira that would not say which kind it is gets the routes every deployment serves.
        /// </summary>
        public static JiraRestRoutes For(JiraDeployment deployment)
            => deployment switch
            {
                JiraDeployment.Cloud => CloudRoutes,
                JiraDeployment.DataCenter => DataCenterRoutes,
                JiraDeployment.Unknown => UnidentifiedRoutes,
                _ => throw new ArgumentOutOfRangeException(nameof(deployment), deployment, null),
            };

        /// <summary>The kind of Jira these routes were chosen for.</summary>
        public abstract JiraDeployment Deployment { get; }

        /// <summary>
        /// Both take their parameters in a json body. Cloud's is paged by token, Data Center's by offset.
        /// </summary>
        public abstract string Search { get; }

        /// <summary>The paged project search, or null where the instance has none and only the plain list will do.</summary>
        public abstract string? PagedProjectSearch { get; }

        /// <summary>
        /// Whether a 404 from the paged project search means the instance simply lacks it, so the plain list
        /// is worth asking for instead.
        /// </summary>
        public abstract bool FallsBackToProjectListWhenSearchIsMissing { get; }

        /// <summary>Whether the version list applies a status filter itself, or hands back every version.</summary>
        public abstract bool CanFilterVersionsByStatus { get; }

        public abstract string ProjectVersions(string projectKey, int startAt, int pageSize, string statuses);

        public abstract string Version(string versionId);

        private sealed class Cloud : JiraRestRoutes
        {
            public override JiraDeployment Deployment => JiraDeployment.Cloud;

            public override string Search => "rest/api/3/search/jql";

            public override string? PagedProjectSearch => "rest/api/3/project/search";

            public override bool FallsBackToProjectListWhenSearchIsMissing => false;

            public override bool CanFilterVersionsByStatus => true;

            public override string ProjectVersions(string projectKey, int startAt, int pageSize, string statuses)
                => $"rest/api/3/project/{Uri.EscapeDataString(projectKey)}/version?startAt={startAt}&maxResults={pageSize}&status={statuses}";

            public override string Version(string versionId) => $"rest/api/3/version/{Uri.EscapeDataString(versionId)}";
        }

        private class DataCenter : JiraRestRoutes
        {
            public override JiraDeployment Deployment => JiraDeployment.DataCenter;

            public override string Search => "rest/api/latest/search";

            public override string? PagedProjectSearch => null;

            public override bool FallsBackToProjectListWhenSearchIsMissing => false;

            public override bool CanFilterVersionsByStatus => false;

            public override string ProjectVersions(string projectKey, int startAt, int pageSize, string statuses)
                => $"rest/api/2/project/{Uri.EscapeDataString(projectKey)}/version?startAt={startAt}&maxResults={pageSize}";

            public override string Version(string versionId) => $"rest/api/2/version/{Uri.EscapeDataString(versionId)}";
        }

        /// <summary>
        /// The routes both deployments serve. The one difference from Data Center: the paged project search is
        /// tried first, because Cloud serves it, and the plain list is asked for only when Jira answers it with
        /// a 404, as Data Center does.
        /// </summary>
        private sealed class Unidentified : DataCenter
        {
            public override JiraDeployment Deployment => JiraDeployment.Unknown;

            public override string? PagedProjectSearch => "rest/api/2/project/search";

            public override bool FallsBackToProjectListWhenSearchIsMissing => true;
        }
    }
}
