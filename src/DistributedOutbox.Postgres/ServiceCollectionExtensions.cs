using System;
using System.Collections.Generic;
using System.Linq;
using DistributedOutbox.Postgres.EFIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedOutbox.Postgres
{
    /// <summary>
    /// Расширения для хранения событий outbox в БД postgres
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет возможность хранения событий outbox в БД postgres
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="configure">Конфигурация БД</param>
        /// <typeparam name="TDbContext">Используемый <see cref="DbContext"/></typeparam>
        /// <returns></returns>
        public static IServiceCollection UsePostgresOutboxStorage<TDbContext>(this IServiceCollection services, Action<PostgresWorkingSetOptions> configure)
            where TDbContext : DbContext
        {
            services.Configure<PostgresWorkingSetOptions>(configure);
            services.AddScoped<IPostgresOutboxConnectionProvider, DbContextConnectionProvider<TDbContext>>();
            services.AddScoped<IWorkingSetsProvider, PostgresWorkingSetsProvider>();
            services.AddScoped<IDatabaseUnitOfWork, DbContextAttachedDatabaseUnitOfWork<TDbContext>>();
            services.AddScoped<IOutbox, PostgresOutbox>();

            services.AddSingleton<IEventTargetsProvider, EventTargetsProvider>();
            return services;
        }

        /// <summary>
        /// Добавляет сопоставление типа события и цели его назначения
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="eventType">Тип события</param>
        /// <param name="eventTarget">Цель назначения</param>
        public static IServiceCollection WithEventTargets(this IServiceCollection services, string eventType, string eventTarget)
        {
            return services.WithEventTargets(new[] { eventType }, new[] { eventTarget });
        }

        /// <summary>
        /// Добавляет сопоставление типа события и целей его назначения
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="eventType">Тип события</param>
        /// <param name="eventTargets">Цели назначения</param>
        /// <remarks>Тип события будет сопоставлен всем указанным целям</remarks>
        public static IServiceCollection WithEventTargets(this IServiceCollection services, string eventType, IEnumerable<string> eventTargets)
        {
            return services.WithEventTargets(new[] { eventType }, eventTargets);
        }

        /// <summary>
        /// Добавляет сопоставление типов событий и цели их назначения
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="eventTypes">Типы событий</param>
        /// <param name="eventTarget">Цель назначения</param>
        /// <remarks>Каждый тип события будет сопоставлен указанной цели</remarks>
        public static IServiceCollection WithEventTargets(this IServiceCollection services, IEnumerable<string> eventTypes, string eventTarget)
        {
            return services.WithEventTargets(eventTypes, new[] { eventTarget });
        }

        /// <summary>
        /// Добавляет сопоставление типов событий и целей их назначения
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="eventTypes">Типы событий</param>
        /// <param name="eventTargets">Цели назначения</param>
        /// <remarks>Каждый тип события будет сопоставлен всем указанным целям</remarks>
        public static IServiceCollection WithEventTargets(this IServiceCollection services, IEnumerable<string> eventTypes, IEnumerable<string> eventTargets)
        {
            var targets = eventTargets.ToArray();
            foreach (var eventType in eventTypes)
            {
                var map = new EventTypeToTargetsMap(eventType, targets);
                services.AddSingleton<IEventTypeToTargetsMap>(map);
            }

            return services;
        }
    }
}