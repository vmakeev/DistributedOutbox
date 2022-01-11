using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace DistributedOutbox.Postgres.Tests.Attributes
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