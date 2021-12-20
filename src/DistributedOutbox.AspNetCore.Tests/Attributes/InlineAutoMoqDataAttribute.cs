using AutoFixture.Xunit2;

namespace DistributedOutbox.AspNetCore.Tests.Attributes
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