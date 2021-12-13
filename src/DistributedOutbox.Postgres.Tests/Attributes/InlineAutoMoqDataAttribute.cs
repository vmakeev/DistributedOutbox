using AutoFixture.Xunit2;

namespace DistributedOutbox.Postgres.Tests.Attributes
{
    public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
    {
        /// <inheritdoc />
        public InlineAutoMoqDataAttribute(params object?[] objects)
            : base(new AutoMoqDataAttribute(), objects)
        {
        }
    }
}