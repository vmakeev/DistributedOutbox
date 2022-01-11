using System;
using System.Text.Json;
using DistributedOutbox.Postgres.Tests.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class OutboxEventMapperTests
    {
        [Theory]
        [InlineAutoMoqData(new object?[] { null })]
        [InlineAutoMoqData("")]
        public void ToPostgresOutboxEvent_ShouldMapCorrect_NotOrdered(
            string? sequenceName,
            Mock<PostgresOutboxEventRaw> rawEventMock,
            string[] eventTargets,
            EventStatus status)
        {
            // Arrange

            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(sequenceName);

            var metadata = new { foo = "bar", answer = "42" };
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(JsonSerializer.Serialize(metadata));
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(JsonSerializer.Serialize(eventTargets));
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = rawEventMock.Object.ToPostgresOutboxEvent();

            // Assert
            
            outboxEvent.Should().NotBeAssignableTo<IOrderedOutboxEvent>();

            outboxEvent.Id.Should().Be(default);
            outboxEvent.EventKey.Should().Be(rawEventMock.Object.Key);
            outboxEvent.EventType.Should().Be(rawEventMock.Object.Type);
            outboxEvent.EventDate.Should().Be(rawEventMock.Object.Date);
            outboxEvent.EventTargets.Should()
                       .NotBeNull()
                       .And
                       .BeEquivalentTo(eventTargets, options => options.WithStrictOrdering());

            outboxEvent.Metadata.Should().NotBeNull().And.HaveCount(2);
            outboxEvent.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
            outboxEvent.Metadata.Should().ContainKey("answer").WhoseValue.Should().Be("42");

            outboxEvent.Status.Should().Be(status);
            outboxEvent.Payload.Should().Be(rawEventMock.Object.Payload);
        }

        [Theory, AutoMoqData]
        public void ToPostgresOutboxEvent_ShouldMapCorrect_Ordered(
            Mock<PostgresOutboxEventRaw> rawEventMock,
            string sequenceName,
            string[] eventTargets,
            EventStatus status)
        {
            // Arrange

            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(sequenceName);

            var metadata = new { foo = "bar", answer = "42" };
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(JsonSerializer.Serialize(metadata));
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(JsonSerializer.Serialize(eventTargets));
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = rawEventMock.Object.ToPostgresOutboxEvent();

            // Assert
            
            outboxEvent.Should().BeAssignableTo<IOrderedOutboxEvent>();

            ((IOrderedOutboxEvent)outboxEvent).SequenceName.Should().Be(sequenceName);
            outboxEvent.Id.Should().Be(default);
            outboxEvent.EventKey.Should().Be(rawEventMock.Object.Key);
            outboxEvent.EventType.Should().Be(rawEventMock.Object.Type);
            outboxEvent.EventDate.Should().Be(rawEventMock.Object.Date);
            outboxEvent.EventTargets.Should()
                       .NotBeNull()
                       .And
                       .BeEquivalentTo(eventTargets, options => options.WithStrictOrdering());

            outboxEvent.Metadata.Should().NotBeNull().And.HaveCount(2);
            outboxEvent.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
            outboxEvent.Metadata.Should().ContainKey("answer").WhoseValue.Should().Be("42");

            outboxEvent.Status.Should().Be(status);
            outboxEvent.Payload.Should().Be(rawEventMock.Object.Payload);
        }

        [Theory, AutoMoqData]
        public void ToOrderedPostgresOutboxEvent_ShouldThrows(
            Mock<PostgresOutboxEventRaw> rawEventMock,
            string[] eventTargets,
            EventStatus status)
        {
            // Arrange

            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns((string?)null);

            var metadata = new { foo = "bar", answer = "42" };
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(JsonSerializer.Serialize(metadata));
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(JsonSerializer.Serialize(eventTargets));
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            var action = new Action(() => rawEventMock.Object.ToOrderedPostgresOutboxEvent());
            
            // Act & assert
            
            action.Should().ThrowExactly<ArgumentException>().WithParameterName("SequenceName");
        }

        [Theory, AutoMoqData]
        public void ToOrderedPostgresOutboxEvent_ShouldMapCorrect(
            Mock<PostgresOutboxEventRaw> rawEventMock,
            string sequenceName,
            string[] eventTargets,
            EventStatus status)
        {
            // Arrange

            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(sequenceName);

            var metadata = new { foo = "bar", answer = "42" };
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(JsonSerializer.Serialize(metadata));
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(JsonSerializer.Serialize(eventTargets));
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = rawEventMock.Object.ToOrderedPostgresOutboxEvent();

            // Assert
            
            outboxEvent.Should().BeAssignableTo<IOrderedOutboxEvent>();

            outboxEvent.SequenceName.Should().Be(sequenceName);
            outboxEvent.Id.Should().Be(default);
            outboxEvent.EventKey.Should().Be(rawEventMock.Object.Key);
            outboxEvent.EventType.Should().Be(rawEventMock.Object.Type);
            outboxEvent.EventDate.Should().Be(rawEventMock.Object.Date);
            outboxEvent.EventTargets.Should()
                       .NotBeNull()
                       .And
                       .BeEquivalentTo(eventTargets, options => options.WithStrictOrdering());

            outboxEvent.Metadata.Should().NotBeNull().And.HaveCount(2);
            outboxEvent.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
            outboxEvent.Metadata.Should().ContainKey("answer").WhoseValue.Should().Be("42");

            outboxEvent.Status.Should().Be(status);
            outboxEvent.Payload.Should().Be(rawEventMock.Object.Payload);
        }
    }
}