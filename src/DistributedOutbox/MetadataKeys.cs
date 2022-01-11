namespace DistributedOutbox
{
    /// <summary>
    /// Ключи метаданных
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// Причина последнего сбоя в отправке
        /// </summary>
        public static string LastFailureReason => nameof(LastFailureReason);
        
        /// <summary>
        /// Время отправки
        /// </summary>
        public static string SentTime => nameof(SentTime);
    }
}