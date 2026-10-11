// SPDX-License-Identifier: MPL-2.0

using Holo.Models;

namespace Holo.Configuration;

/// <summary>
/// Color configuration
/// </summary>
public class ColorConfiguration
{
    /// <summary>
    /// Standard event color
    /// </summary>
    public HoloColor Default { get; set; } = new(0, 0, 0, 0);

    /// <summary>
    /// Indicates events that are selected
    /// </summary>
    public HoloColor Selection { get; set; } = new(30, 144, 255, 64);

    /// <summary>
    /// Indicates comment events
    /// </summary>
    public HoloColor Comment { get; set; } = new(128, 128, 128, 128);

    /// <summary>
    /// Indicates comment events that are selected
    /// </summary>
    public HoloColor CommentSelection { get; set; } = new(112, 128, 144, 128);

    /// <summary>
    /// Indicates events that are part of the current video frame
    /// </summary>
    public HoloColor InFrame { get; set; } = new(60, 179, 113, 64);

    /// <summary>
    /// Indicates events that collide with the active event
    /// </summary>
    public HoloColor Collision { get; set; } = new(255, 165, 0, 64);

    /// <summary>
    /// Indicates events that collide with the active event
    /// and are part of the current video frame
    /// </summary>
    public HoloColor CollisionInFrame { get; set; } = new(186, 85, 211, 64);
}
