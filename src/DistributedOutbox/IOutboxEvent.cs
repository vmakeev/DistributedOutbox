using System;
using System.Collections.Generic;

namespace DistributedOutbox
{
    /// <summary>
    /// Событие, передаваемое через Outbox
    /// </summary>
    public interface IOutboxEvent
    {
        /// <summary>
        /// Ключ события
        /// </summary>
        string EventKey { get; }

        /// <summary>
        /// Тип события
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Цели события
        /// </summary>
        IEnumerable<string> EventTargets { get; }

        /// <summary>
        /// Дата возникновения события
        /// </summary>
        DateTime EventDate { get; }

        /// <summary>
        /// Метаданные события
        /// </summary>
        IOutboxEventMetadata Metadata { get; }

        /// <summary>
        /// Статус события
        /// </summary>
        EventStatus Status { get; }

        /// <summary>
        /// Полезная нагрузка события
        /// </summary>
        string Payload { get; }

        /// <summary>
        /// Помечает событие как завершенное
        /// </summary>
        void MarkCompleted();

        /// <summary>
        /// Помечает событие как сбойное
        /// </summary>
        /// <param name="reason">Причина сбоя</param>
        void MarkFailed(string reason);

        /// <summary>
        /// Помечает событие как отклоненное 
        /// </summary>
        /// <param name="reason">Причина отклонения</param>
        void MarkDeclined(string reason);
    }
}