namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Параметры рабочего набора
    /// </summary>
    public class PostgresWorkingSetOptions
    {
        /// <summary>
        /// Используемая схема БД
        /// </summary>
        public string Schema { get; set; } = "public";

        /// <summary>
        /// Имя таблицы с событиями
        /// </summary>
        public string Table { get; set; } = "OutboxEvents";

        /// <summary>
        /// Максимальное количество загружаемых сообщений для параллельной отправки
        /// </summary>
        public int ParallelLimit { get; set; } = 100;

        /// <summary>
        /// Максимальное количество загружаемых сообщений для последовательной отправки
        /// </summary>
        public int SequentialLimit { get; set; } = 100;
    }
}