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
        public Karaoke Karaoke => _cache.GetValue(@event, e => new Karaoke(e));
    }
}
