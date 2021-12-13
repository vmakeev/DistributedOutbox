using System;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Маппер <see cref="PostgresOutboxEventRaw"/> в события
    /// </summary>
    internal static class OutboxEventMapper
    {
        /// <summary>
        /// Преобразует <paramref name="source" /> в <see cref="IPostgresOutboxEvent" />
        /// </summary>
        /// <param name="source">Преобразуемый объект</param>
        /// <returns>Результат преобразования</returns>
        public static IPostgresOutboxEvent ToPostgresOutboxEvent(this PostgresOutboxEventRaw source)
        {
            return string.IsNullOrEmpty(source.SequenceName)
                ? new PostgresOutboxEvent(source)
                : new OrderedPostgresOutboxEvent(source);
        }

        /// <summary>
        /// Преобразует <paramref name="source" /> в <see cref="IOrderedPostgresOutboxEvent" />
        /// </summary>
        /// <param name="source">Преобразуемый объект</param>
        /// <returns>Результат преобразования</returns>
        public static IOrderedPostgresOutboxEvent ToOrderedPostgresOutboxEvent(this PostgresOutboxEventRaw source)
        {
            if (string.IsNullOrEmpty(source.SequenceName))
            {
                throw new ArgumentException($"{nameof(PostgresOutboxEventRaw.SequenceName)} can not be null or empty.", nameof(source.SequenceName));
            }

            return new OrderedPostgresOutboxEvent(source);
        }
    }
}