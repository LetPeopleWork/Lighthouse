using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class TerminologyRepository : RepositoryBase<TerminologyEntry>, IRepository<TerminologyEntry>
    {
        public TerminologyRepository(LighthouseAppContext context, ILogger<TerminologyRepository> logger) 
            : base(context, (LighthouseAppContext context) => context.TerminologyEntries, logger)
        {
            SeedTerminology();
        }

        private void SeedTerminology()
        {
            AddIfNotExists("workItem", "Work Item", "Units of Value that move through your system and that your teams work on. Alternatives may be 'Story' or 'Issue'");
            AddIfNotExists("workItems", "Work Items", "Plural form of 'Work Item'");
            AddIfNotExists("feature", "Feature", "Larger unit of work that contains multiple Work Items. Alternatives may be 'Epic' or 'Theme'");
            AddIfNotExists("features", "Features", "Plural form of 'Feature'");
            AddIfNotExists("cycleTime", "Cycle Time", "The elapsed time between when a work item started and when a work item finished. Alternatives may be 'Lead Time' or 'Flow Time'");
            AddIfNotExists("throughput", "Throughput", "The number of work items finished per unit of time. Alternatives may be 'Delivery Rate' or 'Flow Velocity'");
            AddIfNotExists("workInProgress", "Work In Progress", "The number of work items started but not finished. Alternatives may be 'Flow Load' or 'Ongoing Stuff'");
            AddIfNotExists("wip", "WIP", "Abbreviation of 'Work In Progress'.");
            AddIfNotExists("workItemAge", "Work Item Age", "The elapsed time between when a work item started and the current date. Alternatives may be 'Age' or 'In Progress Time'");
            AddIfNotExists("tag", "Tag", "A user defined indication on your 'Work Items'. Alternatives may be 'Label' or 'Category'");
            AddIfNotExists("workTrackingSystem", "Work Tracking System", "Generic name of the source of your data. Alternatives may be 'Jira Instance' or 'Azure DevOps Organization'");
            AddIfNotExists("workTrackingSystems", "Work Tracking Systems", "Plural form of 'Work Tracking System'");
            AddIfNotExists("workItemQuery", "Work Item Query", "Query that is applied on your 'Work Tracking System' for filtering. Alternatives may be 'JQL' or 'WIQL'");
            AddIfNotExists("blocked", "Blocked", "Indication for 'Work Items' that don't progress anymore. Alternatives may be 'On Hold' or 'Stopped'");
            AddIfNotExists("serviceLevelExpectation", "Service Level Expectation", "A forecast of how long it should take a work item to flow from started to finished. Alternatives may be 'Target' or 'Goal'");
            AddIfNotExists("sle", "SLE", "Abbreviation of 'Service Level Expectation'");
            AddIfNotExists("team", "Team", "The smallest groups in the organization that deliver 'Work Items'. Alternatives may be 'Squad' or 'Crew'");
            AddIfNotExists("teams", "Teams", "Plural form of 'Team'");

            SaveSync();
        }

        private void AddIfNotExists(string key, string defaultValue, string descrption)
        {
            var terminologyEntry = new TerminologyEntry
            {
                Key = key,
                DefaultValue = defaultValue,
                Description = descrption
            };

            var existingEntry = GetByPredicate((entry) => entry.Key == terminologyEntry.Key);
            if (existingEntry == null)
            {
                Add(terminologyEntry);
            }
            else
            {
                existingEntry.DefaultValue = terminologyEntry.DefaultValue;
                existingEntry.Description = terminologyEntry.Description;
                Update(existingEntry);
            }
        }
    }
}
