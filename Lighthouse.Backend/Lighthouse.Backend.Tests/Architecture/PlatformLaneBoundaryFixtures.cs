// Made-up types for the platform lane's own rules: a made-up lane under Platform, with one namespace per case so a rule
// handed one case sees only it, and the made-up API, context adapters, context Application and Services it may or may
// not reach. A type outside Services and outside the lane carries the same short names, so a rule that matched names
// without their namespace would count it.

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Compliant
{
    internal static class ACompliantLaneService
    {
        public static int Answer()
            => Services.AListedServicesHelper.Value()
                + Outside.AListedServicesHelper.Value()
                + Modules.Harbour.Application.AHarbourAnswer.Value();

        internal static class AHeldWork
        {
            public static int Answer() => Services.AListedServicesHelper.Value();
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesTheApi
{
    internal static class ALaneServiceReachingTheApi
    {
        public static int Answer() => API.AnEndpoint.Value();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesAnAdapter
{
    internal static class ALaneServiceReachingAnAdapter
    {
        public static int Answer() => Modules.Harbour.Adapters.AHarbourStore.Value();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesUnlistedServices
{
    internal static class ALaneServiceWithANewServicesEdge
    {
        public static int Answer() => Services.AListedServicesHelper.Value();

        internal static class ANewcomer
        {
            public static int Answer() => Services.AnUnlistedServicesHelper.Value();
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Lanterns
{
    internal static class ALanternTrimmer
    {
        public static int Value() => 3;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Beacons
{
    internal static class ABeaconLighter
    {
        public static int Value() => 5;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Services.Lanterns
{
    internal static class ALanternKeeper
    {
        public static int Value() => 7;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Services
{
    internal static class AListedServicesHelper
    {
        public static int Value() => 11;
    }

    internal static class AnUnlistedServicesHelper
    {
        public static int Value() => 13;
    }

    internal static class AGoneServicesHelper
    {
        public static int Value() => 17;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Outside
{
    internal static class AListedServicesHelper
    {
        public static int Value() => 19;
    }

    internal static class ALanternTrimmer
    {
        public static int Value() => 23;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.API
{
    internal static class AnEndpoint
    {
        public static int Value() => 29;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Modules.Harbour.Adapters
{
    internal static class AHarbourStore
    {
        public static int Value() => 31;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Modules.Harbour.Application
{
    internal static class AHarbourAnswer
    {
        public static int Value() => 37;
    }
}
