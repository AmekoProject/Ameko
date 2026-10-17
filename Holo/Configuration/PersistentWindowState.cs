// SPDX-License-Identifier: MPL-2.0

namespace Holo.Configuration;

/// <summary>
/// Window State options
/// </summary>
public enum PersistentWindowState
{
    /// <summary>The window is neither minimized nor maximized.</summary>
    Normal,

    /// <summary>The window is minimized.</summary>
    Minimized,

    /// <summary>The window is maximized.</summary>
    Maximized,

    /// <summary>The window is fullscreen.</summary>
    FullScreen,
}
