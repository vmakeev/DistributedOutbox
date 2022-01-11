using System;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    public class PostgresOutboxEventData : IOutboxEventData
    {
        public PostgresOutboxEventData(
            string eventKey,
            string eventType,
            IOutboxEventMetadata metadata,
            object payload)
        {
            EventKey = eventKey;
            EventType = eventType;
            Metadata = metadata;
            SequenceName = null;
            EventDate = DateTime.UtcNow;
            Payload = payload;
        }

        public PostgresOutboxEventData(
            string eventKey,
            string eventType,
            string sequenceName,
            IOutboxEventMetadata metadata,
            object payload)
        {
            EventKey = eventKey;
            EventType = eventType;
            Metadata = metadata;
            SequenceName = sequenceName;
            EventDate = DateTime.UtcNow;
            Payload = payload;
        }

        /// <inheritdoc />
        public string EventKey { get; }

        /// <inheritdoc />
        public string EventType { get; }

        /// <inheritdoc />
        public string? SequenceName { get; }

        /// <inheritdoc />
        public DateTime EventDate { get; }

        /// <inheritdoc />
        public IOutboxEventMetadata Metadata { get; set; }

        /// <inheritdoc />
        public object Payload { get; }
    }
}