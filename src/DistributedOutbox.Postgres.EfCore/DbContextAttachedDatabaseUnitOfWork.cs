using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DistributedOutbox.Postgres.EfCore
{
    /// <summary>
    /// Unit of work, работающий при сохранении данных в <typeparamref name="TDbContext"/>
    /// </summary>
    /// <typeparam name="TDbContext">Тип контекста данных</typeparam>
    internal sealed class DbContextAttachedDatabaseUnitOfWork<TDbContext> : IDatabaseUnitOfWork
        where TDbContext : DbContext
    {
        private readonly List<Func<DbConnection, Task>> _actions = new();
        private readonly SemaphoreSlim _activeTransactionAccessSemaphore = new(1, 1);
        private readonly TDbContext _context;

        private DbContextTransactionManager<TDbContext>? _activeTransaction;

        public DbContextAttachedDatabaseUnitOfWork(TDbContext context)
        {
            _context = context;

            _context.SavingChanges += DbContextOnSavingChanges;
            _context.SavedChanges += DbContextOnSavedChanges;
            _context.SaveChangesFailed += DbContextOnSaveChangesFailed;
        }

        /// <inheritdoc />
        public async Task Enqueue(Func<DbConnection, Task> action)
        {
            await _activeTransactionAccessSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                EnsureHasNoTransactionInProgress();
                _actions.Add(action);
            }
            finally
            {
                _activeTransactionAccessSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_activeTransaction is not null)
            {
                await _activeTransaction.DisposeAsync();
                _activeTransaction = null;
            }

            _context.SavedChanges -= DbContextOnSavedChanges;
            _context.SavingChanges -= DbContextOnSavingChanges;
            _context.SaveChangesFailed -= DbContextOnSaveChangesFailed;
        }

        private void DbContextOnSavedChanges(object? sender, SavedChangesEventArgs e)
        {
            OnSavedAsync()
                .GetAwaiter()
                .GetResult();
        }

        private async Task OnSavedAsync()
        {
            await _activeTransactionAccessSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                EnsureHasTransactionInProgress();
                _actions.Clear();
                await _activeTransaction!.DisposeAsync();
                _activeTransaction = null;
            }
            finally
            {
                _activeTransactionAccessSemaphore.Release();
            }
        }

        private void DbContextOnSavingChanges(object? sender, SavingChangesEventArgs e)
        {
            OnSavingAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        private async Task OnSavingAsync(CancellationToken cancellationToken)
        {
            await _activeTransactionAccessSemaphore.WaitAsync(cancellationToken);

            try
            {
                EnsureHasNoTransactionInProgress();

                _activeTransaction = new DbContextTransactionManager<TDbContext>(_context, _actions);
                try
                {
                    _activeTransaction.Initialize();
                    await _activeTransaction.Perform(cancellationToken);
                }
                catch
                {
                    await _activeTransaction.DisposeAsync();
                    _activeTransaction = null;
                    throw;
                }
            }
            finally
            {
                _activeTransactionAccessSemaphore.Release();
            }
        }

        private void DbContextOnSaveChangesFailed(object? sender, SaveChangesFailedEventArgs e)
        {
            OnSaveFailedAsync()
                .GetAwaiter()
                .GetResult();
        }

        private async Task OnSaveFailedAsync()
        {
            await _activeTransactionAccessSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                if (_activeTransaction is not null)
                {
                    await _activeTransaction.DisposeAsync();
                    _activeTransaction = null;
                }
            }
            finally
            {
                _activeTransactionAccessSemaphore.Release();
            }
        }

        private void EnsureHasNoTransactionInProgress()
        {
            if (_activeTransaction is not null)
            {
                throw new InvalidOperationException("Saving changes is in progress. Cannot save changes concurrently.");
            }
        }

        private void EnsureHasTransactionInProgress()
        {
            if (_activeTransaction is null)
            {
                throw new InvalidOperationException("No active saving changes process found.");
            }
        }
    }
}