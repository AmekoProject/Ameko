// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;

namespace AssCS.Overrides;

/// <summary>
/// Extensions on <see cref="Event"/>
/// </summary>
public static class KaraokeExtensions
{
    private static readonly ConditionalWeakTable<Event, Karaoke> _cache = new();

    extension(Event @event)
    {
        /// <summary>
        /// Karaoke API
        /// </summary>
        public Karaoke Karaoke
        {
            get
            {
                if (_cache.TryGetValue(@event, out var karaoke))
                    return karaoke;

                karaoke = new Karaoke(@event);
                _cache.Add(@event, karaoke);
                return karaoke;
            }
        }
    }
}
