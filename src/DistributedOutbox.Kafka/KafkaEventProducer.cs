using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace DistributedOutbox.Kafka
{
    /// <inheritdoc />
    internal sealed class KafkaEventProducer : IEventProducer
    {
        private readonly IOptions<KafkaProducerOptions> _options;
        private readonly IProducer<string, byte[]> _eventProducer;

        public KafkaEventProducer(IOptions<KafkaProducerOptions> options)
        {
            _options = options;
            if (_options.Value.ProducerConfig is null)
            {
                throw new ArgumentException($"{nameof(KafkaProducerOptions.ProducerConfig)} must be specified.");
            }
            
            var builder = new ProducerBuilder<string, byte[]>(options.Value.ProducerConfig);
            _eventProducer = builder.Build();
        }

        /// <inheritdoc />
        public Task ProduceAsync(string topic, IOutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            var message = new Message<string, byte[]>
            {
                Key = outboxEvent.EventKey,
                Value = _options.Value.MessageEncoding.GetBytes(outboxEvent.Payload),
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            return _eventProducer.ProduceAsync(topic, message, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _eventProducer.Dispose();
        }
    }
}