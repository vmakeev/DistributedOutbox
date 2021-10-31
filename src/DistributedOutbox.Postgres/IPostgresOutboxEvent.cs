namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    internal interface IPostgresOutboxEvent : IOutboxEvent
    {
        /// <summary>
        /// Идентификатор события в БД
        /// </summary>
        public long Id { get; }
    }
}