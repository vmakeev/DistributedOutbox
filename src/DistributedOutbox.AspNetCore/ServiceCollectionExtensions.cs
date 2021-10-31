using System;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedOutbox.AspNetCore
{
    /// <summary>
    /// Расширения для регистрации Outbox
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет обработчик исходящих событий
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="configure">Конфигурация сервиса</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddDistributedOutbox(this IServiceCollection services, Action<DistributedOutboxServiceOptions> configure)
        {
            services.Configure<DistributedOutboxServiceOptions>(configure);
            services.AddHostedService<DistributedOutboxService>();
            services.AddTransient<IOutboxProcessor, OutboxProcessor>();

            return services;
        }
    }
}