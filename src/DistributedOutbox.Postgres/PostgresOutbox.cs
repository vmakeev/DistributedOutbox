using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DistributedOutbox.Postgres.Queries;
using Microsoft.Extensions.Options;

namespace DistributedOutbox.Postgres
{
    internal class PostgresOutbox : IOutbox
    {
        private readonly AddEventsQuery _addEventsQuery;
        private readonly GetNextEventIdQuery _getNextEventIdQuery;

        private readonly IEventTargetsProvider _eventTargetsProvider;
        private readonly IDatabaseUnitOfWork _databaseUnitOfWork;

        public PostgresOutbox(IOptions<PostgresWorkingSetOptions> options,
                              IEventTargetsProvider eventTargetsProvider,
                              IDatabaseUnitOfWork databaseUnitOfWork)
        {
            _eventTargetsProvider = eventTargetsProvider;
            _databaseUnitOfWork = databaseUnitOfWork;
            _addEventsQuery = new AddEventsQuery(options.Value.Schema, options.Value.Table);
            _getNextEventIdQuery = new GetNextEventIdQuery(options.Value.Schema);
        }

        /// <inheritdoc />
        public async Task AddEventAsync(IOutboxEventData outboxEventData, CancellationToken cancellationToken)
        {
            await AddEventsAsync(new[] { outboxEventData }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddEventsAsync(IEnumerable<IOutboxEventData> outboxEventsData,
                                         CancellationToken cancellationToken)
        {
            await _databaseUnitOfWork.Enqueue(connection => AddEventsInternal(connection, outboxEventsData, cancellationToken));
        }

        private async Task AddEventsInternal(DbConnection connection,
                                             IEnumerable<IOutboxEventData> outboxEventsData,
                                             CancellationToken cancellationToken)
        {
            var itemTasks = outboxEventsData
                            .Select(data => GetRawEventAsync(connection, data, cancellationToken))
                            .ToArray();

            await _addEventsQuery.AddAsync(
                connection: connection,
                items: (await Task.WhenAll(itemTasks)).ToArray(),
                cancellationToken: cancellationToken);
        }

        private async Task<PostgresOutboxEventRaw> GetRawEventAsync(DbConnection connection,
                                                                    IOutboxEventData data,
                                                                    CancellationToken cancellationToken)
        {
            var target = new PostgresOutboxEventRaw
            {
                Id = await GetNextEventIdAsync(connection, cancellationToken),
                Date = data.EventDate,
                Key = data.EventKey,
                Targets = JsonSerializer.Serialize(_eventTargetsProvider.GetTargets(data.EventType)),
                Metadata = JsonSerializer.Serialize(PostgresOutboxEventMetadata.Empty),
                Payload = JsonSerializer.Serialize(data.Payload),
                Status = EventStatus.New.ToString("G"),
                Type = data.EventType,
                SequenceName = data.SequenceName
            };

            return target;
        }

        // todo: hi-lo?
        private async ValueTask<long> GetNextEventIdAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            return await _getNextEventIdQuery
                         .GetAsync(connection, cancellationToken)
                         .SingleAsync(cancellationToken) ??
                   throw new InvalidOperationException("Can not fetch next event id from database");
        }
    }
}