namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Connection-scope option keys for a ServiceNow connection (ADR-116: the work item table
    /// lives at connection scope so ValidateConnection has something to probe rights against).
    /// </summary>
    public static class ServiceNowWorkTrackingOptionNames
    {
        public const string InstanceUrl = "Instance Url";

        public const string Username = "Username";

        public const string Password = "Password";

        public const string WorkItemTable = "Work Item Table";

        /// <summary>
        /// ITSM-first default (D4). The table stays configurable so an Agile Development 2.0 shop
        /// is not locked out.
        /// </summary>
        public const string DefaultWorkItemTable = "incident";
    }
}
