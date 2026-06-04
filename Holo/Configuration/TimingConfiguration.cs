// SPDX-License-Identifier: MPL-2.0

using AssCS;

namespace Holo.Configuration;

/// <summary>
/// Defines thresholds for timing actions
/// </summary>
public class TimingConfiguration : BindableBase
{
    /// <summary>
    /// Number of milliseconds to add as lead-in
    /// </summary>
    public uint LeadIn { get; set; }

    /// <summary>
    /// Number of milliseconds to add as lead-out
    /// </summary>
    public uint LeadOut { get; set; }

    /// <summary>
    /// Threshold to snap the start of an event to an earlier goal, in milliseconds
    /// </summary>
    public uint SnapStartEarlierThreshold { get; set; }

    /// <summary>
    /// Threshold to snap the start of an event to a later goal, in milliseconds
    /// </summary>
    public uint SnapStartLaterThreshold { get; set; }

    /// <summary>
    /// Threshold to snap the end of an event to an earlier goal, in milliseconds
    /// </summary>
    public uint SnapEndEarlierThreshold { get; set; }

    /// <summary>
    /// Threshold to snap the end of an event to a later goal, in milliseconds
    /// </summary>
    public uint SnapEndLaterThreshold { get; set; }
}
