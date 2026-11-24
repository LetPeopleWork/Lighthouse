namespace Lighthouse.Backend.Tests.PlacementFixtures.Platform
{
    /// <summary>
    /// A made-up platform type for the platform placement rule. It names no real type, so the rule over the real tree never
    /// judges it; the rule is handed this type as a made-up subject to show that it finds a test by the file it was
    /// compiled from.
    /// </summary>
    internal static class AMadeUpLanternKeeper
    {
        public static int Answer() => 42;
    }

    /// <summary>
    /// A made-up unit test of the made-up platform type, compiled from this file, which sits outside the Platform test
    /// folders. The placement rule must name this file with the Platform folder of the made-up type.
    /// </summary>
    internal static class AMadeUpLanternKeeperTest
    {
        public static int Answer() => AMadeUpLanternKeeper.Answer();
    }
}
