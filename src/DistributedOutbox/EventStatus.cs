namespace DistributedOutbox
{
    /// <summary>
    /// Статус события
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        /// Новое, не отправлено
        /// </summary>
        New,
        
        /// <summary>
        /// Успешно отправлено
        /// </summary>
        Sent,
        
        /// <summary>
        /// Еще не отправлено по причине сбоя
        /// </summary>
        Failed,
        
        /// <summary>
        /// Отправка отклонена
        /// </summary>
        Declined
    }
}