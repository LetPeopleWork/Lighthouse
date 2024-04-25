using Lighthouse.WorkTracking.Jira;
using System.Text.Json;

namespace Lighthouse.Factories
{
    public interface IIssueFactory
    {
        Issue CreateIssueFromJson(JsonElement json);
    }
}