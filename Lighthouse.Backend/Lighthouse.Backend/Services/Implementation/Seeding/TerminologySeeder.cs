using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class TerminologySeeder(LighthouseAppContext context, ILogger<TerminologySeeder> logger)
        : ISeeder
    {
        public async Task Seed()
        {
            logger.LogInformation("Seeding Terminology");

            await AddOrUpdateTerminology();
            await RemoveDeprecatedTerminology();

            await context.SaveChangesAsync();

            logger.LogInformation("Terminology seeded successfully");
        }

        private async Task AddOrUpdateTerminology()
        {
            var terminologyEntries = new[]
            {
                new TerminologyEntry
                {
                    Key = "workItem",
                    DefaultValue = "Work Item",
                    Description = "Units of Value that move through your system and that your teams work on. Alternatives may be 'Story' or 'Issue'"
                },
                new TerminologyEntry
                {
                    Key = "workItems",
                    DefaultValue = "Work Items",
                    Description = "Plural form of 'Work Item'"
                },
                new TerminologyEntry
                {
                    Key = "feature",
                    DefaultValue = "Feature",
                    Description = "Larger unit of work that contains multiple Work Items. Alternatives may be 'Epic' or 'Theme'"
                },
                new TerminologyEntry
                {
                    Key = "features",
                    DefaultValue = "Features",
                    Description = "Plural form of 'Feature'"
                },
                new TerminologyEntry
                {
                    Key = "cycleTime",
                    DefaultValue = "Cycle Time",
                    Description = "The elapsed time between when a work item started and when a work item finished. Alternatives may be 'Lead Time' or 'Flow Time'"
                },
                new TerminologyEntry
                {
                    Key = "throughput",
                    DefaultValue = "Throughput",
                    Description = "The number of work items finished per unit of time. Alternatives may be 'Delivery Rate' or 'Flow Velocity'"
                },
                new TerminologyEntry
                {
                    Key = "workInProgress",
                    DefaultValue = "Work In Progress",
                    Description = "The number of work items started but not finished. Alternatives may be 'Flow Load' or 'Ongoing Stuff'"
                },
                new TerminologyEntry
                {
                    Key = "wip",
                    DefaultValue = "WIP",
                    Description = "Abbreviation of 'Work In Progress'."
                },
                new TerminologyEntry
                {
                    Key = "workItemAge",
                    DefaultValue = "Work Item Age",
                    Description = "The elapsed time between when a work item started and the current date. Alternatives may be 'Age' or 'In Progress Time'"
                },
                new TerminologyEntry
                {
                    Key = "tag",
                    DefaultValue = "Tag",
                    Description = "A user defined indication on your 'Work Items'. Alternatives may be 'Label' or 'Category'"
                },
                new TerminologyEntry
                {
                    Key = "workTrackingSystem",
                    DefaultValue = "Work Tracking System",
                    Description = "Generic name of the source of your data. Alternatives may be 'Jira Instance' or 'Azure DevOps Organization'"
                },
                new TerminologyEntry
                {
                    Key = "workTrackingSystems",
                    DefaultValue = "Work Tracking Systems",
                    Description = "Plural form of 'Work Tracking System'"
                },
                new TerminologyEntry
                {
                    Key = "blocked",
                    DefaultValue = "Blocked",
                    Description = "Indication for 'Work Items' that don't progress anymore. Alternatives may be 'On Hold' or 'Stopped'"
                },
                new TerminologyEntry
                {
                    Key = "serviceLevelExpectation",
                    DefaultValue = "Service Level Expectation",
                    Description = "A forecast of how long it should take a work item to flow from started to finished. Alternatives may be 'Target' or 'Goal'"
                },
                new TerminologyEntry
                {
                    Key = "sle",
                    DefaultValue = "SLE",
                    Description = "Abbreviation of 'Service Level Expectation'"
                },
                new TerminologyEntry
                {
                    Key = "team",
                    DefaultValue = "Team",
                    Description = "The smallest groups in the organization that deliver 'Work Items'. Alternatives may be 'Squad' or 'Crew'"
                },
                new TerminologyEntry
                {
                    Key = "teams",
                    DefaultValue = "Teams",
                    Description = "Plural form of 'Team'"
                },
                new TerminologyEntry
                {
                    Key = "portfolio",
                    DefaultValue = "Portfolio",
                    Description = "Collection of work items that belong together and are managed as a unit. Alternatives may be 'Project' or 'Initiative'"
                },
                new TerminologyEntry
                {
                    Key = "portfolios",
                    DefaultValue = "Portfolios",
                    Description = "Plural form of 'Portfolio'"
                },
                new TerminologyEntry
                {
                    Key = "delivery",
                    DefaultValue = "Delivery",
                    Description = "A delivery marks a specific point in time where a defined list of Features should be done. Alternative names may be Milestone, Checkpoint, etc."
                },
                new TerminologyEntry
                {
                    Key = "deliveries",
                    DefaultValue = "Deliveries",
                    Description = "Plural form of 'Delivery'"
                }
            };

            foreach (var entry in terminologyEntries)
            {
                var existing = await context.TerminologyEntries
                    .FirstOrDefaultAsync(t => t.Key == entry.Key);

                if (existing == null)
                {
                    context.TerminologyEntries.Add(entry);
                    logger.LogDebug("Adding TerminologyEntry: {Key}", entry.Key);
                }
                else
                {
                    // Update the default value and description
                    existing.DefaultValue = entry.DefaultValue;
                    existing.Description = entry.Description;
                    logger.LogDebug("Updating TerminologyEntry: {Key}", entry.Key);
                }
            }
        }

        private async Task RemoveDeprecatedTerminology()
        {
            var deprecatedKeys = new[] { "workItemQuery", "query" };

            var toRemove = await context.TerminologyEntries
                .Where(t => deprecatedKeys.Contains(t.Key))
                .ToListAsync();

            if (toRemove.Count > 0)
            {
                context.TerminologyEntries.RemoveRange(toRemove);
                logger.LogInformation("Removing {Count} deprecated TerminologyEntries", toRemove.Count);
            }
        }
    }
}