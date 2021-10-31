using System.Collections.Generic;

namespace DistributedOutbox
{
    /// <summary>
    /// Поставщик информации о необходимых целях отправки события
    /// </summary>
    public interface IEventTargetsProvider
    {
        /// <summary>
        /// Возвращает список целей для указанного типа сообщения
        /// </summary>
        /// <param name="eventType">Тип сообщения</param>
        /// <returns>Список целей для указанного типа сообщения</returns>
        public IEnumerable<string> GetTargets(string eventType);
    }
}