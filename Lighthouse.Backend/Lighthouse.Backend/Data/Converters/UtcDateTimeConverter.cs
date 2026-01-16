using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lighthouse.Backend.Data.Converters
{
    /// <summary>
    /// Entity Framework Core value converter that ensures all DateTime values
    /// are converted to UTC before saving to the database and marked as UTC when reading.
    /// This applies to all DateTime properties mapped through EF Core.
    /// </summary>
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() 
            : base(
                // Convert to UTC when writing to database
                dateTime => dateTime.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
                    : dateTime.ToUniversalTime(),
                
                // Ensure UTC kind when reading from database
                dateTime => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
        {
        }
    }
}
