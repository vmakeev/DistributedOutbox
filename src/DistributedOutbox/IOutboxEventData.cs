using System;

namespace DistributedOutbox
{
    public interface IOutboxEventData
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
        /// Имя последовательности сообщений, соблюдающих строгую очередность
        /// </summary>
        string? SequenceName { get; }

        /// <summary>
        /// Дата возникновения события
        /// </summary>
        DateTime EventDate { get; }

        /// <summary>
        /// Полезная нагрузка события
        /// </summary>
        object Payload { get; }
    }
}