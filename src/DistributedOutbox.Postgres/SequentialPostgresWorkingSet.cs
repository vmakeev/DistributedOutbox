using System.Collections.Generic;
using System.Data.Common;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Рабочий набор для последовательно обрабатываемых событий
    /// </summary>
    internal class SequentialPostgresWorkingSet : PostgresWorkingSet, ISequentialWorkingSet
    {
        /// <inheritdoc />
        public SequentialPostgresWorkingSet(IReadOnlyList<IPostgresOutboxEvent> events, DbTransaction transaction)
            : base(events, transaction)
        {
        }
    }
}