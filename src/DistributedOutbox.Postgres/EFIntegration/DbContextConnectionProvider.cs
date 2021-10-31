using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DistributedOutbox.Postgres.EFIntegration
{
    /// <summary>
    /// Поставщик соединений с БД, получающий строку подключения из <typeparamref name="TDbContext"/>
    /// </summary>
    /// <typeparam name="TDbContext">Используемый <see cref="DbContext"/></typeparam>
    internal class DbContextConnectionProvider<TDbContext> : IPostgresOutboxConnectionProvider
        where TDbContext : DbContext
    {
        private readonly TDbContext _innerContext;

        public DbContextConnectionProvider(TDbContext innerContext)
        {
            _innerContext = innerContext;
        }

        /// <inheritdoc />
        public async Task<DbConnection> GetDbConnectionAsync(CancellationToken cancellationToken)
        {
            var connectionString = _innerContext.Database.GetConnectionString();

            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            return connection;
        }
    }
}