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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var serviceScope = _serviceScopeFactory.CreateScope();
                    var outboxProcessor = serviceScope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                    var sentEventsCount = await outboxProcessor.ProcessAsync(stoppingToken);

                    _logger.LogTrace("{SentEventsCount} events produced", sentEventsCount);

                    if (sentEventsCount == 0)
                    {
                        await Task.Delay(_options.Value.NoEventsPublishedDelay, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing");
                    await Task.Delay(_options.Value.ErrorDelay, stoppingToken);
                }
            }

            _logger.LogInformation(nameof(DistributedOutboxService) + " stopping due to stoppingToken has been canceled");
        }
    }
}