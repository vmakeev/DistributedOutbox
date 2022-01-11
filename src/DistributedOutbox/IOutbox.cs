using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox
{
    /// <summary>
    /// Outbox
    /// </summary>
    public interface IOutbox
    {
        /// <summary>
        /// Добавляет событие в Outbox
        /// </summary>
        /// <param name="outboxEventData">Добавляемое событие</param>
        /// <param name="cancellationToken">Токен отмены</param>
        public Task AddEventAsync(IOutboxEventData outboxEventData, CancellationToken cancellationToken);

        /// <summary>
        /// Добавляет события в Outbox
        /// </summary>
        /// <param name="outboxEventsData">Добавляемые события</param>
        /// <param name="cancellationToken">Токен отмены</param>
        public Task AddEventsAsync(IEnumerable<IOutboxEventData> outboxEventsData, CancellationToken cancellationToken);
    }
}