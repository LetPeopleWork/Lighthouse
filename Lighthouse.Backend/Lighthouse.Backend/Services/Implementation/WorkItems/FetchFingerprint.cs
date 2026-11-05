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
        /// The registered properties that live on the connection rather than on a Team or Portfolio, and so
        /// change for every owner on the connection at once. <see cref="ForConnection"/> renders these, plus the
        /// Jira reading version for a Jira connection.
        /// Declared first because static initialisers run top to bottom and the full list below includes it.
        /// </summary>
        public static IReadOnlyCollection<string> ConnectionScopedProperties { get; } =
        [
            nameof(WorkTrackingSystemConnection.AdditionalFieldDefinitions),
            nameof(WorkTrackingSystemConnection.WorkTrackingSystem),
        ];

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
            nameof(WorkTrackingSystemOptionsOwner.ParentSources),
            nameof(Portfolio.FeatureOwnerAdditionalFieldDefinitionId),
            nameof(Portfolio.SizeEstimateAdditionalFieldDefinitionId),
            nameof(Portfolio.DependencyOverrideAdditionalFieldDefinitionId),
            .. ConnectionScopedProperties,

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

            string[] whatIsAskedForAndHowTheAnswerIsRead =
            [
                queryOwner.DataRetrievalValue,
                Unordered(queryOwner.WorkItemTypes, BetweenItems),
                Render(queryOwner.DoneItemsCutoffDays),
                Unordered(queryOwner.ToDoStates, BetweenItems),
                Unordered(queryOwner.DoingStates, BetweenItems),
                Unordered(queryOwner.DoneStates, BetweenItems),
                Render(queryOwner.StateMappings),
                RenderParentSources(queryOwner.TheEffectiveParentSources()),
                Render(portfolio?.FeatureOwnerAdditionalFieldDefinitionId),
                Render(portfolio?.SizeEstimateAdditionalFieldDefinitionId),
                Render(portfolio?.DependencyOverrideAdditionalFieldDefinitionId),
                ForConnection(queryOwner.WorkTrackingSystemConnection),
                Render(queryOwner.WorkTrackingSystemConnectionId),
            ];

            return Digest(string.Join(BetweenProperties, whatIsAskedForAndHowTheAnswerIsRead));
        }

        /// <summary>
        /// A list keeps its order here, unlike the collections above, because the same sources in another order
        /// name another parent.
        /// </summary>
        /// <remarks>
        /// The tracker's own parent alone keeps the text of "no override": that list holds no link entry, so its
        /// meaning cannot have moved and its owners are not re-read. Every other list starts with a letter, so it
        /// can never equal the digits or the dash an old fingerprint rendered.
        /// </remarks>
        public static string RenderParentSources(ParentSources parentSources)
            => parentSources.Entries is [{ Kind: ParentSourceKind.TrackerParent }]
                ? Absent
                : string.Join(BetweenParentSources, parentSources.Entries.Select(Render));

        /// <summary>
        /// The connection's share of <see cref="For"/>: what changes for every Team and Portfolio on
        /// <paramref name="connection"/> at once.
        /// </summary>
        /// <remarks>
        /// Rendered, not digested, and joined with the same separator <see cref="For"/> uses between its
        /// properties: <see cref="For"/> splices this answer in where the two properties used to sit, so a
        /// fingerprint already stored keeps matching after an upgrade. A Jira connection is the one deliberate
        /// exception: its reading version changes every Jira fingerprint once, so each Jira owner is re-read.
        /// </remarks>
        public static string ForConnection(WorkTrackingSystemConnection? connection)
            => connection?.WorkTrackingSystem == WorkTrackingSystems.Jira
                ? string.Join(BetweenProperties, RenderedWithoutReadingVersion(connection), JiraReadingVersion)
                : RenderedWithoutReadingVersion(connection);

        private static string RenderedWithoutReadingVersion(WorkTrackingSystemConnection? connection)
            => string.Join(
                BetweenProperties,
                Render(connection?.AdditionalFieldDefinitions),
                connection?.WorkTrackingSystem.ToString() ?? Absent);

        /// <summary>
        /// Whether a parent source of a Jira owner is a field or a link type is known only when reading, so the
        /// move to ordered sources cannot tell which owners it changes and must re-read every item once. An
        /// unchanged item is otherwise skipped, so the marker changes every Jira fingerprint one time. Raise it
        /// only to force such a re-read again.
        /// </summary>
        private const string JiraReadingVersion = "parent-sources-2";

        private const string Absent = "-";

        private const char BetweenProperties = '\u001E';

        private const char BetweenItems = '\u001F';

        private const char WithinAnItem = '\u001D';

        private const char BetweenNestedItems = '\u001C';

        private const char BetweenParentSources = ',';

        private static string Render(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Render(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? Absent;

        private static string Render(ParentSourceEntry entry)
            => entry.AdditionalFieldDefinitionId is { } field ? $"F:{Render(field)}" : "P";

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
