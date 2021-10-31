using System.Collections.Generic;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Сопоставление типа события и целей его назначения
    /// </summary>
    internal interface IEventTypeToTargetsMap
    {
        /// <summary>
        /// Тип события
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Цели события
        /// </summary>
        IEnumerable<string> Targets { get; }
    }
}