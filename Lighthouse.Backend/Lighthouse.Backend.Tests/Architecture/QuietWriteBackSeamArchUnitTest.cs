using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class QuietWriteBackSeamArchUnitTest
    {

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private static readonly string[] TheOnlyAsynchronousMember = ["FlushAsync"];

        [Test]
        public void WriteBackTriggerService_DoesNotDependOnTheWriteBackService()
        {
            Classes().That().HaveFullName("Lighthouse.Backend.Services.Implementation.WriteBackTriggerService")
                .Should().NotDependOnAny(Types().That().HaveFullName("Lighthouse.Backend.Services.Interfaces.IWriteBackService"))
                .Because(
                    "ADR-144 D1: the trigger service is a resolver. It returns a plan and performs no I/O, so " +
                    "'did this write?' is answerable from the signature rather than by reading the body.")
                .Check(Architecture);
        }

        /// <summary>
        /// Not a RED scaffold - a standing guard. ADR-144 makes <c>FlushAsync</c> the collector's only
        /// impure member; a second asynchronous member would mean staging had grown a side effect.
        /// </summary>
        [Test]
        public void WriteBackCollector_HasFlushAsyncAsItsOnlyAsynchronousMember()
        {
            var asynchronousMembers = typeof(WriteBackCollector)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
                .Select(method => method.Name)
                .ToList();

            Assert.That(asynchronousMembers, Is.EquivalentTo(TheOnlyAsynchronousMember));
        }
    }
}
