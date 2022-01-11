using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Рабочий набор, содержащий транзакцию, которую можно подтвердить
    /// </summary>
    internal interface IPostgresWorkingSet : IWorkingSet
    {
        /// <summary>
        /// События, входящие в рабочий набор
        /// </summary>
        public new IReadOnlyList<IPostgresOutboxEvent> Events { get; }

        /// <summary>
        /// Выполняет подтверждение транзакции
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        Task CommitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Выполняет откат транзакции
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        Task RollbackAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Используемое подключение к БД
        /// </summary>
        DbConnection DbConnection { get; }

        /// <summary>
        /// Статус рабочего набора
        /// </summary>
        public new WorkingSetStatus Status { get; set; }
    }
}