using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using System.Text.Json;

namespace Lighthouse.Backend.Factories
{
    public interface IIssueFactory
    {
        Issue CreateIssueFromJson(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string? additionalRelatedField = null, string? rankFieldName = null, string? flaggedField = null);
    }
}