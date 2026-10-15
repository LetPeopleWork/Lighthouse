using ArchUnitNET.NUnit;
using Lighthouse.Backend.API;
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
            "Telling every Team and Portfolio on a connection to update is something only an administrator's " +
            "save of that connection may do. Opening the connection's settings page quietly registers the " +
            "connector's own fields, and the Jira refresh does the same on every run; if either of those " +
            "announced a changed fetch shape, every open settings page and every scheduled refresh would set " +
            "off a full re-download of every Team and Portfolio on the connection. The event itself, its " +
            "handler and the composition root that registers the handler are the only other types that may " +
            "name it. Types are matched by name prefix so the compiler-generated helpers nested inside " +
            "them (async state machines, lambda closures) count as the type they belong to.";

        [Test]
        public void OnlyTheConnectionSave_PublishesThatAConnectionsFetchShapeChanged()
        {
            Types().That()
                .DoNotHaveFullNameContaining(typeof(WorkTrackingSystemConnectionController).FullName!).And()
                .DoNotHaveFullNameContaining(typeof(ConnectionFetchShapeChanged).FullName!).And()
                .DoNotHaveFullNameContaining(typeof(ConnectionFetchShapeChangedUpdateTriggerHandler).FullName!).And()
                .DoNotHaveFullNameStartingWith(typeof(Program).FullName! + "+").And()
                .AreNot(typeof(Program))
                .Should().NotDependOnAny(Types().That().Are(typeof(ConnectionFetchShapeChanged)))
                .Because(Because)
                .Check(Architecture);
        }

        [Test]
        public void TheConnectionSave_IsSeenToPublishThatAConnectionsFetchShapeChanged()
        {
            Types().That()
                .HaveFullNameContaining(typeof(WorkTrackingSystemConnectionController).FullName!)
                .Should().DependOnAny(Types().That().Are(typeof(ConnectionFetchShapeChanged)))
                .Because("the rule above only means something while it can see the one save that does publish the event")
                .Check(Architecture);
        }
    }
}
