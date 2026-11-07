namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Puts a test class in the integration tier through the category it inherits. It holds nothing and has no
    /// TestFixture attribute, so deriving from it changes nothing else about how the class is built or named.
    /// </summary>
    [Category("Integration")]
    public abstract class IntegrationFixture;
}
