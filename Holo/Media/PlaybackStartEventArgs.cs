// SPDX-License-Identifier: MPL-2.0

using Holo.Models;

namespace Holo.Media;

/// <summary>
/// EventArgs for the playback start event
/// </summary>
/// <param name="target">Playback target</param>
/// <param name="start">Start time to play from</param>
/// <param name="goal">Goal time to play to</param>
public class PlaybackStartEventArgs(PlaybackTarget target, long start, long goal) : EventArgs
{
    /// <summary>
    /// Playback target
    /// </summary>
    public PlaybackTarget Target { get; } = target;

    /// <summary>
    /// Start time to play from
    /// </summary>
    public long StartTime { get; } = start;

    /// <summary>
    /// Goal time to play to
    /// </summary>
    public long GoalTime { get; } = goal;
}
