namespace Lighthouse.Backend.Models.Encryption
{
    // Who owns the key an instance is running on, which decides whether Lighthouse is able to create a
    // replacement at all. Only where Lighthouse wrote the key itself can it write a new one and have that
    // survive a restart; anywhere else the value the operator supplied wins again on the next start, and a
    // key minted over it would take every secret written under it out of reach.
    // The first value is the one an unnamed custody falls back to, so a ring that was never told where its
    // key came from claims the least.
    public enum KeyCustody
    {
        NoDurableStore,
        GeneratedForThisInstance,
        SuppliedByConfiguration,
        SuppliedByExternalSecret,
    }
}
