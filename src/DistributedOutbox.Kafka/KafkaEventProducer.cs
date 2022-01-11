﻿using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<IKafkaMessagePreprocessor> _messagePreprocessors;
        private readonly Lazy<IProducer<string, byte[]>> _eventProducerFactory;

        public KafkaEventProducer(IOptions<KafkaProducerOptions> options, IEnumerable<IKafkaMessagePreprocessor> messagePreprocessors)
        {
            _options = options;
            _messagePreprocessors = messagePreprocessors;
            if (_options.Value.ProducerConfig is null)
            {
                throw new ArgumentException($"{nameof(KafkaProducerOptions.ProducerConfig)} must be specified.");
            }

            _eventProducerFactory = new Lazy<IProducer<string, byte[]>>(
                () => new ProducerBuilder<string, byte[]>(_options.Value.ProducerConfig).Build(),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <inheritdoc />
        public async Task ProduceAsync(string topic, IOutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            var message = new Message<string, byte[]>
            {
                Key = outboxEvent.EventKey,
                Value = _options.Value.MessageEncoding.GetBytes(outboxEvent.Payload),
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            foreach (var preprocessor in _messagePreprocessors)
            {
                await preprocessor.Preprocess(message, outboxEvent, cancellationToken);
            }

            await _eventProducerFactory.Value.ProduceAsync(topic, message, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_eventProducerFactory.IsValueCreated)
            {
                _eventProducerFactory.Value.Dispose();
            }
        }
    }
}