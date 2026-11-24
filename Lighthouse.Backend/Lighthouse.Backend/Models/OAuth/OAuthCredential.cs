namespace Lighthouse.Backend.Models.OAuth
{
    public class OAuthCredential : IEntity
    {
        public int Id { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public OAuthCredentialStatus Status { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>The rights the identity platform granted at the last sign-in; null when that sign-in predates recording them.</summary>
        public WorkItemRights? GrantedRights { get; set; }

        /// <summary>
        /// A sign-in stored before rights were recorded counts by what it asked for: an Azure DevOps sign-in always
        /// asked to write, so it keeps writing back; a Jira sign-in asked only to read.
        /// </summary>
        public WorkItemRights ConsentedRightsOn(WorkTrackingSystemConnection connection)
            => GrantedRights ?? (connection.WorkTrackingSystem == WorkTrackingSystems.Jira ? WorkItemRights.Read : WorkItemRights.ReadAndWrite);

        public bool LacksRightsFor(WorkTrackingSystemConnection connection)
            => connection.NeededWorkItemRights == WorkItemRights.ReadAndWrite && ConsentedRightsOn(connection) == WorkItemRights.Read;
    }
}
