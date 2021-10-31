using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Рабочий набор, снабженный информацией о транзакции
    /// </summary>
    internal sealed class PostgresWorkingSet : IPostgresWorkingSet
    {
        private bool _isDisposed;
        private bool _isTransactionFinished;

        private readonly DbTransaction _transaction;
        private readonly IReadOnlyList<IPostgresOutboxEvent> _events;

        public DbConnection DbConnection => _transaction.Connection ?? throw new InvalidOperationException("Can not obtain connection from transaction");

        public PostgresWorkingSet(IReadOnlyList<IPostgresOutboxEvent> events,
                                  DbTransaction transaction)
        {
            _events = events;
            _transaction = transaction;
            Status = WorkingSetStatus.Active;
        }

        /// <inheritdoc />
        IReadOnlyList<IOutboxEvent> IWorkingSet.Events => _events;

        /// <inheritdoc cref="IPostgresWorkingSet.Status" />
        public WorkingSetStatus Status { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IPostgresOutboxEvent> Events => _events;

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            EnsureTransactionNotFinished();
            await _transaction.CommitAsync(cancellationToken);
            _isTransactionFinished = true;
        }

        /// <inheritdoc />
        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            EnsureTransactionNotFinished();
            await _transaction.RollbackAsync(cancellationToken);
            _isTransactionFinished = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                var connection = _transaction.Connection;
                await _transaction.DisposeAsync();
                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        private void EnsureTransactionNotFinished()
        {
            if (_isTransactionFinished)
            {
                throw new InvalidOperationException("The transaction has already been committed or rolled back.");
            }
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PostgresWorkingSet));
            }
        }
    }
}