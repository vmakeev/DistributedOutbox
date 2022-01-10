using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedOutbox.Postgres.EfCore
{
    /// <summary>
    /// Расширения для хранения событий outbox в БД postgres с использованием EF Core
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет возможность хранения событий outbox в БД postgres с поддержкой Entity Framework
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="configure">Конфигурация БД</param>
        /// <typeparam name="TDbContext">Используемый <see cref="DbContext"/></typeparam>
        /// <returns></returns>
        public static IServiceCollection UsePostgresOutboxStorage<TDbContext>(this IServiceCollection services, Action<PostgresWorkingSetOptions> configure)
            where TDbContext : DbContext
        {
            return services.UsePostgresOutboxStorage<DbContextConnectionProvider<TDbContext>, DbContextAttachedDatabaseUnitOfWork<TDbContext>>(configure);
        }
    }
}