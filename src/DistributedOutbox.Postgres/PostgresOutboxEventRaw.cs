using System;
#pragma warning disable 8618

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Событие Outbox, хранящееся в БД
    /// </summary>
    public class PostgresOutboxEventRaw
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Ключ
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Цели события
        /// </summary>
        public string Targets { get; set; }

        /// <summary>
        /// Имя последовательности сообщений, соблюдающих строгую очередность
        /// </summary>
        public string? SequenceName { get; set; }

        /// <summary>
        /// Тип
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Дата возникновения
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Метаданные
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Статус
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        public string Payload { get; set; }

        public void AfterLoad()
        {
            // do nothing
        }
    }
}