// SPDX-License-Identifier: MPL-2.0

namespace Holo.Scripting;

/// <summary>
/// Specifies the behavior of the <see cref="HoloScript"/> log window
/// </summary>
public enum LogDisplay
{
    /// <summary>
    /// The log window is not displayed unless there is an error during execution
    /// </summary>
    OnError,

    /// <summary>
    /// The log window is displayed following script execution,
    /// regardless of whether there was an error during execution
    /// </summary>
    Forced,
}
