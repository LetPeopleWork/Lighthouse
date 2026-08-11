using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    /// <summary>
    /// What the last cycle asked the tracker for, and how it read the answer.
    ///
    /// Two callers share this one list: the sync decides whether the next cycle must download the whole
    /// query, and the settings-save path decides whether the stored records must be discarded first. If
    /// they ever drift into two lists, the shorter one wins silently and the cheap path serves stale data.
    ///
    /// Membership is "what the query asks for" plus "how the answer is read into the stored record" - a
    /// cheap cycle skips an unchanged record's whole derivation, so a state mapping or a connection field
    /// definition shapes the result just as much as the query text does.
    /// </summary>
    public static class FetchFingerprint
    {
        /// <summary>
        /// Every property whose change makes the next cycle download the whole query. The guard test
        /// <c>FetchShapingPropertyGuardTest</c> records why each one is here, and why every other property
        /// an operator can edit is not.
        /// </summary>
        public static IReadOnlyCollection<string> RegisteredProperties { get; } =
        [
            // What the query asks the tracker for.
            nameof(WorkTrackingSystemOptionsOwner.DataRetrievalValue),
            nameof(WorkTrackingSystemOptionsOwner.WorkItemTypes),
            nameof(WorkTrackingSystemOptionsOwner.DoneItemsCutoffDays),
            nameof(WorkTrackingSystemOptionsOwner.ToDoStates),
            nameof(WorkTrackingSystemOptionsOwner.DoingStates),
            nameof(WorkTrackingSystemOptionsOwner.DoneStates),

            // How the answer is read into the stored record.
            nameof(WorkTrackingSystemOptionsOwner.StateMappings),
            nameof(WorkTrackingSystemOptionsOwner.ParentOverrideAdditionalFieldDefinitionId),
            nameof(Portfolio.FeatureOwnerAdditionalFieldDefinitionId),
            nameof(Portfolio.SizeEstimateAdditionalFieldDefinitionId),
            nameof(WorkTrackingSystemConnection.AdditionalFieldDefinitions),
            nameof(WorkTrackingSystemConnection.WorkTrackingSystem),

            nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId),
        ];

        /// <summary>
        /// The subset that ALSO costs a fresh start at save time. Only a connection change belongs here:
        /// it is the only edit that makes the same reference id a different item, and therefore the only
        /// one <c>removed = stored - fetched</c> cannot reconcile on the next full cycle.
        /// </summary>
        public static IReadOnlyCollection<string> PropertiesThatAlsoCostAFreshStart { get; } =
        [
            nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId),
        ];

        /// <summary>
        /// The fingerprint of what <paramref name="queryOwner"/> would ask for on its next cycle.
        /// </summary>
        /// <remarks>
        /// A stored fingerprint has to still match after a restart and after an upgrade, so nothing here
        /// may reach for <c>GetHashCode</c> (randomised per process) or object identity: the input is
        /// rendered, sorted and digested. The two portfolio-only references arrive by pattern match rather
        /// than by widening <see cref="IWorkItemQueryOwner"/> - a team would carry them as dead surface,
        /// and a second entry point would let the registry above stop meaning one function.
        /// </remarks>
        public static string For(IWorkItemQueryOwner queryOwner)
        {
            var portfolio = queryOwner as Portfolio;
            var connection = queryOwner.WorkTrackingSystemConnection;

            string[] whatIsAskedForAndHowTheAnswerIsRead =
            [
                queryOwner.DataRetrievalValue,
                Unordered(queryOwner.WorkItemTypes, BetweenItems),
                Render(queryOwner.DoneItemsCutoffDays),
                Unordered(queryOwner.ToDoStates, BetweenItems),
                Unordered(queryOwner.DoingStates, BetweenItems),
                Unordered(queryOwner.DoneStates, BetweenItems),
                Render(queryOwner.StateMappings),
                Render(queryOwner.ParentOverrideAdditionalFieldDefinitionId),
                Render(portfolio?.FeatureOwnerAdditionalFieldDefinitionId),
                Render(portfolio?.SizeEstimateAdditionalFieldDefinitionId),
                Render(connection?.AdditionalFieldDefinitions),
                connection?.WorkTrackingSystem.ToString() ?? Absent,
                Render(queryOwner.WorkTrackingSystemConnectionId),
            ];

            return Digest(string.Join(BetweenProperties, whatIsAskedForAndHowTheAnswerIsRead));
        }

        private const string Absent = "-";

        private const char BetweenProperties = '\u001E';

        private const char BetweenItems = '\u001F';

        private const char WithinAnItem = '\u001D';

        private const char BetweenNestedItems = '\u001C';

        private static string Render(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Render(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? Absent;

        /// <summary>
        /// Order-insensitive over the mappings AND over each mapping's raw states: neither carries meaning,
        /// and EF returns them in whatever order the query produced.
        /// </summary>
        private static string Render(IEnumerable<StateMapping> stateMappings)
            => Unordered(
                stateMappings.Select(mapping => string.Join(WithinAnItem, mapping.Name, Unordered(mapping.States, BetweenNestedItems))),
                BetweenItems);

        /// <summary>
        /// Rendered by what the definition decides - which field is requested and under which name it is
        /// stored - rather than by object identity, so a re-materialised connection hashes the same.
        /// </summary>
        private static string Render(IEnumerable<AdditionalFieldDefinition>? fieldDefinitions)
            => fieldDefinitions is null
                ? Absent
                : Unordered(
                    fieldDefinitions.Select(definition => string.Join(
                        WithinAnItem,
                        Render(definition.Id),
                        definition.Reference,
                        definition.DisplayName,
                        definition.IsPredefined)),
                    BetweenItems);

        private static string Unordered(IEnumerable<string> renderedItems, char separator)
            => string.Join(separator, renderedItems.OrderBy(item => item, StringComparer.Ordinal));

        private static string Digest(string renderedInput)
            => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(renderedInput)).AsSpan(0, 16));
    }
}
