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
        /// The text a Release already carries, or nothing when it carries none. A Release with no
        /// description at all is the ordinary case - every Release on the demo instance was like that - so
        /// the key simply not being there is an answer rather than a payload that could not be read.
        /// </summary>
        public static string? ReadVersionDescription(string versionPayload)
        {
            using var payload = JsonDocument.Parse(versionPayload);

            // Read as text only when it is text. A description that came back as anything else is not a
            // description Lighthouse can merge into, and asking for the string would throw where the
            // honest answer is that there is nothing here to keep.
            return payload.RootElement.TryGetProperty("description", out var description)
                && description.ValueKind == JsonValueKind.String
                ? description.GetString()
                : null;
        }

        /// <summary>
        /// What Jira said when it would not take a write, in its own words - which already name what to
        /// fix in the vocabulary the administrator will search for. Nothing when the body is not a refusal
        /// Jira wrote, because inventing a sentence here would put words in its mouth that nobody can look
        /// up.
        ///
        /// Both halves of Jira's error envelope are read. A refusal about the request as a whole arrives
        /// in <c>errorMessages</c>; one about a particular field arrives in <c>errors</c> with
        /// <c>errorMessages</c> empty, and that is the shape of the refusal this write can actually
        /// provoke - a description that would go over Jira's size ceiling. Reading only the first half
        /// would leave the one refusal an administrator can act on reported as a bare status line.
        /// </summary>
        public static string? ReadRefusalMessage(string refusalPayload)
        {
            try
            {
                using var payload = JsonDocument.Parse(refusalPayload);

                return AboutTheRequest(payload.RootElement) ?? AboutAField(payload.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? AboutTheRequest(JsonElement refusal)
        {
            if (!refusal.TryGetProperty("errorMessages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return messages.EnumerateArray()
                .Where(message => message.ValueKind == JsonValueKind.String)
                .Select(message => message.GetString())
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
        }

        private static string? AboutAField(JsonElement refusal)
        {
            if (!refusal.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return errors.EnumerateObject()
                .Where(field => field.Value.ValueKind == JsonValueKind.String)
                .Select(field => field.Value.GetString())
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
        }

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
