using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Обработчик рабочего набора, выполняющий последовательную отправку сообщений в kafka
    /// </summary>
    internal class KafkaSequentialWorkingSetProcessor : ISequentialWorkingSetProcessor
    {
        private readonly ILogger<KafkaSequentialWorkingSetProcessor> _logger;
        private readonly IEventProducer _eventProducer;

        public KafkaSequentialWorkingSetProcessor(ILogger<KafkaSequentialWorkingSetProcessor> logger,
                                                  IEventProducer eventProducer)
        {
            _logger = logger;
            _eventProducer = eventProducer;
        }

        /// <inheritdoc />
        public async Task<IWorkingSet> ProcessAsync(IWorkingSet workingSet, CancellationToken cancellationToken)
        {
            foreach (IOutboxEvent outboxEvent in workingSet.Events)
            {
                try
                {
                    var isProduced = false;
                    foreach (var eventTarget in outboxEvent.EventTargets.Where(target => !string.IsNullOrEmpty(target)))
                    {
                        await _eventProducer.ProduceAsync(eventTarget, outboxEvent, cancellationToken);
                        isProduced = true;
                    }

                    if (isProduced)
                    {
                        outboxEvent.MarkCompleted();
                    }
                    else
                    {
                        outboxEvent.MarkDeclined("No matching target was found.");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Can not publish event of type {outboxEvent.EventType}");
                    outboxEvent.MarkFailed($"Exception occurred: {ex}");
                }
            }

            return workingSet;
        }
    }
}