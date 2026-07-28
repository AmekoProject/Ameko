// SPDX-License-Identifier: MPL-2.0

using Holo.Media;
using Holo.Media.Providers;

namespace TestingUtils;

public unsafe class NullSourceProvider : ISourceProvider
{
    /// <inheritdoc />
    public bool IsInitialized => true;

    /// <inheritdoc />
    public int FrameCount => 0;

    /// <inheritdoc />
    public Rational Sar => new();

    /// <inheritdoc />
    public bool ValidateDependencies()
    {
        return true;
    }

    /// <inheritdoc />
    public int Initialize()
    {
        return 0;
    }

    /// <inheritdoc />
    public int LoadVideo(string filename, ISourceProvider.ProgressCallback? progressCallback = null)
    {
        return 0;
    }

    /// <inheritdoc />
    public int LoadAudio(string filename, int? trackNumber)
    {
        return 0;
    }

    /// <inheritdoc />
    public int LoadKeyframes(string filename)
    {
        return 0;
    }

    /// <inheritdoc />
    public TrackInfo[] GetAudioTrackInfo(string filename)
    {
        return [];
    }

    /// <inheritdoc />
    public int CloseVideo()
    {
        return 0;
    }

    /// <inheritdoc />
    public int CloseAudio()
    {
        return 0;
    }

    /// <inheritdoc />
    public int SetSubtitles(string data, string? codePage)
    {
        return 0;
    }

    /// <inheritdoc />
    public int AllocateBuffers(int numBuffers, int maxCacheSize)
    {
        return 0;
    }

    /// <inheritdoc />
    public int AllocateAudioBuffer()
    {
        return 0;
    }

    /// <inheritdoc />
    public int FreeBuffers()
    {
        return 0;
    }

    /// <inheritdoc />
    public FrameGroup* GetFrame(int frameNumber, long timestamp, bool raw)
    {
        return null;
    }

    /// <inheritdoc />
    public AudioFrame* GetAudio(ISourceProvider.ProgressCallback? progressCallback = null)
    {
        return null;
    }

    /// <inheritdoc />
    public Bitmap* GetVisualization(
        int width,
        int height,
        double pixelsPerMs,
        double amplitudeScale,
        long startTime,
        long videoTime,
        long audioTime,
        long* eventBounds,
        int eventBoundsLength
    )
    {
        return null;
    }

    /// <inheritdoc />
    public int[] GetKeyframes()
    {
        return [];
    }

    /// <inheritdoc />
    public long[] GetTimecodes()
    {
        return [];
    }

    /// <inheritdoc />
    public long[] GetMidcodes()
    {
        return [];
    }

    /// <inheritdoc />
    public long[] GetFrameIntervals()
    {
        return [];
    }

    /// <inheritdoc />
    public int GetChannelCount()
    {
        return 2;
    }

    /// <inheritdoc />
    public int GetSampleRate()
    {
        return 1;
    }

    /// <inheritdoc />
    public long GetSampleCount()
    {
        return 1;
    }

    /// <inheritdoc />
    public ProfilePoint[] ProfileSubtitles(
        int fromFrame,
        int toFrame,
        int width,
        int height,
        ISourceProvider.ProgressCallback? progressCallback
    )
    {
        return [];
    }

    /// <inheritdoc />
    public void CleanCache(uint days) { }
}
