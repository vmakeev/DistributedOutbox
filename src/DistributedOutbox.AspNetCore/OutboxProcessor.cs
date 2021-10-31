using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedOutbox.AspNetCore
{
    /// <inheritdoc />
    internal sealed class OutboxProcessor : IOutboxProcessor
    {
        private readonly IWorkingSetsProvider _workingSetsProvider;
        private readonly IParallelWorkingSetProcessor _parallelWorkingSetProcessor;
        private readonly ISequentialWorkingSetProcessor _sequentialWorkingSetProcessor;

        public OutboxProcessor(IWorkingSetsProvider workingSetsProvider,
                               IParallelWorkingSetProcessor parallelWorkingSetProcessor,
                               ISequentialWorkingSetProcessor sequentialWorkingSetProcessor)
        {
            _workingSetsProvider = workingSetsProvider;
            _parallelWorkingSetProcessor = parallelWorkingSetProcessor;
            _sequentialWorkingSetProcessor = sequentialWorkingSetProcessor;
        }

        /// <inheritdoc />
        public async Task<int> ProcessAsync(CancellationToken cancellationToken)
        {
            var workingSets = await _workingSetsProvider.AcquireWorkingSetsAsync(cancellationToken);

            try
            {
                var tasksDictionary = StartWorkingSetsProcessing(workingSets, cancellationToken);

                while (tasksDictionary.Any())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // освобождаем отдельные рабочие наборы сразу по завершении обработки,
                    // чтобы не ждать пока будут обработаны все
                    var finishedTask = await Task.WhenAny(tasksDictionary.Keys);
                    if (tasksDictionary.Remove(finishedTask, out var workingSet))
                    {
                        var isProcessed = finishedTask.IsCompletedSuccessfully;
                        await _workingSetsProvider.ReleaseWorkingSetAsync(workingSet, isProcessed, cancellationToken);
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot find finished task in dictionary.");
                    }
                }

                var sentEventsCount = workingSets
                                      .Where(workingSet => workingSet.Status == WorkingSetStatus.Completed)
                                      .Sum(workingSet => workingSet.Events.Count(outboxEvent => outboxEvent.Status == EventStatus.Sent));
                return sentEventsCount;
            }
            finally
            {
                foreach (IWorkingSet workingSet in workingSets)
                {
                    await workingSet.DisposeAsync();
                }
            }
        }

        private IDictionary<Task, IWorkingSet> StartWorkingSetsProcessing(IReadOnlyCollection<IWorkingSet> workingSets, CancellationToken cancellationToken)
        {
            // если в рабочем наборе есть хотя бы одно событие, для которого важна очередность,
            // то весь рабочий набор будет обрабатываться последовательно
            var parallelSets = workingSets.Where(workingSet => !workingSet.Events.OfType<IOrderedOutboxEvent>().Any());
            var sequentialSets = workingSets.Where(workingSet => workingSet.Events.OfType<IOrderedOutboxEvent>().Any());

            var parallelSetsTasks = parallelSets
                .Select(
                    workingSet =>
                    (
                        _parallelWorkingSetProcessor.ProcessAsync(
                            workingSet,
                            cancellationToken),
                        workingSet
                    ));

            var sequentialSetsTasks = sequentialSets
                .Select(
                    workingSet =>
                    (
                        _sequentialWorkingSetProcessor.ProcessAsync(
                            workingSet,
                            cancellationToken),
                        workingSet
                    ));

            var tasksDictionary = new Dictionary<Task, IWorkingSet>();
            foreach ((Task task, IWorkingSet workingSet) in parallelSetsTasks.Concat(sequentialSetsTasks))
            {
                tasksDictionary.Add(task, workingSet);
            }

            return tasksDictionary;
        }
    }
}