namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector
    {
        public static class LinearResponses
        {
            public class ViewerResponse
            {
                public Viewer Viewer { get; set; }
            }

            public class Viewer
            {
                public string Id { get; set; }
            }

            public class TeamsResponse
            {
                public Teams Teams { get; set; }
            }

            public class TeamResponse
            {
                public TeamNode Team { get; set; }
            }

            public class Teams
            {
                public List<TeamNode> Nodes { get; set; }
            }

            public class TeamNode
            {
                public string Id { get; set; }

                public string Name { get; set; }
                
                public Issues Issues { get; set; }
            }

            public class Issues
            {
                public List<IssueNode> Nodes { get; set; }
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