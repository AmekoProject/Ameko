// SPDX-License-Identifier: MPL-2.0

using AssCS;

namespace Holo.Media;

/// <summary>
/// Information about a video source
/// </summary>
/// <param name="path">File path</param>
/// <param name="frameCount">Number of frames in the source</param>
/// <param name="sar">Screen aspect ratio</param>
/// <param name="frameTimes">List of frame times, in milliseconds</param>
/// <param name="frameIntervals">List of frame intervals, in milliseconds</param>
/// <param name="keyframes">List of keyframe indexes</param>
/// <param name="width">Width of the video, in pixels</param>
/// <param name="height">Height of the video, in pixels</param>
public class VideoInfo(
    string path,
    int frameCount,
    Rational sar,
    long[] frameTimes,
    long[] frameIntervals,
    int[] keyframes,
    int width,
    int height
)
{
    /// <summary>
    /// File path
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Number of frames in the source
    /// </summary>
    public int FrameCount { get; } = frameCount;

    /// <summary>
    /// Screen aspect ratio
    /// </summary>
    public Rational Sar { get; init; } = sar;

    /// <summary>
    /// List of frame times, in milliseconds
    /// </summary>
    public long[] FrameTimes { get; } = frameTimes;

    /// <summary>
    /// List of frame intervals, in milliseconds
    /// </summary>
    public long[] FrameIntervals { get; } = frameIntervals;

    /// <summary>
    /// List of keyframe indexes
    /// </summary>
    public int[] Keyframes { get; internal set; } = keyframes;

    /// <summary>
    /// Width of the video, in pixels
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// Height of the video, in pixels
    /// </summary>
    public int Height { get; } = height;

    /// <summary>
    /// Path to an external keyframes file
    /// </summary>
    public string? KeyframesFile { get; internal set; }

    /// <summary>
    /// Gets the index of the frame with the largest timestamp ≤ <paramref name="millis"/>.
    /// </summary>
    /// <remarks>
    /// If the time is out of bounds, returns the closest frame (0 or <see cref="FrameCount"/>)
    /// </remarks>
    /// <param name="millis">Time in milliseconds</param>
    /// <returns>Frame number</returns>
    public int FrameFromMillis(int millis)
    {
        if (millis < FrameTimes[0])
            return 0;
        if (millis > FrameTimes[^1])
            return FrameTimes.Length - 1;

        var bs = Array.BinarySearch(FrameTimes, millis);

        if (bs >= 0)
            return bs;

        // ~bs → Index of the first greater element.
        return ~bs;
    }

    /// <summary>
    /// Gets the time in milliseconds of the provided <paramref name="frameNumber"/>
    /// </summary>
    /// <remarks>
    /// If the frame number is out of bounds, returns the closest time
    /// </remarks>
    /// <param name="frameNumber">Frame number</param>
    /// <returns>Time in milliseconds</returns>
    public long MillisecondsFromFrame(int frameNumber)
    {
        return FrameTimes[Math.Clamp(frameNumber, 0, FrameTimes.Length - 1)];
    }

    /// <summary>
    /// Gets the index of the frame with the largest timestamp ≤ <paramref name="time"/>.
    /// </summary>
    /// <remarks>
    /// If the time is out of bounds, returns the closest frame (0 or <see cref="FrameCount"/>)
    /// </remarks>
    /// <param name="time">Time</param>
    /// <returns>Frame number</returns>
    public int FrameFromTime(Time time)
    {
        return FrameFromMillis((int)time.TotalMilliseconds);
    }

    /// <summary>
    /// Gets the time of the provided <paramref name="frameNumber"/>
    /// </summary>
    /// <remarks>
    /// If the frame number is out of bounds, returns the closest time
    /// </remarks>
    /// <param name="frameNumber">Frame number</param>
    /// <returns>Time</returns>
    public Time TimeFromFrame(int frameNumber)
    {
        return Time.FromMillis(MillisecondsFromFrame(frameNumber));
    }

    /// <summary>
    /// Get the midpoint between <paramref name="frame1"/> and <paramref name="frame2"/> in milliseconds
    /// </summary>
    /// <param name="frame1">First frame</param>
    /// <param name="frame2">Second frame</param>
    /// <returns>Time in milliseconds</returns>
    public long MillisecondsFromMidpoint(int frame1, int frame2)
    {
        if (frame1 > frame2)
            (frame1, frame2) = (frame2, frame1);

        var ms1 = MillisecondsFromFrame(frame1);
        var ms2 = MillisecondsFromFrame(frame2);

        // Real midpoint
        if (ms1 != ms2)
        {
            return (ms2 - ms1) / 2 + ms1;
        }

        // Zero
        if (frame1 < 0)
            return 0;

        // Max
        if (frame2 >= FrameCount)
        {
            // Use the midpoint delta between frame1 and the previous frame
            // to calculate the midpoint of frame2 and the end of the video
            var ms3 = MillisecondsFromFrame(frame1 - 1);
            return (ms1 - ms3) / 2 + ms2;
        }

        // Not sure what's happening here
        return ms1;
    }

    /// <summary>
    /// Get the midpoint between <paramref name="frame1"/> and <paramref name="frame2"/>
    /// </summary>
    /// <param name="frame1">First frame</param>
    /// <param name="frame2">Second frame</param>
    /// <returns>Time</returns>
    public Time TimeFromMidpoint(int frame1, int frame2)
    {
        return Time.FromMillis(MillisecondsFromMidpoint(frame1, frame2));
    }
}
