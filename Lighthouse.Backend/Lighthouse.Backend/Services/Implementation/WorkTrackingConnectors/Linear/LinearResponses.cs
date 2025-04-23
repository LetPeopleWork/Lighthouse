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
            }

            public class StateNode
            {
                public string Id { get; set; }

                public string Name { get; set; }
            }
        }
    }
}