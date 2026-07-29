namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    public enum WorkTrackingSystems
    {
        AzureDevOps,

        Jira,

        Linear,

        Csv,

        // Append-only. EF persists this property as an int (no HasConversion anywhere in
        // LighthouseAppContext), so inserting a member above silently repoints every stored
        // connection. ServiceNowConnectionConfigurationTest pins the ordinal.
        ServiceNow,
    }
}
