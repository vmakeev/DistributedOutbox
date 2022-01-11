using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Unit of work
    /// </summary>
    /// <remarks>
    /// Позволяет выполнять отложенное сохранение событий в outbox
    /// </remarks>
    public interface IDatabaseUnitOfWork : IAsyncDisposable
    {
        /// <summary>
        /// Добавляет действие <paramref name="action"/> в очередь на исполнение
        /// </summary>
        /// <param name="action">Действие</param>
        Task Enqueue(Func<DbConnection, Task> action);
    }
}