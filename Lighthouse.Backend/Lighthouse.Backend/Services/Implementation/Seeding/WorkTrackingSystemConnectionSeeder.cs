using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class WorkTrackingSystemConnectionSeeder(
        LighthouseAppContext context,
        IWorkTrackingSystemFactory workTrackingSystemFactory,
        ILogger<WorkTrackingSystemConnectionSeeder> logger)
        : ISeeder
    {
        private const string AzureDevOpsCsvConnectionName = "CSV Azure DevOps";

        public async Task Seed()
        {
            logger.LogInformation("Seeding WorkTrackingSystemConnections");

            await SeedBuiltInConnections();

            await context.SaveChangesAsync();

            logger.LogInformation("WorkTrackingSystemConnections seeded successfully");
        }

        private async Task SeedBuiltInConnections()
        {
            var hasExistingConnections = await context.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .Include(c => c.AdditionalFieldDefinitions)
                .AnyAsync();

            if (!hasExistingConnections)
            {
                var csvWorkTrackingConnection = workTrackingSystemFactory
                    .CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
                
                csvWorkTrackingConnection.Name = AzureDevOpsCsvConnectionName;
                SetAzureDevOpsConnectionOptions(csvWorkTrackingConnection);

                context.WorkTrackingSystemConnections.Add(csvWorkTrackingConnection);
                
                logger.LogInformation("Created built-in CSV Azure DevOps work tracking connection");
            }
            else
            {
                logger.LogDebug("WorkTrackingSystemConnections already exist, skipping seed");
            }
        }

        private static void SetAzureDevOpsConnectionOptions(WorkTrackingSystemConnection connection)
        {
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.Delimiter, ",");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.DateTimeFormat, "d.M.yyyy HH:mm:ss");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.TagSeparator, ";");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.IdHeader, "ID");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.NameHeader, "Title");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.StateHeader, "State");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.TypeHeader, "Work Item Type");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.StartedDateHeader, "Activated Date");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.ClosedDateHeader, "Closed Date");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.CreatedDateHeader, "Created Date");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.ParentReferenceIdHeader, "Parent");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.TagsHeader, "Tags");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.UrlHeader, "Url");
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.OwningTeamHeader, string.Empty);
            SetWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.EstimatedSizeHeader, string.Empty);
        }

        private static void SetWorkTrackingSystemOption(WorkTrackingSystemConnection connection, string key, string value)
        {
            connection.Options.Single(o => o.Key == key).Value = value;
        }
    }
}