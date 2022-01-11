namespace DistributedOutbox
{
    /// <summary>
    /// Событие, передаваемое через Outbox, соблюдающее очередность отправки
    /// </summary>
    public interface IOrderedOutboxEvent : IOutboxEvent
    {
        /// <summary>
        /// Имя последовательности сообщений, соблюдающих строгую очередность
        /// </summary>
        public string SequenceName { get; }
    }
}