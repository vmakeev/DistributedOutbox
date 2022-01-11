using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps72.Dev.Data.DbMocker;
using AutoFixture;
using AutoFixture.Xunit2;
using DistributedOutbox.Postgres.Tests.Attributes;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class PostgresOutboxTests
    {
        private const int ParametersPerRowInsert = 9;

        [Theory, AutoMoqData]
        internal async Task AddEventAsync_ShouldEnqueueEvent(
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            PostgresOutbox outbox,
            Mock<IOutboxEventData> outboxEventDataMock)
        {
            // Arrange
            
            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            // Act
            
            await outbox.AddEventAsync(outboxEventDataMock.Object, CancellationToken.None);

            // Assert
            
            actions.Should().HaveCount(1);
        }

        [Theory, AutoMoqData]
        internal async Task AddEventAsync_ShouldEnqueueEvents(
            IFixture fixture,
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            PostgresOutbox outbox)
        {
            // Arrange
            
            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            var outboxEvents = fixture.CreateMany<IOutboxEventData>().ToArray();

            // Act
            
            foreach (var outboxEventData in outboxEvents)
            {
                await outbox.AddEventAsync(outboxEventData, CancellationToken.None);
            }

            // Assert
            
            actions.Should().HaveCount(outboxEvents.Length);
        }

        [Theory, AutoMoqData]
        internal async Task AddEventsAsync_ShouldEnqueueEvent(
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            PostgresOutbox outbox,
            Mock<IOutboxEventData> outboxEventDataMock)
        {
            // Arrange
            
            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            // Act
            
            await outbox.AddEventsAsync(new[] { outboxEventDataMock.Object }, CancellationToken.None);

            // Assert
            
            actions.Should().HaveCount(1);
        }

        [Theory, AutoMoqData]
        internal async Task AddEventsAsync_ShouldEnqueueEvents(
            IFixture fixture,
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            PostgresOutbox outbox)
        {
            // Arrange
            
            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            var outboxEvents = fixture.CreateMany<IOutboxEventData>().ToArray();

            // Act
            
            await outbox.AddEventsAsync(outboxEvents, CancellationToken.None);

            // Assert
            
            actions.Should().HaveCount(1);
        }

        [Theory]
        [InlineAutoMoqData(0)]
        [InlineAutoMoqData(1)]
        [InlineAutoMoqData(10)]
        [InlineAutoMoqData(42)]
        [InlineAutoMoqData(451)]
        internal async Task AddEventAsync_ShouldProcess(
            int eventsCount,
            IFixture fixture,
            [Frozen] Mock<IEventTargetsProvider> eventTargetsProviderMock,
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            MockDbConnection dbConnectionMock,
            PostgresOutbox outbox)
        {
            // Arrange
            
            var lastId = 0L;
            dbConnectionMock.Mocks
                            .When(command => command.CommandText.Contains("nextval"))
                            .ReturnsScalar(_ => ++lastId);

            var insertCallsCount = 0;
            var passedParametersCount = 0;
            dbConnectionMock.Mocks
                            .When(command => command.CommandText.Contains("INSERT INTO"))
                            .ReturnsScalar(
                                command =>
                                {
                                    insertCallsCount++;
                                    passedParametersCount += command.Parameters.Count();
                                    return command.Parameters.Count() / ParametersPerRowInsert;
                                });

            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            eventTargetsProviderMock.Setup(provider => provider.GetTargets(It.IsAny<string>()))
                   .Returns(fixture.CreateMany<string>());

            // Act
            
            for (var i = 0; i < eventsCount; i++)
            {
                var outboxEventDataMock = fixture.Create<Mock<IOutboxEventData>>();
                outboxEventDataMock.Setup(eventData => eventData.Metadata).Returns(new PostgresOutboxEventMetadata { ["some"] = "thing" });
                outboxEventDataMock.Setup(eventData => eventData.Payload).Returns(new { foo = "bar" });
                await outbox.AddEventAsync(outboxEventDataMock.Object, CancellationToken.None);
            }

            foreach (var action in actions)
            {
                await action.Invoke(dbConnectionMock);
            }

            // Assert
            
            lastId.Should().Be(eventsCount);
            insertCallsCount.Should().Be(eventsCount);
            passedParametersCount.Should().Be(eventsCount * ParametersPerRowInsert);
        }

        [Theory]
        [InlineAutoMoqData(1, 12, 3)]
        [InlineAutoMoqData(0, 1, 1)]
        [InlineAutoMoqData(7, 2, 4)]
        [InlineAutoMoqData(1, 3, 5)]
        [InlineAutoMoqData(4, 5, 1)]
        internal async Task AddEventsAsync_ShouldProcess(
            int pack1Length,
            int pack2Length,
            int pack3Length,
            IFixture fixture,
            [Frozen] Mock<IEventTargetsProvider> eventTargetsProviderMock,
            [Frozen] Mock<IDatabaseUnitOfWork> unitOfWorkMock,
            MockDbConnection dbConnectionMock,
            PostgresOutbox outbox)
        {
            // Arrange
            
            var lastId = 0L;
            dbConnectionMock.Mocks
                            .When(command => command.CommandText.Contains("nextval"))
                            .ReturnsScalar(_ => ++lastId);

            var insertCallsCount = 0;
            var passedParametersCount = 0;
            dbConnectionMock.Mocks
                            .When(command => command.CommandText.Contains("INSERT INTO"))
                            .ReturnsScalar(
                                command =>
                                {
                                    insertCallsCount++;
                                    passedParametersCount += command.Parameters.Count();
                                    return command.Parameters.Count() / ParametersPerRowInsert;
                                });

            var actions = new List<Func<DbConnection, Task>>();

            unitOfWorkMock.Setup(uow => uow.Enqueue(It.IsAny<Func<DbConnection, Task>>()))
                          .Callback((Func<DbConnection, Task> func) => actions.Add(func))
                          .Returns(Task.CompletedTask);

            eventTargetsProviderMock.Setup(provider => provider.GetTargets(It.IsAny<string>()))
                   .Returns(fixture.CreateMany<string>());

            var eventPackLengths = new[] { pack1Length, pack2Length, pack3Length };

            // Act
            
            foreach (var packLength in eventPackLengths)
            {
                var outboxEvents = new List<IOutboxEventData>();

                for (var i = 0; i < packLength; i++)
                {
                    var outboxEventDataMock = fixture.Create<Mock<IOutboxEventData>>();
                    outboxEventDataMock.Setup(eventData => eventData.Metadata).Returns(new PostgresOutboxEventMetadata { ["some"] = "thing" });
                    outboxEventDataMock.Setup(eventData => eventData.Payload).Returns(new { foo = "bar" });
                    outboxEvents.Add(outboxEventDataMock.Object);
                }

                await outbox.AddEventsAsync(outboxEvents, CancellationToken.None);
            }

            foreach (var action in actions)
            {
                await action.Invoke(dbConnectionMock);
            }

            // Assert
            
            lastId.Should().Be(eventPackLengths.Sum());
            insertCallsCount.Should().Be(eventPackLengths.Count(length => length > 0));
            passedParametersCount.Should().Be(eventPackLengths.Sum() * ParametersPerRowInsert);
        }
    }
}