namespace Lighthouse.Backend.Models.OAuth
{
    /// <summary>What a connection's sign-in may do with work items in the tracker.</summary>
    public enum WorkItemRights
    {
        Read = 0,
        ReadAndWrite = 1,
    }
}
