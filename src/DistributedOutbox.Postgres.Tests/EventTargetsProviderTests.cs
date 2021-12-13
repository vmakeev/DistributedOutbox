using System.Linq;
using AutoFixture;
using DistributedOutbox.Postgres.Tests.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class EventTargetsProviderTests
    {
        [Theory, AutoMoqData]
        internal void GetTargets_SingleMap_MatchingType_ShouldReturnTargets(
            string eventType,
            string[] eventTargets,
            Mock<IEventTypeToTargetsMap> mapMock)
        {
            mapMock.Setup(map => map.EventType).Returns(eventType);
            mapMock.Setup(map => map.Targets).Returns(eventTargets);

            var provider = new EventTargetsProvider(new[] { mapMock.Object });

            provider.GetTargets(eventType).Should().BeEquivalentTo(eventTargets, options => options.WithoutStrictOrdering());
        }

        [Theory, AutoMoqData]
        internal void GetTargets_SingleMap_NotMatchingType_ShouldReturnEmptyTargets(
            string existingType,
            string nonExistingType,
            string[] eventTargets,
            Mock<IEventTypeToTargetsMap> mapMock)
        {
            mapMock.Setup(map => map.EventType).Returns(existingType);
            mapMock.Setup(map => map.Targets).Returns(eventTargets);

            var provider = new EventTargetsProvider(new[] { mapMock.Object });

            provider.GetTargets(nonExistingType).Should().BeEmpty();
        }

        [Theory, AutoMoqData]
        internal void GetTargets_ManyMaps_MatchingType_ShouldReturnTargets(
            string eventType,
            string[] eventTargets1,
            string[] eventTargets2,
            Mock<IEventTypeToTargetsMap> mapMock1,
            Mock<IEventTypeToTargetsMap> mapMock2)
        {
            mapMock1.Setup(map => map.EventType).Returns(eventType);
            mapMock1.Setup(map => map.Targets).Returns(eventTargets1);

            mapMock2.Setup(map => map.EventType).Returns(eventType);
            mapMock2.Setup(map => map.Targets).Returns(eventTargets2);

            var provider = new EventTargetsProvider(new[] { mapMock1.Object, mapMock2.Object });

            provider.GetTargets(eventType).Should().BeEquivalentTo(eventTargets1.Concat(eventTargets2), options => options.WithoutStrictOrdering());
        }

        [Theory, AutoMoqData]
        internal void GetTargets_ManyMaps_ManyTypes_MatchingType_ShouldReturnTargets(
            string eventType1,
            string eventType2,
            string[] eventTargets1,
            string[] eventTargets2,
            Mock<IEventTypeToTargetsMap> mapMock1,
            Mock<IEventTypeToTargetsMap> mapMock2)
        {
            mapMock1.Setup(map => map.EventType).Returns(eventType1);
            mapMock1.Setup(map => map.Targets).Returns(eventTargets1);

            mapMock2.Setup(map => map.EventType).Returns(eventType2);
            mapMock2.Setup(map => map.Targets).Returns(eventTargets2);

            var provider = new EventTargetsProvider(new[] { mapMock1.Object, mapMock2.Object });

            provider.GetTargets(eventType1).Should().BeEquivalentTo(eventTargets1, options => options.WithoutStrictOrdering());
            provider.GetTargets(eventType2).Should().BeEquivalentTo(eventTargets2, options => options.WithoutStrictOrdering());
        }

        [Theory, AutoMoqData]
        internal void GetTargets_ManyMaps_ManyTypes_NotMatchingType_ShouldReturnEmpty(
            string eventType1,
            string eventType2,
            string notExistingType,
            string[] eventTargets1,
            string[] eventTargets2,
            Mock<IEventTypeToTargetsMap> mapMock1,
            Mock<IEventTypeToTargetsMap> mapMock2)
        {
            mapMock1.Setup(map => map.EventType).Returns(eventType1);
            mapMock1.Setup(map => map.Targets).Returns(eventTargets1);

            mapMock2.Setup(map => map.EventType).Returns(eventType2);
            mapMock2.Setup(map => map.Targets).Returns(eventTargets2);

            var provider = new EventTargetsProvider(new[] { mapMock1.Object, mapMock2.Object });

            provider.GetTargets(notExistingType).Should().BeEmpty();
        }

        [Theory, AutoMoqData]
        internal void GetTargets_ManyMaps_SameEvents_MatchingType_ShouldReturnTargets(
            IFixture fixture,
            string eventType,
            Mock<IEventTypeToTargetsMap> mapMock1,
            Mock<IEventTypeToTargetsMap> mapMock2)
        {
            var eventTargets = fixture.CreateMany<string>(5).ToArray();

            mapMock1.Setup(map => map.EventType).Returns(eventType);
            mapMock1.Setup(map => map.Targets).Returns(eventTargets);

            mapMock2.Setup(map => map.EventType).Returns(eventType);
            mapMock2.Setup(map => map.Targets).Returns(eventTargets.Skip(2));

            var provider = new EventTargetsProvider(new[] { mapMock1.Object, mapMock2.Object });

            provider.GetTargets(eventType).Should().BeEquivalentTo(eventTargets, options => options.WithoutStrictOrdering());
        }
    }
}