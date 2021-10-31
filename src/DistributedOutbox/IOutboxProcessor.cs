using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox
{
    /// <summary>
    /// Обработчик outbox
    /// </summary>
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Выполняет загрузку событий и отправку их по назначению
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Количество успешно отправленных сообщений</returns>
        Task<int> ProcessAsync(CancellationToken cancellationToken);
    }
}