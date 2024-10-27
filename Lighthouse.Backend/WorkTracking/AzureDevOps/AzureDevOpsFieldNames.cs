namespace Lighthouse.Backend.WorkTracking.AzureDevOps
{
    public static class AzureDevOpsFieldNames
    {
        public static string Id => "System.Id";

        public static string Title => "System.Title";

        public static string State => "System.State";

        public static string TeamProject => "System.TeamProject";

        public static string AreaPath => "System.AreaPath";

        public static string Parent => "System.Parent";

        public static string WorkItemType => "System.WorkItemType";

        public static string Tags => "System.Tags";

        public static string ClosedDate => "Microsoft.VSTS.Common.ClosedDate";

        public static string StackRank => "Microsoft.VSTS.Common.StackRank";

        public static string BacklogPriority => "Microsoft.VSTS.Common.BacklogPriority";
    }
}
