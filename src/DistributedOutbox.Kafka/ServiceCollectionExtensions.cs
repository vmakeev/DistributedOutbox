using System;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedOutbox.Kafka
{
    /// <summary>
    /// Расширения для регистрации отправки сообщений в kafka
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет возможность отправки событий outbox в kafka
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="configure"></param>
        /// <returns><see cref="IServiceCollection"/></returns>
        public static IServiceCollection UseKafkaOutboxTarget(this IServiceCollection services, Action<KafkaProducerOptions> configure)
        {
            services.AddTransient<IWorkingSetProcessor, KafkaParallelWorkingSetProcessor>();
            services.AddTransient<IParallelWorkingSetProcessor, KafkaParallelWorkingSetProcessor>();
            services.AddTransient<ISequentialWorkingSetProcessor, KafkaSequentialWorkingSetProcessor>();

            services.Configure<KafkaProducerOptions>(configure);

            services.AddSingleton<IEventProducer, KafkaEventProducer>();

            return services;
        }
    }
}