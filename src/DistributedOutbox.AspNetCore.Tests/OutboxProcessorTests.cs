using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using DistributedOutbox.AspNetCore.Tests.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.AspNetCore.Tests
{
    public class OutboxProcessorTests
    {
        [Theory]
        [InlineAutoMoqData(13, 3)]
        [InlineAutoMoqData(1, 4)]
        [InlineAutoMoqData(100, 0)]
        [InlineAutoMoqData(0, 0)]
        [InlineAutoMoqData(0, 37)]
        [InlineAutoMoqData(42, 65)]
        internal async Task OutboxProcessor_ShouldProcessSingleSet(
            int sentEventsCount,
            int failedEventsCount,
            Mock<IWorkingSet> workingSetMock,
            [Frozen] Mock<IWorkingSetsProvider> workingSetsProviderMock,
            [Frozen] Mock<IParallelWorkingSetProcessor> parallelWorkingSetProcessorMock,
            [Frozen] Mock<ISequentialWorkingSetProcessor> sequentialWorkingSetProcessorMock,
            OutboxProcessor outboxProcessor)
        {
            // Arrange

            workingSetMock.SetupGet(workingSet => workingSet.Events)
                          .Returns(GetOutboxEvents(sentEventsCount, failedEventsCount, 0, 0));

            workingSetMock.Setup(workingSet => workingSet.DisposeAsync())
                          .Returns(new ValueTask())
                          .Verifiable();

            var workingSets = new[]
            {
                workingSetMock.Object
            };

            parallelWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(workingSetMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sentEventsCount)
                .Verifiable();

            sequentialWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(It.IsAny<IWorkingSet>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sentEventsCount)
                .Verifiable();

            workingSetsProviderMock.Setup(provider => provider.AcquireWorkingSetsAsync(It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(workingSets);

            workingSetsProviderMock.Setup(provider => provider.ReleaseWorkingSetAsync(workingSetMock.Object, true, It.IsAny<CancellationToken>()))
                                   .Returns(Task.CompletedTask)
                                   .Verifiable();
            // Act

            var actualSentEventsCount = await outboxProcessor.ProcessAsync(CancellationToken.None);

            // Assert

            actualSentEventsCount.Should().Be(sentEventsCount);
            parallelWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(workingSetMock.Object, It.IsAny<CancellationToken>()), Times.Once);
            sequentialWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(It.IsAny<IWorkingSet>(), It.IsAny<CancellationToken>()), Times.Never);

            workingSetsProviderMock.Verify(provider => provider.ReleaseWorkingSetAsync(workingSetMock.Object, true, It.IsAny<CancellationToken>()), Times.Once);

            workingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Once);
        }

        [Theory]
        [InlineAutoMoqData(3, 13, 3)]
        [InlineAutoMoqData(6, 100, 46)]
        [InlineAutoMoqData(2, 48, 37)]
        [InlineAutoMoqData(8, 42, 65)]
        [InlineAutoMoqData(5, 59, 14)]
        internal async Task OutboxProcessor_ShouldProcessManySets(
            int sentEventsCount1,
            int sentEventsCount2,
            int sentOrderedEventsCount,
            IFixture fixture,
            Mock<IWorkingSet> workingSetMock1,
            Mock<IWorkingSet> workingSetMock2,
            Mock<ISequentialWorkingSet> sequentialWorkingSetMock,
            [Frozen] Mock<IWorkingSetsProvider> workingSetsProviderMock,
            [Frozen] Mock<IParallelWorkingSetProcessor> parallelWorkingSetProcessorMock,
            [Frozen] Mock<ISequentialWorkingSetProcessor> sequentialWorkingSetProcessorMock,
            OutboxProcessor outboxProcessor)
        {
            // Arrange

            workingSetMock1.SetupGet(workingSet => workingSet.Events)
                           .Returns(GetOutboxEvents(sentEventsCount1, 0, 0, 0));

            workingSetMock2.SetupGet(workingSet => workingSet.Events)
                           .Returns(GetOutboxEvents(sentEventsCount2, 0, 0, 0));

            sequentialWorkingSetMock.SetupGet(workingSet => workingSet.Events)
                                    .Returns(GetOrderedOutboxEvents(fixture.Create<string>(), sentOrderedEventsCount, 0, 0, 0));

            workingSetMock1.Setup(workingSet => workingSet.DisposeAsync())
                           .Returns(new ValueTask())
                           .Verifiable();

            workingSetMock2.Setup(workingSet => workingSet.DisposeAsync())
                           .Returns(new ValueTask())
                           .Verifiable();

            sequentialWorkingSetMock.Setup(workingSet => workingSet.DisposeAsync())
                                    .Returns(new ValueTask())
                                    .Verifiable();

            var workingSets = new[]
            {
                workingSetMock1.Object,
                sequentialWorkingSetMock.Object,
                workingSetMock2.Object
            };

            var totalEventsCount = sentEventsCount1 + sentEventsCount2 + sentOrderedEventsCount;

            parallelWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(workingSetMock1.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sentEventsCount1)
                .Verifiable();

            parallelWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(sequentialWorkingSetMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0)
                .Verifiable();

            parallelWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(workingSetMock2.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sentEventsCount2)
                .Verifiable();

            sequentialWorkingSetProcessorMock
                .Setup(processor => processor.ProcessAsync(sequentialWorkingSetMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sentOrderedEventsCount)
                .Verifiable();

            workingSetsProviderMock.Setup(provider => provider.AcquireWorkingSetsAsync(It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(workingSets);

            workingSetsProviderMock.Setup(provider => provider.ReleaseWorkingSetAsync(workingSetMock1.Object, true, It.IsAny<CancellationToken>()))
                                   .Returns(Task.CompletedTask)
                                   .Verifiable();

            workingSetsProviderMock.Setup(provider => provider.ReleaseWorkingSetAsync(workingSetMock2.Object, true, It.IsAny<CancellationToken>()))
                                   .Returns(Task.CompletedTask)
                                   .Verifiable();

            workingSetsProviderMock.Setup(provider => provider.ReleaseWorkingSetAsync(sequentialWorkingSetMock.Object, true, It.IsAny<CancellationToken>()))
                                   .Returns(Task.CompletedTask)
                                   .Verifiable();

            // Act

            var actualSentEventsCount = await outboxProcessor.ProcessAsync(CancellationToken.None);

            // Assert

            actualSentEventsCount.Should().Be(totalEventsCount);

            parallelWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(workingSetMock1.Object, It.IsAny<CancellationToken>()), Times.Once);
            parallelWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(sequentialWorkingSetMock.Object, It.IsAny<CancellationToken>()), Times.Never);
            parallelWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(workingSetMock2.Object, It.IsAny<CancellationToken>()), Times.Once);

            sequentialWorkingSetProcessorMock
                .Verify(processor => processor.ProcessAsync(sequentialWorkingSetMock.Object, It.IsAny<CancellationToken>()), Times.Once);

            workingSetsProviderMock.Verify(provider => provider.ReleaseWorkingSetAsync(workingSetMock1.Object, true, It.IsAny<CancellationToken>()), Times.Once);
            workingSetsProviderMock.Verify(provider => provider.ReleaseWorkingSetAsync(workingSetMock2.Object, true, It.IsAny<CancellationToken>()), Times.Once);
            workingSetsProviderMock.Verify(provider => provider.ReleaseWorkingSetAsync(sequentialWorkingSetMock.Object, true, It.IsAny<CancellationToken>()), Times.Once);

            workingSetMock1.Verify(workingSet => workingSet.DisposeAsync(), Times.Once);
            workingSetMock2.Verify(workingSet => workingSet.DisposeAsync(), Times.Once);
            sequentialWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Once);
        }

        internal static IOutboxEvent[] GetOutboxEvents(int sentEventsCount, int failedEventsCount, int declinedEventsCount, int newEventsCount)
        {
            IEnumerable<IOutboxEvent> CreateEvents(int count, EventStatus status)
            {
                return Enumerable.Range(0, count).Select(
                    _ =>
                    {
                        var outboxEventMock = new Mock<IOutboxEvent>();
                        outboxEventMock.SetupGet(outboxEvent => outboxEvent.Status).Returns(status);
                        return outboxEventMock.Object;
                    });
            }

            var sentEvents = CreateEvents(sentEventsCount, EventStatus.Sent);
            var failedEvents = CreateEvents(failedEventsCount, EventStatus.Failed);
            var declinedEvents = CreateEvents(declinedEventsCount, EventStatus.Declined);
            var newEvents = CreateEvents(newEventsCount, EventStatus.New);

            var outboxEvents = sentEvents.Concat(failedEvents).Concat(declinedEvents).Concat(newEvents).ToArray();

            return outboxEvents;
        }

        internal static IOutboxEvent[] GetOrderedOutboxEvents(string sequenceName, int sentEventsCount, int failedEventsCount, int declinedEventsCount, int newEventsCount)
        {
            IEnumerable<IOutboxEvent> CreateEvents(int count, EventStatus status)
            {
                return Enumerable.Range(0, count).Select(
                    _ =>
                    {
                        var outboxEventMock = new Mock<IOrderedOutboxEvent>();
                        outboxEventMock.SetupGet(outboxEvent => outboxEvent.Status).Returns(status);
                        outboxEventMock.SetupGet(outboxEvent => outboxEvent.SequenceName).Returns(sequenceName);
                        return outboxEventMock.Object;
                    });
            }

            var sentEvents = CreateEvents(sentEventsCount, EventStatus.Sent);
            var failedEvents = CreateEvents(failedEventsCount, EventStatus.Failed);
            var declinedEvents = CreateEvents(declinedEventsCount, EventStatus.Declined);
            var newEvents = CreateEvents(newEventsCount, EventStatus.New);

            var outboxEvents = sentEvents.Concat(failedEvents).Concat(declinedEvents).Concat(newEvents).ToArray();

            return outboxEvents;
        }
    }
}