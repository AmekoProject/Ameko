// SPDX-License-Identifier: MPL-2.0

namespace Holo.Media;

/// <summary>
/// Information about an audio source
/// </summary>
/// <param name="path">Filepath to the audio source</param>
/// <param name="trackCount">Number of tracks</param>
/// <param name="channelCount">Number of channels</param>
/// <param name="sampleRate">Sample rate</param>
/// <param name="sampleCount">Total number of samples</param>
public class AudioInfo(
    string path,
    int trackCount,
    int channelCount,
    int sampleRate,
    long sampleCount
)
{
    /// <summary>
    /// Filepath to the audio source
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Number of tracks
    /// </summary>
    public int TrackCount { get; } = trackCount;

    /// <summary>
    /// Number of channels
    /// </summary>
    public int ChannelCount { get; } = channelCount;

    /// <summary>
    /// Sample rate
    /// </summary>
    public int SampleRate { get; } = sampleRate;

    /// <summary>
    /// Total number of samples
    /// </summary>
    public long SampleCount { get; } = sampleCount;

    /// <summary>
    /// Duration of the audio, derived from the <see cref="SampleCount"/> and <see cref="SampleRate"/>
    /// </summary>
    public long Duration => (long)(1000.0 * SampleCount / SampleRate);
}
