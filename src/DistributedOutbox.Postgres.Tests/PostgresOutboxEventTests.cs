using System;
using System.Text.Json;
using AutoFixture;
using DistributedOutbox.Postgres.Tests.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class PostgresOutboxEventTests
    {
        [Theory, AutoMoqData]
        public void Ctor_ShouldMapCorrect(
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

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            // Assert
            
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
        public void Ctor_ShouldMapEmptyTargetsCorrect(
            Mock<PostgresOutboxEventRaw> rawEventMock,
            EventStatus status)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns((string?)null);

            var metadata = new { foo = "bar", answer = "42" };
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(JsonSerializer.Serialize(metadata));
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            // Assert
            
            outboxEvent.Id.Should().Be(default);
            outboxEvent.EventKey.Should().Be(rawEventMock.Object.Key);
            outboxEvent.EventType.Should().Be(rawEventMock.Object.Type);
            outboxEvent.EventDate.Should().Be(rawEventMock.Object.Date);
            outboxEvent.EventTargets.Should().NotBeNull().And.BeEmpty();

            outboxEvent.Metadata.Should().NotBeNull().And.HaveCount(2);
            outboxEvent.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
            outboxEvent.Metadata.Should().ContainKey("answer").WhoseValue.Should().Be("42");

            outboxEvent.Status.Should().Be(status);
            outboxEvent.Payload.Should().Be(rawEventMock.Object.Payload);
        }

        [Theory, AutoMoqData]
        public void Ctor_ShouldMapEmptyMetadataCorrect(
            Mock<PostgresOutboxEventRaw> rawEventMock,
            string[] eventTargets,
            EventStatus status)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns((string?)null);

            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(JsonSerializer.Serialize(eventTargets));
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            // Assert
            
            outboxEvent.Id.Should().Be(default);
            outboxEvent.EventKey.Should().Be(rawEventMock.Object.Key);
            outboxEvent.EventType.Should().Be(rawEventMock.Object.Type);
            outboxEvent.EventDate.Should().Be(rawEventMock.Object.Date);
            outboxEvent.EventTargets.Should()
                       .NotBeNull()
                       .And
                       .BeEquivalentTo(eventTargets, options => options.WithStrictOrdering());

            outboxEvent.Metadata.Should().NotBeNull().And.BeEmpty();

            outboxEvent.Status.Should().Be(status);
            outboxEvent.Payload.Should().Be(rawEventMock.Object.Payload);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.New)]
        [InlineAutoMoqData(EventStatus.Failed)]
        public void MarkCompleted_ShouldBeSuccess_WithCurrentDate(EventStatus status,
                                                                  IFixture fixture,
                                                                  Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            var testStartedAt = DateTime.UtcNow;

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);
            outboxEvent.MarkCompleted();

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Sent);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.SentTime).WhoseValue.Should().NotBeNull().And.NotBeEmpty();

            var sentTime = DateTime.Parse(outboxEvent.Metadata[MetadataKeys.SentTime]!).ToUniversalTime();
            sentTime.Should().BeOnOrAfter(testStartedAt).And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.Sent)]
        public void MarkCompleted_ShouldBeSuccess_WithKeepDate(EventStatus status,
                                                               DateTime existingSentTime,
                                                               IFixture fixture,
                                                               Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object)
            {
                Metadata =
                {
                    [MetadataKeys.SentTime] = existingSentTime.ToString("O")
                }
            };
            outboxEvent.MarkCompleted();

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Sent);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.SentTime).WhoseValue.Should().NotBeNull().And.NotBeEmpty();

            var sentTime = DateTime.Parse(outboxEvent.Metadata[MetadataKeys.SentTime]!);
            sentTime.Should().Be(existingSentTime);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.Declined)]
        [InlineAutoMoqData((EventStatus)42)]
        public void MarkCompleted_ShouldBeFailed(EventStatus status,
                                                 IFixture fixture,
                                                 Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            var action = new Action(() => outboxEvent.MarkCompleted());

            // Assert
            
            action.Should().ThrowExactly<InvalidOperationException>();
            outboxEvent.Status.Should().Be(status);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.New)]
        [InlineAutoMoqData(EventStatus.Failed)]
        public void MarkFailed_ShouldBeSuccess_WithReason(EventStatus status,
                                                          string reason,
                                                          IFixture fixture,
                                                          Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);
            outboxEvent.MarkFailed(reason);

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Failed);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.LastFailureReason).WhoseValue.Should().Be(reason);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.New)]
        [InlineAutoMoqData(EventStatus.Failed)]
        public void MarkFailed_ShouldBeSuccess_WithOverrideReason(EventStatus status,
                                                                  string existingReason,
                                                                  string actualReason,
                                                                  IFixture fixture,
                                                                  Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object)
            {
                Metadata =
                {
                    [MetadataKeys.LastFailureReason] = existingReason
                }
            };
            outboxEvent.MarkFailed(actualReason);

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Failed);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.LastFailureReason).WhoseValue.Should().Be(actualReason);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.Sent)]
        [InlineAutoMoqData(EventStatus.Declined)]
        [InlineAutoMoqData((EventStatus)42)]
        public void MarkFailed_ShouldFail(EventStatus status,
                                          string reason,
                                          IFixture fixture,
                                          Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            var action = new Action(() => outboxEvent.MarkFailed(reason));

            // Assert
            
            action.Should().ThrowExactly<InvalidOperationException>();
            outboxEvent.Status.Should().Be(status);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.New)]
        [InlineAutoMoqData(EventStatus.Failed)]
        [InlineAutoMoqData(EventStatus.Declined)]
        public void MarkDeclined_ShouldBeSuccess_WithReason(EventStatus status,
                                                            string reason,
                                                            IFixture fixture,
                                                            Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);
            outboxEvent.MarkDeclined(reason);

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Declined);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.LastFailureReason).WhoseValue.Should().Be(reason);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.New)]
        [InlineAutoMoqData(EventStatus.Failed)]
        [InlineAutoMoqData(EventStatus.Declined)]
        public void MarkDeclined_ShouldBeSuccess_WithOverrideReason(EventStatus status,
                                                                    string existingReason,
                                                                    string actualReason,
                                                                    IFixture fixture,
                                                                    Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object)
            {
                Metadata =
                {
                    [MetadataKeys.LastFailureReason] = existingReason
                }
            };
            outboxEvent.MarkDeclined(actualReason);

            // Assert
            
            outboxEvent.Status.Should().Be(EventStatus.Declined);
            outboxEvent.Metadata.Should().HaveCount(1)
                       .And
                       .ContainKey(MetadataKeys.LastFailureReason).WhoseValue.Should().Be(actualReason);
        }

        [Theory]
        [InlineAutoMoqData(EventStatus.Sent)]
        [InlineAutoMoqData((EventStatus)42)]
        public void MarkDeclined_ShouldFail(EventStatus status,
                                            string reason,
                                            IFixture fixture,
                                            Mock<PostgresOutboxEventRaw> rawEventMock)
        {
            // Arrange
            
            rawEventMock.Setup(rawEvent => rawEvent.SequenceName).Returns(fixture.Create<string>());
            rawEventMock.Setup(rawEvent => rawEvent.Metadata).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Targets).Returns(string.Empty);
            rawEventMock.Setup(rawEvent => rawEvent.Status).Returns(status.ToString("G"));

            // Act
            
            var outboxEvent = new PostgresOutboxEvent(rawEventMock.Object);

            var action = new Action(() => outboxEvent.MarkDeclined(reason));

            // Assert
            
            action.Should().ThrowExactly<InvalidOperationException>();
            outboxEvent.Status.Should().Be(status);
        }
    }
}