using System.Collections.Generic;
using System.Linq;

namespace DistributedOutbox.Postgres
{
    /// <inheritdoc />
    internal sealed class EventTargetsProvider : IEventTargetsProvider
    {
        private readonly Dictionary<string, List<string>> _dictionary = new();

        public EventTargetsProvider(IEnumerable<IEventTypeToTargetsMap> maps)
        {
            foreach (var map in maps)
            {
                if (_dictionary.ContainsKey(map.EventType))
                {
                    _dictionary[map.EventType] = _dictionary[map.EventType]
                                                 .Concat(map.Targets)
                                                 .Distinct()
                                                 .ToList();
                }
                else
                {
                    _dictionary[map.EventType] = map.Targets.ToList();
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetTargets(string eventType)
        {
            if (!_dictionary.TryGetValue(eventType, out var targets))
            {
                return Enumerable.Empty<string>();
            }

            return targets;
        }
    }
}