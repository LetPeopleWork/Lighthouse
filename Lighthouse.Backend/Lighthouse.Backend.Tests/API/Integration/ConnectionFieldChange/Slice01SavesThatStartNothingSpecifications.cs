namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Step definitions for the saves that start nothing. The shared ones are in the harness.
    /// </summary>
    public partial class Slice01SavesThatStartNothingTest : ConnectionFieldChangeAcceptanceTest
    {
        /// <summary>
        /// The field Jira offers on every connection, registered by Lighthouse rather than by the admin.
        /// </summary>
        private const string Releases = "Releases";

        private const string ReleasesReference = "fixVersions";
    }
}
