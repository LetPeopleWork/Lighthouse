using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Turns the answer of Jira's project versions endpoint into the sources a Delivery can bind to.
    /// Kept apart from the connector and free of any I/O, so the shapes that are awkward to arrange on a
    /// real board - a Release nobody dated, a Release somebody archived - can be specified directly.
    /// </summary>
    public static class JiraReleaseVersionReader
    {
        public static IReadOnlyList<DeliverySourceOption> ReadOptions(string versionsPayload)
        {
            using var payload = JsonDocument.Parse(versionsPayload);

            return [.. payload.RootElement.EnumerateArray().Select(ToOption)];
        }

        private static DeliverySourceOption ToOption(JsonElement version)
        {
            var date = ReadReleaseDate(version);
            var isRetiredAtSource = ReadFlag(version, "archived");

            return new DeliverySourceOption(
                ReadText(version, "id"),
                ReadText(version, "name"),
                date,
                isRetiredAtSource,
                ReadFlag(version, "released"),
                DeliverySourceBindability.IsSelectable(date.HasValue, isRetiredAtSource),
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
