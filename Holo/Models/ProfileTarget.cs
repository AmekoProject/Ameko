// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// What to include in profiling
/// </summary>
public enum ProfileTarget
{
    /// <summary>
    /// Profile all events
    /// </summary>
    All,

    /// <summary>
    /// Profile all events between the selected times
    /// </summary>
    SelectedTimes,

    /// <summary>
    /// Profile only the selected events
    /// </summary>
    SelectedEvents,
}
