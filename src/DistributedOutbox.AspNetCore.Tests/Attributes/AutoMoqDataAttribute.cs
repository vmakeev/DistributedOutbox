using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace DistributedOutbox.AspNetCore.Tests.Attributes
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        /// <inheritdoc />
        public AutoMoqDataAttribute()
            : base(() => new Fixture().Customize(new AutoMoqCustomization()))
        {
        }
    }
}