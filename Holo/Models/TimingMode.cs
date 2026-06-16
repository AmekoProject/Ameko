// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Timing behavior
/// </summary>
public enum TimingMode
{
    /// <summary>
    /// Times are snapped to the midpoint of the nearest frame by default
    /// </summary>
    SnapToFrame = 0,

    /// <summary>
    /// Times are placed at the nearest centisecond by default
    /// </summary>
    FreePlacement = 1,
}
