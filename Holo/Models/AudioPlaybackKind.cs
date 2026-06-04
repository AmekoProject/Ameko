// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Type of audio playback to perform
/// </summary>
public enum AudioPlaybackKind
{
    /// <summary>
    /// Play the duration of the event
    /// </summary>
    Event,

    /// <summary>
    /// Play 500ms before the event
    /// </summary>
    Before,

    /// <summary>
    /// Play first 500ms of the event
    /// </summary>
    First,

    /// <summary>
    /// Play last 500ms of the event
    /// </summary>
    Last,

    /// <summary>
    /// Play 500ms after the event
    /// </summary>
    After,

    /// <summary>
    /// Play 500ms on either side of the event
    /// </summary>
    Surround,
}
