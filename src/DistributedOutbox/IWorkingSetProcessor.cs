using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox
{
    /// <summary>
    /// Обработчик рабочего набора
    /// </summary>
    public interface IWorkingSetProcessor
    {
        /// <summary>
        /// Выполняет обработку событий, входящих в рабочий набор
        /// </summary>
        /// <param name="workingSet">Рабочий набор</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task<IWorkingSet> ProcessAsync(IWorkingSet workingSet, CancellationToken cancellationToken);
    }
}