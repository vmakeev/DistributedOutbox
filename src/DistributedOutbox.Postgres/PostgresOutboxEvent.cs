using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    internal class PostgresOutboxEvent : IPostgresOutboxEvent
    {
        public PostgresOutboxEvent(PostgresOutboxEventRaw source)
        {
            Id = source.Id;
            EventKey = source.Key;
            EventType = source.Type;
            EventDate = source.Date;
            EventTargets = string.IsNullOrEmpty(source.Targets)
                ? Array.Empty<string>()
                : JsonSerializer.Deserialize<string[]>(source.Targets) ?? Array.Empty<string>();
            Metadata = string.IsNullOrEmpty(source.Metadata)
                ? new PostgresOutboxEventMetadata()
                : JsonSerializer.Deserialize<PostgresOutboxEventMetadata>(source.Metadata) ?? new PostgresOutboxEventMetadata();
            Status = Enum.Parse<EventStatus>(source.Status);
            Payload = source.Payload;
        }

        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public string EventKey { get; }

        /// <inheritdoc />
        public string EventType { get; }

        /// <inheritdoc />
        public IEnumerable<string> EventTargets { get; }

        /// <inheritdoc />
        public DateTime EventDate { get; }

        /// <inheritdoc />
        public IOutboxEventMetadata Metadata { get; }

        /// <inheritdoc />
        public EventStatus Status { get; private set; }

        /// <inheritdoc />
        public string Payload { get; }

        /// <inheritdoc />
        public void MarkCompleted()
        {
            switch (Status)
            {
                case EventStatus.New:
                case EventStatus.Failed:
                    Status = EventStatus.Sent;
                    Metadata[MetadataKeys.SentTime] = DateTime.UtcNow.ToString("O");
                    break;

                case EventStatus.Sent:
                    break;

                case EventStatus.Declined:
                default:
                    throw new ArgumentException($"Can not mark event as {nameof(EventStatus.Sent)} due to current status: {Status:G}");
            }
        }

        /// <inheritdoc />
        public void MarkFailed(string reason)
        {
            switch (Status)
            {
                case EventStatus.New:
                case EventStatus.Failed:
                    Status = EventStatus.Failed;
                    break;

                case EventStatus.Sent:
                case EventStatus.Declined:
                default:
                    throw new ArgumentException($"Can not mark event as {nameof(EventStatus.Failed)} due to current status: {Status:G}");
            }

            Metadata[MetadataKeys.LastFailureReason] = reason;
        }

        /// <inheritdoc />
        public void MarkDeclined(string reason)
        {
            switch (Status)
            {
                case EventStatus.New:
                case EventStatus.Failed:
                case EventStatus.Declined:
                    Status = EventStatus.Declined;
                    break;

                case EventStatus.Sent:
                default:
                    throw new ArgumentException($"Can not mark event as {nameof(EventStatus.Declined)} due to current status: {Status:G}");
            }

            Metadata[MetadataKeys.LastFailureReason] = reason;
        }
    }
}