using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DistributedOutbox.Postgres.EfCore
{
    /// <summary>
    /// Обработчик транзакции, получающий её из активного DbContext
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    internal sealed class DbContextTransactionManager<TDbContext> : IAsyncDisposable
        where TDbContext : DbContext
    {
        private readonly TDbContext _context;
        private readonly Func<DbConnection, Task>[] _actions;

        private bool _disposed;
        private bool _needCloseConnection;

        private DbConnection? _connection;

        public DbContextTransactionManager(TDbContext context, IEnumerable<Func<DbConnection, Task>> actions)
        {
            _context = context;
            _actions = actions.ToArray();
        }

        /// <summary>
        /// Выполняет инициализацию транзакции
        /// </summary>
        /// <exception cref="InvalidOperationException">Активная транзакция отсутствует</exception>
        public void Initialize()
        {
            EnsureNotDisposed();

            // ambient transaction: используем подключение от DbContext,
            // чтобы не напороться на распределенную транзакцию
            if (Transaction.Current is not null)
            {
                _connection = _context.Database.GetDbConnection();
            }
            // Явно начатая транзакция в DbContext:
            // используем именно её подключение 
            else if (_context.Database.CurrentTransaction is not null)
            {
                var dbTransaction = _context.Database.CurrentTransaction.GetDbTransaction();
                _connection = dbTransaction.Connection;
            }
            // Транзакции нет: атомарность обеспечить не можем, кидаем ошибку
            else
            {
                throw new InvalidOperationException(
                    "Outbox can not find active transaction. " +
                    "Ensure that transaction was started before call DbContext.SaveChanges().");
            }
        }

        /// <summary>
        /// Выполняет непосредственно запись событий в таблицу outbox
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <exception cref="InvalidOperationException">Инициализация не пройдена</exception>
        public async Task Perform(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            if (_connection == null)
            {
                throw new InvalidOperationException(
                    $"Connection is missing. Call {nameof(Initialize)}() before using {nameof(Perform)}().");
            }

            if (_connection.State is ConnectionState.Closed or ConnectionState.Broken)
            {
                await _connection.OpenAsync(cancellationToken);
                _needCloseConnection = true;
            }

            foreach (var action in _actions)
            {
                await action.Invoke(_connection);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            if (_needCloseConnection && _connection?.State is ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }

            _connection = null;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}