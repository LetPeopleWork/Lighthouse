using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;

namespace Lighthouse.Backend.Tests.API
{
    public class DemoControllerAuthorizationTests : IntegrationTestBase
    {
        public DemoControllerAuthorizationTests()
            : base(new TestWebApplicationFactory<Program>())
        {
        }

        // Note: LoadAll endpoint has been removed as demo scenarios should only be loaded individually
    }
}
