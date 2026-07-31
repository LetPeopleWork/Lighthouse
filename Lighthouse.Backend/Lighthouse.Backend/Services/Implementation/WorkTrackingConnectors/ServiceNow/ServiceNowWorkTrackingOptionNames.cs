namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Connection-scope option keys for a ServiceNow connection. The table a team reads is not among
    /// them: every read is rooted at <see cref="ServiceNowReadScope.RootTable"/> (ADR-116 decision 1,
    /// withdrawn 2026-07-31).
    /// </summary>
    public static class ServiceNowWorkTrackingOptionNames
    {
        public const string InstanceUrl = "Instance Url";

        public const string Username = "Username";

        public const string Password = "Password";
    }
}
