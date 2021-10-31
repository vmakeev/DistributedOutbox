namespace DistributedOutbox
{
    /// <summary>
    /// Статус рабочего набора
    /// </summary>
    public enum WorkingSetStatus
    {
        /// <summary>
        /// Активен
        /// </summary>
        Active,
        
        /// <summary>
        /// Обработан успешно
        /// </summary>
        Completed,
        
        /// <summary>
        /// Не обработан
        /// </summary>
        NotProcessed,
        
        /// <summary>
        /// Обработка завершилась ошибкой
        /// </summary>
        Failed
    }
}