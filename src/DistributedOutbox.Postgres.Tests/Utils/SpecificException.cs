using System;

namespace DistributedOutbox.Postgres.Tests.Utils
{
    public class SpecificException : Exception
    {
        /// <inheritdoc />
        public SpecificException()
        {
        }

        /// <inheritdoc />
        public SpecificException(string? message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public SpecificException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}