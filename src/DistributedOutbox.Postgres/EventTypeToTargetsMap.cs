using System.Collections.Generic;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc cref="IEventTypeToTargetsMap" />
    internal record EventTypeToTargetsMap(string EventType, IEnumerable<string> Targets)
        : IEventTypeToTargetsMap;
}