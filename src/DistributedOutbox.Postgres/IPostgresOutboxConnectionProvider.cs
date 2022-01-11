using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Поставщик подключений к БД
    /// </summary>
    public interface IPostgresOutboxConnectionProvider
    {
        /// <summary>
        /// Возвращает открытое подключение к БД
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        Task<DbConnection> GetDbConnectionAsync(CancellationToken cancellationToken);
    }
}