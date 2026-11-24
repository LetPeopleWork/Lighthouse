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
        /// A sign-in stored before rights were recorded counts as write-consented: until then every Azure DevOps
        /// sign-in asked for write rights, and those connections must keep writing back.
        /// </summary>
        public WorkItemRights ConsentedRights => GrantedRights ?? WorkItemRights.ReadAndWrite;

        public bool LacksRightsFor(WorkItemRights needed) => needed == WorkItemRights.ReadAndWrite && ConsentedRights == WorkItemRights.Read;
    }
}
