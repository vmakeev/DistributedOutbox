using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DistributedOutbox.Postgres.Queries;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Поставщик информации о рабочих наборах, работающий с Postgres
    /// </summary>
    internal class PostgresWorkingSetsProvider : IWorkingSetsProvider
    {
        private readonly IPostgresOutboxConnectionProvider _connectionProvider;
        private readonly IOptions<PostgresWorkingSetOptions> _options;

        public PostgresWorkingSetsProvider(IPostgresOutboxConnectionProvider connectionProvider, IOptions<PostgresWorkingSetOptions> options)
        {
            _connectionProvider = connectionProvider;
            _options = options;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IWorkingSet>> AcquireWorkingSetsAsync(CancellationToken cancellationToken)
        {
            var schema = _options.Value.Schema;
            var table = _options.Value.Table;
            var parallelLimit = _options.Value.ParallelLimit;
            var sequentialLimit = _options.Value.SequentialLimit;

            var workingSets = new List<IWorkingSet>();

            try
            {
                var parallelWorkingSet = await GetParallelWorkingSetAsync(
                    schema,
                    table,
                    parallelLimit,
                    cancellationToken);

                if (parallelWorkingSet is not null)
                {
                    workingSets.Add(parallelWorkingSet);
                }

                await using var connection = await _connectionProvider.GetDbConnectionAsync(cancellationToken);

                var sequenceNames = new SelectSequenceNamesQuery(schema, table)
                    .GetAsync(connection, sequentialLimit, cancellationToken);

                var loadedSequentialEventsCount = 0;
                await foreach (var sequenceName in sequenceNames.WithCancellation(cancellationToken))
                {
                    if (sequenceName is null)
                    {
                        continue;
                    }

                    var actualLimit = sequentialLimit - loadedSequentialEventsCount;
                    if (actualLimit <= 0)
                    {
                        break;
                    }

                    var sequentialSet = await GetSequentialWorkingSetAsync(
                        schema,
                        table,
                        actualLimit,
                        sequenceName,
                        cancellationToken);

                    if (sequentialSet is not null)
                    {
                        workingSets.Add(sequentialSet);
                        loadedSequentialEventsCount += sequentialSet.Events.Count;
                    }
                }
            }
            catch
            {
                foreach (var workingSet in workingSets)
                {
                    await workingSet.DisposeAsync();
                }

                throw;
            }

            return workingSets;
        }

        /// <inheritdoc />
        public async Task ReleaseWorkingSetAsync(IWorkingSet workingSet, bool isProcessed, CancellationToken cancellationToken)
        {
            if (isProcessed)
            {
                await CommitWorkingSetAsync(workingSet, cancellationToken);
            }
            else
            {
                await RollbackWorkingSetAsync(workingSet, cancellationToken);
            }
        }

        private async Task<IPostgresWorkingSet?> GetParallelWorkingSetAsync(
            string schema,
            string table,
            int limit,
            CancellationToken cancellationToken)
        {
            DbConnection? connection = null;
            DbTransaction? transaction = null;

            try
            {
                connection = await _connectionProvider.GetDbConnectionAsync(cancellationToken);
                transaction = await connection.BeginTransactionAsync(cancellationToken);

                var parallelEvents = await new SelectElderNonSequentialEventsQuery(schema, table)
                                           .GetAsync(connection, limit, cancellationToken)
                                           .Select(postgresOutboxEvent => postgresOutboxEvent.ToPostgresOutboxEvent())
                                           .ToListAsync(cancellationToken: cancellationToken);

                var parallelWorkingSet = new ParallelPostgresWorkingSet(parallelEvents, transaction);
                return parallelWorkingSet;
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }

                throw;
            }
        }

        private async Task<IPostgresWorkingSet?> GetSequentialWorkingSetAsync(
            string schema,
            string table,
            int limit,
            string sequenceName,
            CancellationToken cancellationToken)
        {
            DbConnection? connection = null;
            DbTransaction? transaction = null;

            try
            {
                connection = await _connectionProvider.GetDbConnectionAsync(cancellationToken);
                transaction = await connection.BeginTransactionAsync(cancellationToken);

                var sequentialEvents = await new SelectElderEventsBySequenceNameQuery(schema, table)
                                             .GetAsync(connection, sequenceName, limit, cancellationToken)
                                             .Select(postgresOutboxEvent => postgresOutboxEvent.ToOrderedPostgresOutboxEvent())
                                             .ToListAsync(cancellationToken: cancellationToken);

                return new SequentialPostgresWorkingSet(sequentialEvents, transaction);
            }
            // Штатная ситуация: кто-то другой сейчас читает события по текущей последовательности
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.LockNotAvailable)
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }

                return null;
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }

                throw;
            }
        }

        private async Task CommitWorkingSetAsync(IWorkingSet workingSet, CancellationToken cancellationToken)
        {
            if (workingSet is IPostgresWorkingSet postgresWorkingSet)
            {
                try
                {
                    await StoreEvents(postgresWorkingSet, cancellationToken);
                    await postgresWorkingSet.CommitAsync(cancellationToken);
                    postgresWorkingSet.Status = WorkingSetStatus.Completed;
                }
                catch
                {
                    postgresWorkingSet.Status = WorkingSetStatus.Failed;
                    throw;
                }
            }
            else
            {
                throw new ArgumentException($"This working set is not supported by {GetType().Name}", nameof(workingSet));
            }
        }

        private async Task RollbackWorkingSetAsync(IWorkingSet workingSet, CancellationToken cancellationToken)
        {
            if (workingSet is IPostgresWorkingSet postgresWorkingSet)
            {
                try
                {
                    await postgresWorkingSet.RollbackAsync(cancellationToken);
                    postgresWorkingSet.Status = WorkingSetStatus.NotProcessed;
                }
                catch
                {
                    postgresWorkingSet.Status = WorkingSetStatus.Failed;
                    throw;
                }
            }
            else
            {
                throw new ArgumentException($"This working set is not supported by {GetType().Name}", nameof(workingSet));
            }
        }

        private async Task StoreEvents(IPostgresWorkingSet workingSet, CancellationToken cancellationToken)
        {
            var schema = _options.Value.Schema;
            var table = _options.Value.Table;

            var updateStatusWithMetadataQuery = new UpdateStatusWithMetadataQuery(schema, table);

            foreach (var outboxEvent in workingSet.Events)
            {
                var metadata = JsonSerializer.Serialize(outboxEvent.Metadata);
                var status = outboxEvent.Status.ToString("G");

                await updateStatusWithMetadataQuery.UpdateAsync(
                    workingSet.DbConnection,
                    status,
                    metadata,
                    outboxEvent.Id,
                    cancellationToken);
            }
        }
    }
}