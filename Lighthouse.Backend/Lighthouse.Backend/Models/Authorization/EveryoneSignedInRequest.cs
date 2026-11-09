namespace Lighthouse.Backend.Models.Authorization
{
    public class EveryoneSignedInRequest
    {
        public required bool EveryoneCanView { get; init; }

        public Guid? ConcurrencyToken { get; set; }
    }
}
