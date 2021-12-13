using System;
using System.Data.Common;

namespace DistributedOutbox.Postgres.Tests.Utils
{
    public abstract class ReceiveConnectionDbTransaction : DbTransaction
    {
        /// <inheritdoc />
        protected override DbConnection DbConnection => PublicDbConnection ?? throw new InvalidOperationException($"{nameof(PublicDbConnection)} is not set.");

        /// <summary>
        /// Public Morozov for <see cref="DbConnection"/>
        /// </summary>
        public virtual DbConnection? PublicDbConnection { get; set; }
    }
}