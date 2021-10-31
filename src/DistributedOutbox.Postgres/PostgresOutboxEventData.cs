using System;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    public class PostgresOutboxEventData : IOutboxEventData
    {
        public PostgresOutboxEventData(
            string eventKey,
            string eventType,
            object payload)
        {
            EventKey = eventKey;
            EventType = eventType;
            SequenceName = null;
            EventDate = DateTime.UtcNow;
            Payload = payload;
        }

        public PostgresOutboxEventData(
            string eventKey,
            string eventType,
            string sequenceName,
            object payload)
        {
            EventKey = eventKey;
            EventType = eventType;
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
        public object Payload { get; }
    }
}