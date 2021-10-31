using System.Collections.Generic;

namespace DistributedOutbox.Postgres
{
    internal record EventTypeToTargetsMap(string EventType, IEnumerable<string> Targets)
        : IEventTypeToTargetsMap;
}