namespace Lighthouse.Backend.Models
{
    public interface IConcurrencyTokenEntity : IEntity
    {
        Guid ConcurrencyToken { get; set; }
    }
}
