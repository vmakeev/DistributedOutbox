using System.Collections.Generic;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc cref="IOutboxEventMetadata"/>
    internal class PostgresOutboxEventMetadata : Dictionary<string, object?>, IOutboxEventMetadata
    {
        public static IOutboxEventMetadata Empty => new PostgresOutboxEventMetadata();
    }
}