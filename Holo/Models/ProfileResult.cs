// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Subtitle profiling result
/// </summary>
public struct ProfileResult
{
    /// <summary>
    /// Frame index
    /// </summary>
    public required double[] Frames { get; init; }

    /// <summary>
    /// Time it took to render the subtitles, in milliseconds
    /// </summary>
    public required double[] RenderTimeMs { get; init; }

    /// <summary>
    /// Total bitmap size of the subtitles, in kilopixels
    /// </summary>
    public required double[] ImageSizeKp { get; init; }

    /// <summary>
    /// Number of subtitle images on the frame
    /// </summary>
    public required double[] ImageCount { get; init; }
}
