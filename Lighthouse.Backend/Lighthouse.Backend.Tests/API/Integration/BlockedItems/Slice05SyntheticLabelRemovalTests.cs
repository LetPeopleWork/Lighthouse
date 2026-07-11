using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenario (Epic 5074) — Slice 05, AC3: the synthetic "Flagged" label injection is
    /// removed. This is the behavioural equivalent of the ADR-071 "grep asserts no FlaggedName label wiring"
    /// enforcement rule: <see cref="IssueFactory"/> must NOT push a synthetic <c>JiraFieldNames.FlaggedName</c>
    /// label onto an issue's Labels when the flagged custom field is set — the flag now flows only through the
    /// predefined additional field (generic id-keyed path).
    ///
    /// [Ignore]-pending: enable in DELIVER after deleting the IssueFactory L32–40 label injection. It fails RED
    /// today on a clean assertion (the synthetic label IS added). It compiles against today's types (no
    /// reference to the not-yet-existing IsPredefined member).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-05")]
    public class Slice05SyntheticLabelRemovalTests
    {
        private const string FlaggedFieldReference = "customfield_10001";

        // @error @us-05 (AC3 — no synthetic "Flagged" label injection remains)
        [Test]
        [Ignore("DELIVER slice-05 — IssueFactory still injects the synthetic Flagged label (L32-40); the flag must flow only through the predefined additional field")]
        public void A_flagged_jira_issue_is_built_without_a_synthetic_flagged_label()
        {
            var owner = new FlaggedFieldQueryOwner();
            var json = CreateFlaggedIssueJson();

            var issue = new IssueFactory(Mock.Of<ILogger<IssueFactory>>())
                .CreateIssueFromJson(json.RootElement, owner, flaggedField: FlaggedFieldReference);

            Assert.That(issue.Labels, Does.Not.Contain(JiraFieldNames.FlaggedName),
                "IssueFactory must not inject a synthetic \"Flagged\" label — the Jira flag is consumed only as the " +
                "predefined additional field, so no synthetic label may appear on the issue's Labels.");
        }

        private static JsonDocument CreateFlaggedIssueJson()
        {
            var issue = new JsonObject
            {
                [JiraFieldNames.KeyPropertyName] = "PHX-300",
                [JiraFieldNames.ChangelogFieldName] = new JsonObject
                {
                    ["startAt"] = 0,
                    [JiraFieldNames.MaxResultsFieldName] = 100,
                    [JiraFieldNames.TotalFieldName] = 0,
                    [JiraFieldNames.HistoriesFieldName] = new JsonArray(),
                },
                [JiraFieldNames.FieldsFieldName] = new JsonObject
                {
                    [JiraFieldNames.SummaryFieldName] = "Story 300",
                    [JiraFieldNames.CreatedDateFieldName] = "2026-06-01T09:40:12.704+0200",
                    [JiraFieldNames.LabelsFieldName] = new JsonArray("Lagunitas"),
                    [JiraFieldNames.IssueTypeFieldName] = new JsonObject { [JiraFieldNames.NamePropertyName] = "Story" },
                    [JiraFieldNames.StatusFieldName] = new JsonObject
                    {
                        [JiraFieldNames.NamePropertyName] = "In Progress",
                        ["statusCategory"] = new JsonObject { [JiraFieldNames.NamePropertyName] = "In Progress" },
                    },
                    // The flagged custom field is SET — this is what triggers the synthetic-label injection today.
                    [FlaggedFieldReference] = new JsonObject { ["value"] = "Impediment" },
                },
            };

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            issue.WriteTo(writer);
            writer.Flush();

            return JsonDocument.Parse(stream.ToArray());
        }

        private sealed class FlaggedFieldQueryOwner : WorkTrackingSystemOptionsOwner
        {
            public FlaggedFieldQueryOwner()
            {
                ToDoStates = ["New"];
                DoingStates = ["In Progress"];
                DoneStates = ["Done"];
            }

            public override List<string> WorkItemTypes { get; set; } = ["Story"];

            public override int DoneItemsCutoffDays { get; set; } = 180;
        }
    }
}
