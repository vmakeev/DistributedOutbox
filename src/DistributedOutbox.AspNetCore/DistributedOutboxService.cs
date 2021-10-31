using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DistributedOutbox.AspNetCore
{
    /// <summary>
    /// Фоновый сервис обработки сообщений outbox
    /// </summary>
    internal sealed class DistributedOutboxService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DistributedOutboxService> _logger;
        private readonly IOptions<DistributedOutboxServiceOptions> _options;

        public DistributedOutboxService(IServiceScopeFactory serviceScopeFactory,
                                        ILogger<DistributedOutboxService> logger,
                                        IOptions<DistributedOutboxServiceOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options;
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => WorkingLoop(stoppingToken));
        }

        private async Task WorkingLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var serviceScope = _serviceScopeFactory.CreateScope();
                    var outboxProcessor = serviceScope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                    var sentEventsCount = await outboxProcessor.ProcessAsync(cancellationToken);

                    _logger.LogTrace($"{sentEventsCount} events produced");

                    if (sentEventsCount == 0)
                    {
                        await Task.Delay(_options.Value.NoEventsPublishedDelay, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"{nameof(DistributedOutboxService)} stopping due to stoppingToken has been canceled");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing");
                    await Task.Delay(_options.Value.ErrorDelay, cancellationToken);
                }
            }
        }
    }
}