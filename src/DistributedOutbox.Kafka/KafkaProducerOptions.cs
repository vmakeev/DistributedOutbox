using System.Text;
using Confluent.Kafka;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Параметры работы продьюсера сообщений
    /// </summary>
    public class KafkaProducerOptions
    {
        /// <summary>
        /// Конфигурация
        /// </summary>
        public ProducerConfig? ProducerConfig { get; set; }

        /// <summary>
        /// Кодировка сообщений
        /// </summary>
        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;
    }
}