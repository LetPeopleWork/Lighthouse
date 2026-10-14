namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>
    /// How many reports of one type an owner may hold. Pure: the licence and the count are handed in. A licence
    /// that lapses takes nothing away; it only stops creating while the owner already holds its one of that type.
    /// </summary>
    public static class ReportCapPolicy
    {
        public const int CommunityCap = 1;

        public static bool MayCreate(bool premium, int countForOwnerOfThatType)
            => premium || countForOwnerOfThatType < CommunityCap;
    }
}
