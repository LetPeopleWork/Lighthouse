namespace Lighthouse.Backend.Data
{
    public class DatabaseConfiguration
    {
        public string Provider { get; set; } = "Sqlite"; // Default to SQLite
        
        public string ConnectionString { get; set; } = "Data Source=LighthouseAppContext.db";
    }
}