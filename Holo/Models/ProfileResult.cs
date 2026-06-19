// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Subtitle profiling result
/// </summary>
public sealed class ProfileResult
{
    /// <summary>
    /// Frame index
    /// </summary>
    public required IReadOnlyList<int> Frames { get; init; }

    /// <summary>
    /// Time it took to render the subtitles, in milliseconds
    /// </summary>
    public required IReadOnlyList<double> RenderTimeMs { get; init; }

    /// <summary>
    /// Total bitmap size of the subtitles, in kilopixels
    /// </summary>
    public required IReadOnlyList<double> ImageSizeKp { get; init; }

    /// <summary>
    /// Number of subtitle images on the frame
    /// </summary>
    public required IReadOnlyList<int> ImageCount { get; init; }
}
