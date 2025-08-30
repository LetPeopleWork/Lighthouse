using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepository : RepositoryBase<WorkTrackingSystemConnection>
    {
        private readonly ILogger<WorkTrackingSystemConnectionRepository> logger;
        private readonly IWorkTrackingSystemFactory workTrackingSystemFactory;

        private readonly string AzureDevOpsCsvConnectionName = "CSV Azure DevOps";

        public WorkTrackingSystemConnectionRepository(
            LighthouseAppContext context, ILogger<WorkTrackingSystemConnectionRepository> logger, IWorkTrackingSystemFactory workTrackingSystemFactory)
            : base(context, (context) => context.WorkTrackingSystemConnections, logger)
        {
            this.logger = logger;
            this.workTrackingSystemFactory = workTrackingSystemFactory;
            SeedBuiltInConnections();
        }

        public override IEnumerable<WorkTrackingSystemConnection> GetAll()
        {
            return Context.WorkTrackingSystemConnections
                .Include(c => c.Options);
        }

        public override WorkTrackingSystemConnection? GetById(int id)
        {
            return GetAll().SingleOrDefault(t => t.Id == id);
        }

        private void SeedBuiltInConnections()
        {
            var csvAdoConnection = GetByPredicate(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv && c.Name == AzureDevOpsCsvConnectionName);
            if (csvAdoConnection == null)
            {
                var csvWorkTrackingConnection = workTrackingSystemFactory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
                csvWorkTrackingConnection.Name = AzureDevOpsCsvConnectionName;
                SetAzureDevOpsConnectionOptions(csvWorkTrackingConnection);

                Add(csvWorkTrackingConnection);
                SaveSync();
                logger.LogInformation("Created built-in CSV Azure DevOps work tracking connection");
            }
        }

        private void SetAzureDevOpsConnectionOptions(WorkTrackingSystemConnection connection)
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

        private void SetWorkTrackingSystemOption(WorkTrackingSystemConnection connection, string key, string value)
        {
            connection.Options.Single(o => o.Key == key).Value = value;
        }
    }
}
