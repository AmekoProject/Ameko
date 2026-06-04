// SPDX-License-Identifier: MPL-2.0

namespace Holo.Configuration;

/// <summary>
/// What to include in Rich Presence, if anything
/// </summary>
public enum RichPresenceLevel
{
    /// <summary>
    /// Rich presence is disabled
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Rich presence is enabled,
    /// displaying the current project and file
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// Rich presence is enabled,
    /// file information is hidden
    /// </summary>
    TimeOnly = 2,
}
