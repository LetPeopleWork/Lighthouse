using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    public record SystemInfo(
        string Os,
        string Runtime,
        string Architecture,
        int ProcessId,
        string DatabaseProvider,
        string? DatabaseConnection,
        string? LogPath,
        [property: JsonPropertyName("authenticationEnabled")] bool IsAuthenticationEnabled,
        [property: JsonPropertyName("authorizationEnabled")] bool IsAuthorizationEnabled,
        IReadOnlyList<string> EmergencyAdminSubjects,
        string BaseUrl,
        string? InstallTimestamp,
        // Left off the wire entirely rather than sent as null. A property that is present and empty tells
        // a viewer there is something here they are not being shown, which is most of what withholding it
        // was for - and it keeps the answer a viewer gets byte for byte what it was before this field
        // existed.
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Encryption = null)
    {
        // This response answers before anybody is authorised, because the application shell needs the
        // version and the authentication posture to render at all - and a viewer who opens Lighthouse
        // inside an embedded frame satisfies "signed in". Most of what is here is fine to hand to them.
        // Three things are not, and they are named here rather than at the call site so that a fourth one
        // added later is withheld by this sentence instead of by somebody remembering to guard it.
        //
        // The emergency administrators are not a category: they are the names of real people who can
        // administer this installation. Which key it runs on and where that key is kept is the security
        // posture of the whole instance. Where the database lives and what it is called is the address of
        // the one thing worth attacking.
        public SystemInfo WithoutWhatOnlyAnAdministratorMaySee()
        {
            return this with { EmergencyAdminSubjects = [], Encryption = null, DatabaseConnection = null };
        }
    }
}
