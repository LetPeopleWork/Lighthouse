namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IConcurrencyTokenEntity : IEntity
    {
        Guid ConcurrencyToken { get; set; }
    }
}
