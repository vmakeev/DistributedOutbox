using System;
using System.Collections.Generic;

namespace DistributedOutbox
{
    /// <summary>
    /// Рабочий набор
    /// </summary>
    public interface IWorkingSet: IAsyncDisposable
    {
        /// <summary>
        /// События, входящие в рабочий набор
        /// </summary>
        public IReadOnlyList<IOutboxEvent> Events { get; }
        
        /// <summary>
        /// Статус рабочего набора
        /// </summary>
        public WorkingSetStatus Status { get; }
    }
}