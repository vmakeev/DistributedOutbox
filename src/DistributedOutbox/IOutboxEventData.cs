using System;

namespace DistributedOutbox
{
    /// <summary>
    /// Данные события
    /// </summary>
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
        /// Метаданные события
        /// </summary>
        IOutboxEventMetadata Metadata { get; }

        /// <summary>
        /// Полезная нагрузка события
        /// </summary>
        object Payload { get; }
    }
}