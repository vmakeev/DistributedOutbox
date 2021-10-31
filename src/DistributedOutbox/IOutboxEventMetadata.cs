using System.Collections.Generic;

namespace DistributedOutbox
{
    /// <summary>
    /// Метаданные события
    /// </summary>
    public interface IOutboxEventMetadata : IDictionary<string, object?>
    {
    }
}