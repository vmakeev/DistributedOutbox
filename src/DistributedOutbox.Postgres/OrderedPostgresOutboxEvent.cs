using System;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc cref="IOrderedPostgresOutboxEvent" />
    internal class OrderedPostgresOutboxEvent : PostgresOutboxEvent, IOrderedPostgresOutboxEvent
    {
        /// <inheritdoc />
        public OrderedPostgresOutboxEvent(PostgresOutboxEventRaw source)
            : base(source)
        {
            if (string.IsNullOrEmpty(source.SequenceName))
            {
                throw new ArgumentException($"{nameof(SequenceName)} can not be null or empty.");
            }

            SequenceName = source.SequenceName;
        }

        /// <inheritdoc />
        public string SequenceName { get; }
    }
}