using System;

namespace DistributedOutbox.AspNetCore
{
    /// <summary>
    /// Параметры фонового обработчика событий outbox
    /// </summary>
    public class DistributedOutboxServiceOptions
    {
        /// <summary>
        /// Время задержки обработки после сбоя
        /// </summary>
        public TimeSpan ErrorDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Время задержки обработки если за итерацию не было отправлено ни одного события
        /// </summary>
        public TimeSpan NoEventsPublishedDelay { get; set; } = TimeSpan.FromSeconds(10);
    }
}