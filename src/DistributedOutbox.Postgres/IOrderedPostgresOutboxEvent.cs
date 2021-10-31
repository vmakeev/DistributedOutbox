namespace DistributedOutbox.Postgres
{
    /// <inheritdoc cref="IOrderedOutboxEvent" />
    internal interface IOrderedPostgresOutboxEvent : IPostgresOutboxEvent, IOrderedOutboxEvent
    {
    }
}