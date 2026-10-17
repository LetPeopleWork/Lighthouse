using System.Text.RegularExpressions;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation.Connections;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ConnectionFetchShapeChangedSinglePublisherArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string Because =
            "Telling every Team and Portfolio on a connection to update sets off a full re-download of each " +
            "of them, so no sync path or other service may start it. Only the connection save, the " +
            "event itself, its handler and the composition root that registers the handler may name the " +
            "event. The rule cannot tell which of the connection controller's requests calls the save, so it does not hold the settings " +
            "page's read of a connection, which quietly registers the connector's own fields; the acceptance " +
            "scenarios on reading a connection hold that one. A publisher written inside an async method " +
            "anywhere else is only caught in a Debug run: a Release build moves an async method's body into " +
            "a helper type the architecture loader does not attribute to its owner.";

        [Test]
        // @contract-shape:unbounded-preservation
        public void NoTypeOutsideTheConnectionSave_PublishesThatAConnectionsFetchShapeChanged()
        {
            Types().That()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ConnectionSaveService))).And()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ConnectionFetchShapeChanged))).And()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ConnectionFetchShapeChangedUpdateTriggerHandler))).And()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(Program)))
                .Should().NotDependOnAny(Types().That().Are(typeof(ConnectionFetchShapeChanged)))
                .Because(Because)
                .Check(Architecture);
        }

        [Test]
        // @contract-shape:unbounded-preservation
        public void TheConnectionSave_IsSeenToPublishThatAConnectionsFetchShapeChanged()
        {
            Types().That()
                .HaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ConnectionSaveService)))
                .Should().DependOnAny(Types().That().Are(typeof(ConnectionFetchShapeChanged)))
                .Because("the rule above only means something while it can see the one save that does publish the event")
                .Check(Architecture);
        }

        // Matches exactly the given type, or a type nested inside it such as the compiler-generated helpers
        // behind its lambdas and async methods, and nothing whose name merely begins the same way.
        private static string TheTypeOrOneNestedInIt(Type type)
            => "^" + Regex.Escape(type.FullName!) + @"($|\+)";
    }
}
