// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Holo.Media.Providers;

/// <summary>
/// A media source provider
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Whether this provider is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// The number of frames in the loaded video
    /// </summary>
    int FrameCount { get; }

    /// <summary>
    /// Validate that native dependencies are available for use
    /// </summary>
    /// <returns><see langword="true"/> if dependencies are available</returns>
    bool ValidateDependencies();

    /// <summary>
    /// Initialize the provider
    /// </summary>
    /// <returns>0 on success</returns>
    int Initialize();

    /// <summary>
    /// Load a video
    /// </summary>
    /// <param name="filename">Path to the video file to load</param>
    /// <param name="progressCallback">Indexing progress callback</param>
    /// <returns>0 on success</returns>
    int LoadVideo(string filename, ProgressCallback? progressCallback = null);

    /// <summary>
    /// Load a video
    /// </summary>
    /// <param name="filename">Path to the audio file to load</param>
    /// <param name="trackNumber">Track number to load</param>
    /// <returns>0 on success</returns>
    int LoadAudio(string filename, int? trackNumber);

    /// <summary>
    /// Load a keyframes file
    /// </summary>
    /// <param name="filename">Path to the keyframes file to load</param>
    /// <returns>0 on success</returns>
    int LoadKeyframes(string filename);

    /// <summary>
    /// Get information about the audio tracks in a file
    /// </summary>
    /// <param name="filename">Path to the audio file to load</param>
    /// <returns>Array of track information</returns>
    TrackInfo[] GetAudioTrackInfo(string filename);

    /// <summary>
    /// Close the open video (includes audio)
    /// </summary>
    /// <returns>0 on success</returns>
    int CloseVideo();

    /// <summary>
    /// Close the open audio
    /// </summary>
    /// <returns>0 on success</returns>
    int CloseAudio();

    /// <summary>
    /// Set the subtitles to be parsed
    /// </summary>
    /// <param name="data">Subtitle data</param>
    /// <param name="codePage">Codepage to use</param>
    /// <returns>0 on success</returns>
    int SetSubtitles(string data, string? codePage);

    /// <summary>
    /// Allocate frame buffers
    /// </summary>
    /// <param name="numBuffers">Number of buffers to pre-allocate</param>
    /// <param name="maxCacheSize">Maximum cache size in megabytes</param>
    /// <returns>0 on success</returns>
    int AllocateBuffers(int numBuffers, int maxCacheSize);

    /// <summary>
    /// Allocate audio buffer
    /// </summary>
    /// <returns>0 on success</returns>
    int AllocateAudioBuffer();

    /// <summary>
    /// Free frame buffers
    /// </summary>
    /// <returns>0 on success</returns>
    int FreeBuffers();

    /// <summary>
    /// Get a frame
    /// </summary>
    /// <param name="frameNumber">Frame number to get</param>
    /// <param name="timestamp">Timestamp to get</param>
    /// <param name="raw">Just the video</param>
    /// <returns>Output frame</returns>
    unsafe FrameGroup* GetFrame(int frameNumber, long timestamp, bool raw);

    /// <summary>
    /// Get the audio frame buffer
    /// </summary>
    /// <param name="progressCallback">Indexing progress callback</param>
    /// <returns>Audio frame</returns>
    unsafe AudioFrame* GetAudio(ProgressCallback? progressCallback = null);

    /// <summary>
    /// Get a waveform bitmap for the specified time
    /// </summary>
    /// <param name="width">Width of the generated bitmap</param>
    /// <param name="height">Height of the generated bitmap</param>
    /// <param name="pixelsPerMs">Pixels per millisecond - Horizontal scale</param>
    /// <param name="amplitudeScale">Amplitude scale factor - Vertical scale</param>
    /// <param name="startTime">Time to start at</param>
    /// <param name="videoTime">Timestamp of the current video frame</param>
    /// <param name="audioTime">Timestamp of the current audio frame</param>
    /// <param name="eventBounds">Array of bounds of events to display, in milliseconds</param>
    /// <param name="eventBoundsLength">Length of the <paramref name="eventBounds"> array</paramref></param>
    /// <returns>Output bitmap</returns>
    unsafe Bitmap* GetVisualization(
        int width,
        int height,
        double pixelsPerMs,
        double amplitudeScale,
        long startTime,
        long videoTime,
        long audioTime,
        long* eventBounds,
        int eventBoundsLength
    );

    /// <summary>
    /// List of keyframes
    /// </summary>
    /// <returns></returns>
    int[] GetKeyframes();

    /// <summary>
    /// Timecode of each frame
    /// </summary>
    /// <returns></returns>
    long[] GetTimecodes();

    /// <summary>
    /// Time of the middle of each frame
    /// </summary>
    /// <returns></returns>
    long[] GetMidcodes();

    /// <summary>
    /// How long each frame is displayed for
    /// </summary>
    /// <returns></returns>
    long[] GetFrameIntervals();

    /// <summary>
    /// How many channels are in the audio
    /// </summary>
    /// <returns></returns>
    int GetChannelCount();

    /// <summary>
    /// Sample rate of the audio
    /// </summary>
    /// <returns></returns>
    int GetSampleRate();

    /// <summary>
    /// How many samples are in the audio
    /// </summary>
    /// <returns></returns>
    long GetSampleCount();

    /// <summary>
    /// Preload the fonts in a directory
    /// </summary>
    /// <param name="dirName">Path to the directory to load</param>
    /// <returns>0 on success</returns>
    int PreloadFontDirectory(string dirName);

    /// <summary>
    /// Profile the subtitles
    /// </summary>
    /// <param name="fromFrame">Frame to start profiling at</param>
    /// <param name="toFrame">Frame to end profiling at</param>
    /// <param name="width">Width of the rendering frame</param>
    /// <param name="height">Height of the rendering frame</param>
    /// <param name="progressCallback">Profiling progress callback</param>
    /// <returns>Array of profile points</returns>
    ProfilePoint[] ProfileSubtitles(
        int fromFrame,
        int toFrame,
        int width,
        int height,
        ProgressCallback? progressCallback = null
    );

    /// <summary>
    /// Clear the cache
    /// </summary>
    /// <param name="days">Expiration time</param>
    void CleanCache(uint days);

    /// <summary>
    /// Generic progress callback
    /// </summary>
    /// <param name="current">Current progress</param>
    /// <param name="total">Goal progress</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void ProgressCallback(long current, long total);
}
