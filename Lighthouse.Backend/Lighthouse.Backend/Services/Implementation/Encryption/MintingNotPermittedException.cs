using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Raised where the key in force belongs to whoever runs the instance rather than to Lighthouse. A key
    // made here would be written where the supplied one wins again on the next start, and everything moved
    // onto it would be unreadable from then on - a rotation that un-rotates itself is worse than no rotation
    // button at all.
    public sealed class MintingNotPermittedException : InvalidOperationException
    {
        public MintingNotPermittedException(KeyCustody custody)
            : base(WhyNot(custody))
        {
        }

        // Where the reason is not who owns the key. The caller supplies the whole sentence, because an
        // administrator turned down has to be told what to do next and only the caller knows.
        public MintingNotPermittedException(string message)
            : base(message)
        {
        }

        private static string WhyNot(KeyCustody custody)
        {
            var whoOwnsIt = custody switch
            {
                KeyCustody.SuppliedByConfiguration =>
                    "The encryption key was supplied to this instance through its configuration, so it belongs to " +
                    "whoever set it. ",
                KeyCustody.SuppliedByExternalSecret =>
                    "The encryption key was supplied to this instance from a mounted secret, so it belongs to " +
                    "whoever keeps that secret. ",
                _ =>
                    "This instance has nowhere it could keep a key that would still be there after a restart. ",
            };

            return whoOwnsIt +
                "Lighthouse will not make a new one, because it would be written where the supplied key wins " +
                "again on the next start and every secret moved onto it would be unreadable from then on. Add " +
                "the new key ahead of the old one where the key comes from, start Lighthouse again, and then " +
                "move the stored secrets onto it.";
        }
    }
}
