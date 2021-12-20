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

                var totalSentEventsCount = 0;
                
                while (tasksDictionary.Any())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // освобождаем отдельные рабочие наборы сразу по завершении обработки,
                    // чтобы не ждать пока будут обработаны все
                    var finishedTask = await Task.WhenAny(tasksDictionary.Keys);
                    if (!tasksDictionary.Remove(finishedTask, out var workingSet))
                    {
                        throw new InvalidOperationException("Cannot find finished task in dictionary.");
                    }

                    var isProcessed = finishedTask.IsCompletedSuccessfully;
                    await _workingSetsProvider.ReleaseWorkingSetAsync(workingSet, isProcessed, cancellationToken);

                    totalSentEventsCount += finishedTask.Result;
                }
                
                return totalSentEventsCount;
            }
            finally
            {
                foreach (var workingSet in workingSets)
                {
                    await workingSet.DisposeAsync();
                }
            }
        }

        private IDictionary<Task<int>, IWorkingSet> StartWorkingSetsProcessing(IReadOnlyCollection<IWorkingSet> workingSets, CancellationToken cancellationToken)
        {
            var sequentialSets = workingSets.Where(workingSet => workingSet is ISequentialWorkingSet);
            var parallelSets = workingSets.Where(workingSet => workingSet is not ISequentialWorkingSet);

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

            var tasksDictionary = new Dictionary<Task<int>, IWorkingSet>();
            foreach (var (task, workingSet) in parallelSetsTasks.Concat(sequentialSetsTasks))
            {
                tasksDictionary.Add(task, workingSet);
            }

            return tasksDictionary;
        }
    }
}