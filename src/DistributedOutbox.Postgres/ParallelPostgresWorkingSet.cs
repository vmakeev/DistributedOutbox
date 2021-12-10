using System.Collections.Generic;
using System.Data.Common;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Рабочий набор для параллельно обрабатываемых событий
    /// </summary>
    internal class ParallelPostgresWorkingSet : PostgresWorkingSet, IParallelWorkingSet
    {
        /// <inheritdoc />
        public ParallelPostgresWorkingSet(IReadOnlyList<IPostgresOutboxEvent> events, DbTransaction transaction)
            : base(events, transaction)
        {
        }
    }
}