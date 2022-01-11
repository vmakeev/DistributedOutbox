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
        public virtual long Id { get; set; }

        /// <summary>
        /// Ключ
        /// </summary>
        public virtual string Key { get; set; }

        /// <summary>
        /// Цели события
        /// </summary>
        public virtual string Targets { get; set; }

        /// <summary>
        /// Имя последовательности сообщений, соблюдающих строгую очередность
        /// </summary>
        public virtual string? SequenceName { get; set; }

        /// <summary>
        /// Тип
        /// </summary>
        public virtual string Type { get; set; }

        /// <summary>
        /// Дата возникновения
        /// </summary>
        public virtual DateTime Date { get; set; }

        /// <summary>
        /// Метаданные
        /// </summary>
        public virtual string? Metadata { get; set; }

        /// <summary>
        /// Статус
        /// </summary>
        public virtual string Status { get; set; }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        public virtual string Payload { get; set; }

        public void AfterLoad()
        {
            // do nothing
        }
    }
}