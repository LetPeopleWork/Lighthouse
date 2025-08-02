using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class LicenseInformation : IEntity
    {
        public int Id => 1;

        public string Name { get; set; }

        public string Email { get; set; }

        public string Organization { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string Signature { get; set; }
    }
}
