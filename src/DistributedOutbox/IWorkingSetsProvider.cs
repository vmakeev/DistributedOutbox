using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox
{
    /// <summary>
    /// Поставщик информации о рабочих наборах
    /// </summary>
    public interface IWorkingSetsProvider
    {
        /// <summary>
        /// Загружает рабочие наборы
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Рабочие наборы</returns>
        public Task<IReadOnlyCollection<IWorkingSet>> AcquireWorkingSetsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Освобождает рабочий набор
        /// </summary>
        /// <param name="workingSet">Рабочий набор</param>
        /// <param name="isProcessed">Был ли обработан рабочий набор</param>
        /// <param name="cancellationToken">Токен отмены</param>
        public Task ReleaseWorkingSetAsync(IWorkingSet workingSet, bool isProcessed, CancellationToken cancellationToken);
    }
}