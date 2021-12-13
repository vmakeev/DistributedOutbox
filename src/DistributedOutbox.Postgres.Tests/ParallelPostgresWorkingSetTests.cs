﻿using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DistributedOutbox.Postgres.Tests.Attributes;
using DistributedOutbox.Postgres.Tests.Utils;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class ParallelPostgresWorkingSetTests
    {
        [Theory, AutoMoqData]
        internal void Ctor_ShouldBeSuccess(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            workingSet.Events.Should().BeEquivalentTo(outboxEvents, options => options.WithStrictOrdering());
            ((IWorkingSet)workingSet).Events.Should().BeEquivalentTo(outboxEvents, options => options.WithStrictOrdering());

            workingSet.DbConnection.Should().BeSameAs(transactionMock.Object.Connection);

            workingSet.Status.Should().Be(WorkingSetStatus.Active);
        }

        [Theory, AutoMoqData]
        internal async Task CommitAsync_ShouldCommitTransaction(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.CommitAsync(CancellationToken.None);
            transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task CommitAsync_ShouldFailWhenAlreadyCommitted(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.CommitAsync(CancellationToken.None);

            var action = new Func<Task>(() => workingSet.CommitAsync(CancellationToken.None));
            await action.Should().ThrowExactlyAsync<InvalidOperationException>();

            transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task CommitAsync_ShouldFailWhenAlreadyRolledBack(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.RollbackAsync(CancellationToken.None);

            var action = new Func<Task>(() => workingSet.CommitAsync(CancellationToken.None));
            await action.Should().ThrowExactlyAsync<InvalidOperationException>();

            transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task CommitAsync_ShouldFailWhenDisposed(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.DisposeAsync();
            var action = new Func<Task>(() => workingSet.CommitAsync(CancellationToken.None));
            await action.Should()
                        .ThrowExactlyAsync<ObjectDisposedException>()
                        .Where(exception => exception.ObjectName == nameof(ParallelPostgresWorkingSet));

            transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task RollbackAsync_ShouldRollbackTransaction(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.RollbackAsync(CancellationToken.None);
            transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task RollbackAsync_ShouldFailWhenAlreadyCommitted(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.CommitAsync(CancellationToken.None);

            var action = new Func<Task>(() => workingSet.RollbackAsync(CancellationToken.None));
            await action.Should().ThrowExactlyAsync<InvalidOperationException>();

            transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task RollbackAsync_ShouldFailWhenAlreadyRolledBack(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.RollbackAsync(CancellationToken.None);

            var action = new Func<Task>(() => workingSet.RollbackAsync(CancellationToken.None));
            await action.Should().ThrowExactlyAsync<InvalidOperationException>();

            transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task RollbackAsync_ShouldFailWhenDisposed(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbTransaction> transactionMock
        )
        {
            transactionMock
                .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.DisposeAsync();

            var action = new Func<Task>(() => workingSet.RollbackAsync(CancellationToken.None));
            await action.Should()
                        .ThrowExactlyAsync<ObjectDisposedException>()
                        .Where(exception => exception.ObjectName == nameof(ParallelPostgresWorkingSet));

            transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task DisposeAsync_ShouldDisposeTransactionWithConnection(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbConnection> connectionMock,
            Mock<ReceiveConnectionDbTransaction> transactionMock
        )
        {
            connectionMock
                .Setup(connection => connection.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.PublicDbConnection)
                .Returns(connectionMock.Object);

            transactionMock
                .Setup(transaction => transaction.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);
            await workingSet.DisposeAsync();

            transactionMock.Verify(transaction => transaction.DisposeAsync(), Times.Once);
            connectionMock.Verify(connection => connection.DisposeAsync(), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task DisposeAsync_ShouldDisposeTransactionWithConnectionOnce(
            IPostgresOutboxEvent[] outboxEvents,
            Mock<DbConnection> connectionMock,
            Mock<ReceiveConnectionDbTransaction> transactionMock
        )
        {
            connectionMock
                .Setup(connection => connection.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            transactionMock
                .Setup(transaction => transaction.PublicDbConnection)
                .Returns(connectionMock.Object);

            transactionMock
                .Setup(transaction => transaction.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();

            var workingSet = new ParallelPostgresWorkingSet(outboxEvents, transactionMock.Object);

            await workingSet.DisposeAsync();
            await workingSet.DisposeAsync();
            await workingSet.DisposeAsync();
            await workingSet.DisposeAsync();

            transactionMock.Verify(transaction => transaction.DisposeAsync(), Times.Once);
            connectionMock.Verify(connection => connection.DisposeAsync(), Times.Once);
        }
    }
}