using System.Collections.Generic;
using System.Linq;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    internal sealed class EventTargetsProvider : IEventTargetsProvider
    {
        private readonly Dictionary<string, List<string>> _eventTargetsMaps = new();

        public EventTargetsProvider(IEnumerable<IEventTypeToTargetsMap> maps)
        {
            var tempMaps = new Dictionary<string, IEnumerable<string>>();

            foreach (var map in maps)
            {
                if (tempMaps.ContainsKey(map.EventType))
                {
                    tempMaps[map.EventType] = tempMaps[map.EventType].Concat(map.Targets);
                }
                else
                {
                    tempMaps[map.EventType] = map.Targets;
                }
            }

            foreach (var key in tempMaps.Keys)
            {
                _eventTargetsMaps[key] = tempMaps[key].Distinct().ToList();
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetTargets(string eventType)
        {
            if (!_eventTargetsMaps.TryGetValue(eventType, out var targets))
            {
                return Enumerable.Empty<string>();
            }

            return targets;
        }
    }
}