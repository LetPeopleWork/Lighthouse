using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Turns the answer of Jira's project versions endpoints into the sources a Delivery can bind to.
    /// Kept apart from the connector and free of any I/O, so the shapes that are awkward to arrange on a
    /// real board - a Release nobody dated, a Release somebody archived - can be specified directly.
    /// </summary>
    public static class JiraReleaseVersionReader
    {
        public static IReadOnlyList<DeliverySourceOption> ReadOptions(string versionsPayload, DeliverySourceProject project)
        {
            using var payload = JsonDocument.Parse(versionsPayload);

            return [.. payload.RootElement.EnumerateArray().Select(version => ToOption(version, project))];
        }

        /// <summary>
        /// The projects the credential can see, as key and name.
        /// </summary>
        public static (IReadOnlyList<DeliverySourceProject> Projects, bool IsLastPage) ReadProjectPage(string projectPagePayload)
            => ReadPage(projectPagePayload, project => new DeliverySourceProject(ReadText(project, "key"), ReadText(project, "name")));

        /// <summary>
        /// One page of a project's versions. The paginated endpoint wraps the same version objects the bare
        /// list returns, so this is the wrapper coming off rather than a second way of reading a version.
        /// </summary>
        public static (IReadOnlyList<DeliverySourceOption> Options, bool IsLastPage) ReadOptionPage(string versionPagePayload, DeliverySourceProject project)
            => ReadPage(versionPagePayload, version => ToOption(version, project));

        /// <summary>
        /// Jira answers these one page at a time and says on every page whether it was the last, so a caller
        /// following that flag is handed the whole set rather than a first page that quietly looks complete.
        /// </summary>
        private static (IReadOnlyList<T> Values, bool IsLastPage) ReadPage<T>(string pagePayload, Func<JsonElement, T> readOne)
        {
            using var payload = JsonDocument.Parse(pagePayload);
            var root = payload.RootElement;

            var isLastPage = !root.TryGetProperty("isLast", out var isLast) || isLast.ValueKind != JsonValueKind.False;

            if (!root.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
            {
                return ([], true);
            }

            IReadOnlyList<T> parsed = [.. values.EnumerateArray().Select(readOne)];

            return (parsed, isLastPage);
        }

        private static DeliverySourceOption ToOption(JsonElement version, DeliverySourceProject project)
        {
            var date = ReadReleaseDate(version);
            var isRetiredAtSource = ReadFlag(version, "archived");

            return new DeliverySourceOption(
                ReadText(version, "id"),
                ReadText(version, "name"),
                date,
                project,
                isRetiredAtSource,
                ReadFlag(version, "released"),
                DeliverySourceBindability.For(date.HasValue, isRetiredAtSource));
        }

        /// <summary>
        /// Jira leaves the releaseDate key out of a version entirely when nobody has set a date, rather
        /// than sending null. That is the ordinary case - most Releases on a board carry no date at all -
        /// so a missing key is the answer "no date", never a payload that could not be read.
        /// </summary>
        private static DateTime? ReadReleaseDate(JsonElement version)
        {
            if (!version.TryGetProperty("releaseDate", out var releaseDate))
            {
                return null;
            }

            var parsed = DateTime.TryParse(
                releaseDate.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var date);

            return parsed ? date : null;
        }

        private static string ReadText(JsonElement version, string name)
            => version.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

        private static bool ReadFlag(JsonElement version, string name)
            => version.TryGetProperty(name, out var flag) && flag.ValueKind == JsonValueKind.True;
    }
}
