using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// The relation types one organisation lists, each directional type once per end. A reference matches an
    /// end by its reference name in any case, never by the name it is displayed under, because a relation
    /// on a work item carries the reference name and nothing else. Attachments, hyperlinks and artifact
    /// links are listed beside them but never link one work item to another, so nothing can name them.
    /// </summary>
    internal sealed class RelationTypeListing
    {
        private readonly HashSet<string> withoutDirection;

        private RelationTypeListing(List<string> ends, HashSet<string> withoutDirection, bool wasRefused)
        {
            Ends = ends;
            this.withoutDirection = withoutDirection;
            WasRefused = wasRefused;
        }

        public static RelationTypeListing Refused => new([], [], true);

        public List<string> Ends { get; }

        public bool WasRefused { get; }

        public static RelationTypeListing Of(List<WorkItemRelationType> listed)
        {
            var workItemLinks = listed.Where(type => !IsAResourceLink(type)).ToList();

            return new(
                [.. workItemLinks.Select(type => type.ReferenceName).Distinct(StringComparer.OrdinalIgnoreCase)],
                new HashSet<string>(workItemLinks.Where(IsWithoutDirection).Select(type => type.ReferenceName), StringComparer.OrdinalIgnoreCase),
                false);
        }

        public bool Names(string reference) => TheEndCalled(reference) is not null || TheEndsOf(reference).Count > 0;

        public string? TheEndCalled(string reference) => Ends.Find(end => string.Equals(end, reference, StringComparison.OrdinalIgnoreCase));

        // Azure DevOps spells the two ends of a directional type as its base name with -Forward and -Reverse.
        public List<string> TheEndsOf(string baseName)
        {
            var forward = TheEndCalled($"{baseName}-Forward");
            var reverse = TheEndCalled($"{baseName}-Reverse");

            return forward is null || reverse is null ? [] : [forward, reverse];
        }

        public bool HasNoDirection(string end) => withoutDirection.Contains(end);

        private static bool IsWithoutDirection(WorkItemRelationType type)
            => type.Attributes is not null
                && type.Attributes.TryGetValue("directional", out var directional)
                && string.Equals($"{directional}", "false", StringComparison.OrdinalIgnoreCase);

        private static bool IsAResourceLink(WorkItemRelationType type)
            => type.Attributes is not null
                && type.Attributes.TryGetValue("usage", out var usage)
                && string.Equals($"{usage}", "resourceLink", StringComparison.OrdinalIgnoreCase);
    }
}
