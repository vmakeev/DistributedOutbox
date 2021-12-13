using DistributedOutbox.Postgres.Tests.Attributes;
using FluentAssertions;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class EventTypeToTargetsMapTests
    {
        [Theory, AutoMoqData]
        internal void Ctor_ShouldInitializeProperties(
            string eventType,
            string[] targets)
        {
            var map = new EventTypeToTargetsMap(eventType, targets);

            map.EventType.Should().Be(eventType);
            map.Targets.Should().BeEquivalentTo(targets, options => options.WithoutStrictOrdering());
        }
    }
}