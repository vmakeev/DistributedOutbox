﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Обработчик рабочего набора, выполняющий параллельную отправку сообщений в kafka
    /// </summary>
    internal class KafkaParallelWorkingSetProcessor : IParallelWorkingSetProcessor
    {
        private readonly ILogger<KafkaParallelWorkingSetProcessor> _logger;
        private readonly IEventProducer _eventProducer;

        public KafkaParallelWorkingSetProcessor(ILogger<KafkaParallelWorkingSetProcessor> logger,
                                                IEventProducer eventProducer)
        {
            _logger = logger;
            _eventProducer = eventProducer;
        }

        /// <inheritdoc />
        public async Task<int> ProcessAsync(IWorkingSet workingSet, CancellationToken cancellationToken)
        {
            var publishTasks = new List<Task<EventStatus>>();

            foreach (var outboxEvent in workingSet.Events)
            {
                publishTasks.Add(PublishEventAsync(outboxEvent, cancellationToken));
            }

            await Task.WhenAll(publishTasks);

            return publishTasks.Count(task => task.Result is EventStatus.Sent);
        }

        private async Task<EventStatus> PublishEventAsync(IOutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            try
            {
                var tasks = outboxEvent.EventTargets
                                       .Where(eventTarget => !string.IsNullOrEmpty(eventTarget))
                                       .Select(
                                           eventTarget =>
                                               _eventProducer.ProduceAsync(eventTarget, outboxEvent, cancellationToken))
                                       .ToArray();

                await Task.WhenAll(tasks);
                outboxEvent.MarkCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not publish event of type {EventType} ({@Event})", outboxEvent.EventType, outboxEvent);
                outboxEvent.MarkFailed($"Exception occurred: {ex}");
            }

            return outboxEvent.Status;
        }
    }
}