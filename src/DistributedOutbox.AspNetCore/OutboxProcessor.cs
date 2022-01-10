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
                var sequentialSets = workingSets.Where(workingSet => workingSet is ISequentialWorkingSet);
                var parallelSets = workingSets.Where(workingSet => workingSet is not ISequentialWorkingSet);

                var parallelSetsTasks = ProcessWorkingSets(
                    workingSetProcessor: _parallelWorkingSetProcessor,
                    workingSets: parallelSets,
                    cancellationToken: cancellationToken);

                var sequentialSetsTasks = ProcessWorkingSets(
                    workingSetProcessor: _sequentialWorkingSetProcessor,
                    workingSets: sequentialSets,
                    cancellationToken: cancellationToken);

                return (await Task.WhenAll(parallelSetsTasks.Concat(sequentialSetsTasks))).Sum();
            }
            finally
            {
                foreach (var workingSet in workingSets)
                {
                    await workingSet.DisposeAsync();
                }
            }
        }

        private IEnumerable<Task<int>> ProcessWorkingSets(IWorkingSetProcessor workingSetProcessor, IEnumerable<IWorkingSet> workingSets, CancellationToken cancellationToken)
        {
            return workingSets
                .Select(
                    parallelSet =>
                        workingSetProcessor.ProcessAsync(parallelSet, cancellationToken)
                                           .ContinueWith(
                                               async task =>
                                               {
                                                   await _workingSetsProvider.ReleaseWorkingSetAsync(
                                                       workingSet: parallelSet,
                                                       isProcessed: task.IsCompletedSuccessfully,
                                                       cancellationToken: cancellationToken);
                                                   return task.GetAwaiter().GetResult();
                                               },
                                               cancellationToken)
                                           .Unwrap()
                );
        }
    }
}