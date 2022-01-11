using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Предварительный обработчик сообщения kafka
    /// </summary>
    internal interface IKafkaMessagePreprocessor
    {
        /// <summary>
        /// Выполняет предварительную обработку сообщения kafka перед отправкой
        /// </summary>
        /// <param name="message">Подготовленное к отправке сообщение</param>
        /// <param name="sourceEvent">Исходное событие</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task Preprocess(Message<string, byte[]> message, IOutboxEvent sourceEvent, CancellationToken cancellationToken);
    }
}