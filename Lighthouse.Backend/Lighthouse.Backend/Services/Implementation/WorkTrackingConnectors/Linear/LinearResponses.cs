namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector
    {
        public static class LinearResponses
        {
            public interface IPagedRespone
            {
                PageInfo GetPageInfo();
            }

            public class ViewerResponse
            {
                public Viewer Viewer { get; set; }
            }

            public class Viewer
            {
                public string Id { get; set; }
            }

            public class TeamsResponse : IPagedRespone
            {
                public Teams Teams { get; set; }

                public PageInfo GetPageInfo()
                {
                    return Teams?.PageInfo ?? new NullPageInfo();
                }
            }

            public class TeamResponse : IPagedRespone
            {
                public TeamNode Team { get; set; }

                public PageInfo GetPageInfo()
                {
                    return Team?.Issues?.PageInfo ?? new NullPageInfo();
                }
            }

            public class Teams
            {
                public List<TeamNode> Nodes { get; set; }

                public PageInfo PageInfo { get; set; }
            }

            public class TeamNode
            {
                public string Id { get; set; }

                public string Name { get; set; }

                public Issues Issues { get; set; }
            }

            public class ProjectsResponse : IPagedRespone
            {
                public Projects Projects { get; set; }

                public PageInfo GetPageInfo()
                {
                    return Projects?.PageInfo ?? new NullPageInfo();
                }
            }

            public class ProjectResponse : IPagedRespone
            {
                public ProjectNode Project { get; set; }

                public PageInfo GetPageInfo()
                {
                    return Project?.Issues?.PageInfo ?? new NullPageInfo();
                }
            }

            public class Projects
            {
                public List<ProjectNode> Nodes { get; set; }

                public PageInfo PageInfo { get; set; }
            }

            public class ProjectNode
            {
                public string Id { get; set; }

                public string Name { get; set; }
                
                public Issues Issues { get; set; }
            }

            public class Issues
            {
                public List<IssueNode> Nodes { get; set; }

                public PageInfo PageInfo { get; set; }
            }

            public class PageInfo
            {
                public bool HasNextPage { get; set; }
                
                public string EndCursor { get; set; }

                public bool HasPreviousPage { get; set; }
               
                public string StartCursor { get; set; }
            }

            public class NullPageInfo : PageInfo
            {
                public NullPageInfo()
                {
                    HasNextPage = false;
                    EndCursor = "0";
                    HasPreviousPage = false;
                    StartCursor = "0";
                }
            }

            public class IssueNode
            {
                public string Id { get; set; }

                public string Title { get; set; }
                
                public StateNode State { get; set; }

                public string Identifier { get; set; }

                public string Number { get; set; }

                public string Url { get; set; }

                public double SortOrder { get; set; }

                public ParentNode Parent { get; set; }

                public TeamNode Team { get; set; }

                public TemplateNode LastAppliedTemplate { get; set; }

                public DateTime? StartedAt { get; set; }

                public DateTime? CompletedAt { get; set; }

                public DateTime CreatedAt { get; set; }
            }

            public class IssueType
            {
                public string Id { get; set; }
                
                public string Name { get; set; }
            }

            public class TemplateNode
            {
                public string Id { get; set; }
                
                public string Name { get; set; }

                public string Type { get; set; }
            }

            public class ParentNode
            {
                public string Id { get; set; }
                
                public string Identifier { get; set; }
            }

            public class StateNode
            {
                public string Id { get; set; }

                public string Name { get; set; }
            }
        }
    }
}