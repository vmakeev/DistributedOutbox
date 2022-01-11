using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Публикатор сообщений
    /// </summary>
    public interface IEventProducer: IDisposable
    {
        /// <summary>
        /// Выполняет публикацию сообщения в kafka
        /// </summary>
        /// <param name="topic">Целевой топик</param>
        /// <param name="outboxEvent">Событие, на базе которого будет построено сообщение</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task ProduceAsync(string topic, IOutboxEvent outboxEvent, CancellationToken cancellationToken);
    }
}