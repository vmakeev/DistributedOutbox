using System.Collections.Generic;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc cref="IOutboxEventMetadata"/>
    public class PostgresOutboxEventMetadata : Dictionary<string, string?>, IOutboxEventMetadata
    {
        /// <summary>
        /// Пустой набор метаданных
        /// </summary>
        public static IOutboxEventMetadata Empty => new PostgresOutboxEventMetadata();
    }
}