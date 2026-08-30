// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using AssCS;
using AssCS.IO;
using Holo.Configuration;
using Holo.Media;
using Holo.Media.Providers;
using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo;

/// <summary>
/// Encapsulates media and playback control
/// </summary>
public class MediaController : BindableBase
{
    private static readonly AssWriter _assWriter = new(new ConsumerInfo("", "", ""));

    private readonly ISourceProvider _provider;
    private readonly ILogger _logger;
    private readonly IPersistence _persistence;
    private readonly HighResolutionTimer _videoPlayback;
    private readonly HighResolutionTimer _audioPlayback;

    private readonly Lock _requestLock = new();
    private readonly Lock _frameLock = new();
    private readonly Lock _boundsLock = new();

    private unsafe FrameGroup* _lastFrame;
    private unsafe FrameGroup* _nextFrame;
    private unsafe AudioFrame* _audioFrame;
    private unsafe Bitmap* _lastVizFrame;
    private unsafe Bitmap* _nextVizFrame;
    private int _currentVideoFrame;
    private int _currentAudioFrame = -1;

    private Task? _fetchTask;
    private int _pendingFrame = -1;
    private bool _subtitlesChanged;

    private ScaleFactor _scaleFactor = ScaleFactor.Default;
    private double _screenScaleFactor = 1.0d;

    private bool _isVideoPlaying;
    private bool _isAudioPlaying;
    private bool _isPaused;

    private int _destinationFrame;

    private long[] _eventBounds = [];
    private int _activeEventIndex;

    /// <summary>
    /// If a video or audio file is currently being loaded
    /// </summary>
    public bool IsLoadingFile { get; private set; }

    /// <summary>
    /// If modifications are disabled
    /// </summary>
    public bool IsLocked
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// If media operations are to be enabled
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Information about the loaded video
    /// </summary>
    public VideoInfo? VideoInfo
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Information about the loaded audio
    /// </summary>
    public AudioInfo? AudioInfo
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// If a video source is currently loaded
    /// </summary>
    [MemberNotNullWhen(true, nameof(VideoInfo))]
    public bool IsVideoLoaded
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// If an audio source is currently loaded
    /// </summary>
    [MemberNotNullWhen(true, nameof(AudioInfo))]
    public bool IsAudioLoaded
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Scale factor of the viewport
    /// </summary>
    public ScaleFactor ScaleFactor
    {
        get => _scaleFactor;
        set
        {
            SetProperty(ref _scaleFactor, value);
            DisplayWidth = (VideoInfo?.Width ?? 1) * _scaleFactor.Multiplier / _screenScaleFactor;
            DisplayHeight = (VideoInfo?.Height ?? 1) * _scaleFactor.Multiplier / _screenScaleFactor;
            FrameReady?.Invoke();

            if (VideoInfo is not null)
                _persistence.SetScaleForRes(VideoInfo.Height, value);
        }
    }

    /// <summary>
    /// Rotational factor of the viewport
    /// </summary>
    public RotationalFactor RotationalFactor
    {
        get;
        set
        {
            SetProperty(ref field, value);
            DisplayAngle = value.Angle;
        }
    } = RotationalFactor.Default;

    /// <summary>
    /// Screen scale factor (125% scale = 1.25)
    /// </summary>
    public double ScreenScaleFactor
    {
        get => _screenScaleFactor;
        set
        {
            SetProperty(ref _screenScaleFactor, value);
            DisplayWidth = (VideoInfo?.Width ?? 1) * _scaleFactor.Multiplier / _screenScaleFactor;
            DisplayHeight = (VideoInfo?.Height ?? 1) * _scaleFactor.Multiplier / _screenScaleFactor;
        }
    }

    /// <summary>
    /// Width of the viewport
    /// </summary>
    public double DisplayWidth
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Height of the viewport
    /// </summary>
    public double DisplayHeight
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Angle of the viewport
    /// </summary>
    public double DisplayAngle
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Width of the audio visualizer
    /// </summary>
    public int VisualizerWidth
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (IsVideoLoaded)
                RequestFrame(CurrentFrame);
        }
    }

    /// <summary>
    /// Height of the audio visualizer
    /// </summary>
    public int VisualizerHeight
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (IsVideoLoaded)
                RequestFrame(CurrentFrame);
        }
    }

    /// <summary>
    /// Horizontal scale of the audio visualizer
    /// </summary>
    public double VisualizerScaleX
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _persistence.VisualizationScaleX = value;

            if (IsVideoLoaded)
                RequestFrame(CurrentFrame);
        }
    }

    /// <summary>
    /// Vertical scale of the audio visualizer
    /// </summary>
    public double VisualizerScaleY
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _persistence.VisualizationScaleY = value;

            if (IsVideoLoaded)
                RequestFrame(CurrentFrame);
        }
    }

    /// <summary>
    /// Current position of the left-hand edge of the audio visualizer, in milliseconds
    /// </summary>
    public long VisualizerPositionMs
    {
        get;
        set
        {
            if (value <= 0)
                value = 0;
            if (value >= (AudioInfo?.Duration ?? 0))
                value = AudioInfo?.Duration ?? 0;

            SetProperty(ref field, value);
            if (IsVideoLoaded)
                RequestFrame(CurrentFrame);
        }
    } = 0;

    /// <summary>
    /// If we should automatically seek to the start of an event when the selection changes
    /// </summary>
    public bool IsAutoSeekEnabled
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    /// <summary>
    /// If audio playback is muted
    /// </summary>
    public bool IsMuted
    {
        get;
        set
        {
            if (_isVideoPlaying || _isAudioPlaying)
                Pause();
            SetProperty(ref field, value);
            if (_isPaused)
                Resume();
        }
    } = false;

    /// <summary>
    /// The current frame
    /// </summary>
    public int CurrentFrame
    {
        get => _currentVideoFrame;
        set
        {
            SetProperty(ref _currentVideoFrame, value);
            RaisePropertyChanged(nameof(CurrentTime));
            RequestFrame(value);
        }
    }

    /// <summary>
    /// The current frame time
    /// </summary>
    public Time? CurrentTime => VideoInfo?.TimeFromFrame(CurrentFrame);

    /// <summary>
    /// If video is currently playing
    /// </summary>
    public bool IsVideoPlaying
    {
        get => _isVideoPlaying;
        private set => SetProperty(ref _isVideoPlaying, value);
    }

    /// <summary>
    /// If audio is currently playing
    /// </summary>
    public bool IsAudioPlaying
    {
        get => _isAudioPlaying;
        set => SetProperty(ref _isAudioPlaying, value);
    }

    /// <summary>
    /// If only audio is playing
    /// </summary>
    public bool IsOnlyAudioPlaying => !_isVideoPlaying && _isAudioPlaying;

    /// <summary>
    /// If playback is paused
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        private set => SetProperty(ref _isPaused, value);
    }

    /// <summary>
    /// Stop playback
    /// </summary>
    public void Stop()
    {
        if (IsLocked)
            return;

        _logger.LogDebug("Stopping playback");
        if (!IsVideoPlaying && !IsAudioPlaying)
            return;
        IsVideoPlaying = false;
        IsAudioPlaying = false;
        IsPaused = false;
        _videoPlayback.Stop();
        _audioPlayback.Stop();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);

        // Request a re-render of the last frame to ensure GlFinish is executed
        FrameReady?.Invoke();
    }

    /// <summary>
    /// Pause playback
    /// </summary>
    public void Pause()
    {
        _logger.LogDebug("Pausing playback");
        if (!IsVideoPlaying && !IsAudioPlaying)
            return;
        IsPaused = true;
        IsVideoPlaying = false;
        IsAudioPlaying = false;
        _videoPlayback.Stop();
        _audioPlayback.Stop();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);

        // Request a re-render of the last frame to ensure GlFinish is executed
        FrameReady?.Invoke();
    }

    /// <summary>
    /// Play from the current position to the end of the video
    /// </summary>
    public void PlayToEnd()
    {
        if (!IsVideoLoaded)
            throw new InvalidOperationException("Video is not loaded");
        if (IsLocked)
            return;

        Stop();
        _logger.LogDebug("Playing to end");
        _destinationFrame = VideoInfo.FrameCount - 1;
        _videoPlayback.IntervalIndex = _currentVideoFrame;
        _currentAudioFrame = -1; // Hide

        var e = new PlaybackStartEventArgs(
            PlaybackTarget.Video,
            VideoInfo.MillisecondsFromFrame(_currentVideoFrame),
            VideoInfo.MillisecondsFromFrame(_destinationFrame)
        );
        PlaybackStarted?.Invoke(this, e);

        _videoPlayback.Start();
        IsVideoPlaying = true;
        IsAudioPlaying = true;
        IsPaused = false;
    }

    /// <summary>
    /// Play a selection
    /// </summary>
    /// <param name="selection"></param>
    public void PlaySelection(IList<Event> selection)
    {
        if (!IsVideoLoaded)
            throw new InvalidOperationException("Video is not loaded");
        if (IsLocked)
            return;

        Stop();
        var startTime = selection.Min(e => e.Start);
        var endTime = selection.Max(e => e.End);
        _logger.LogDebug("Playing selection [{StartTime}, {EndTime}]", startTime, endTime);

        if (startTime is null || endTime is null)
            return;

        var startFrame = VideoInfo.FrameFromTime(startTime);
        var endFrame = VideoInfo.FrameFromTime(endTime) - 1; // Stop on the last frame including the selection

        CurrentFrame = startFrame;
        _destinationFrame = endFrame;
        _videoPlayback.IntervalIndex = _currentVideoFrame;
        _currentAudioFrame = -1; // Hide

        var e = new PlaybackStartEventArgs(
            PlaybackTarget.Video,
            VideoInfo.MillisecondsFromFrame(_currentVideoFrame),
            VideoInfo.MillisecondsFromFrame(_destinationFrame)
        );
        PlaybackStarted?.Invoke(this, e);

        _videoPlayback.Start();
        IsVideoPlaying = true;
        IsAudioPlaying = true;
        IsPaused = false;
    }

    /// <summary>
    /// Resume playback
    /// </summary>
    public void Resume()
    {
        if (!IsVideoLoaded)
            throw new InvalidOperationException("Video is not loaded");

        _logger.LogDebug("Resuming playback");
        _videoPlayback.IntervalIndex = _currentVideoFrame;
        _currentAudioFrame = -1; // Hide

        PlaybackStarted?.Invoke(
            this,
            new PlaybackStartEventArgs(
                PlaybackTarget.Video,
                VideoInfo.MillisecondsFromFrame(_currentVideoFrame),
                VideoInfo.MillisecondsFromFrame(_destinationFrame)
            )
        );

        _videoPlayback.Start();
        IsVideoPlaying = true;
        IsAudioPlaying = true;
        IsPaused = false;
    }

    /// <summary>
    /// Seek to a frame by number
    /// </summary>
    /// <param name="frameNumber">Frame number to seek to</param>
    public void SeekTo(int frameNumber)
    {
        if (!IsVideoLoaded)
            return;
        if (_isVideoPlaying || _isAudioPlaying)
            Pause();
        CurrentFrame = Math.Clamp(frameNumber, 0, VideoInfo.FrameCount - 1);
        if (_isPaused)
            Resume();
    }

    /// <summary>
    /// Seek to a frame by time
    /// </summary>
    /// <param name="time">Time to seek to</param>
    public void SeekTo(Time time)
    {
        if (!IsVideoLoaded)
            return;
        if (_isVideoPlaying || _isAudioPlaying)
            Pause();
        CurrentFrame = VideoInfo.FrameFromTime(time);
        if (_isPaused)
            Resume();
    }

    /// <summary>
    /// Seek to the first frame of an event
    /// </summary>
    /// <param name="event">Event to seek to the start of</param>
    public void SeekTo(Event @event)
    {
        if (!IsVideoLoaded)
            return;
        if (_isVideoPlaying || _isAudioPlaying)
            Pause();
        CurrentFrame = VideoInfo.FrameFromTime(@event.Start);
        VisualizerPositionMs = @event.Start.TotalMilliseconds;
        if (_isPaused)
            Resume();
    }

    /// <summary>
    /// Seek to the last frame of an event
    /// </summary>
    /// <param name="event">Event to seek to the end of</param>
    public void SeekToEnd(Event @event)
    {
        if (!IsVideoLoaded)
            return;
        if (_isVideoPlaying || _isAudioPlaying)
            Pause();
        CurrentFrame = VideoInfo.FrameFromTime(@event.End) - 1;
        if (_isPaused)
            Resume();
    }

    /// <summary>
    /// Seek to a frame by event if <see cref="IsAutoSeekEnabled"/>
    /// </summary>
    /// <param name="event">Event to seek to the start of</param>
    public void AutoSeekTo(Event @event)
    {
        if (!IsVideoLoaded || !IsAutoSeekEnabled)
            return;
        if (_isVideoPlaying || _isAudioPlaying)
            Stop();
        CurrentFrame = VideoInfo.FrameFromTime(@event.Start);

        var paddingPx = (long)(50 * VisualizerScaleX);
        VisualizerPositionMs = Math.Max(0L, @event.Start.TotalMilliseconds - paddingPx);
    }

    /// <summary>
    /// Play a selection of audio, without playing the video
    /// </summary>
    /// <param name="event">Active event</param>
    /// <param name="kind">Kind of selection to play</param>
    /// <exception cref="InvalidOperationException">If audio is not loaded</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="kind"/> is invalid</exception>
    public void PlayAudioSelection(Event @event, AudioPlaybackKind kind)
    {
        if (!IsVideoLoaded || !IsAudioLoaded)
            throw new InvalidOperationException("Audio is not loaded");

        Stop();

        Time startTime;
        Time endTime;
        var dms = Time.FromMillis(500); // D = 500

        switch (kind)
        {
            case AudioPlaybackKind.Event:
                startTime = @event.Start;
                endTime = @event.End;
                break;
            case AudioPlaybackKind.Before:
                startTime = Time.Max(@event.Start - dms, Time.Zero);
                endTime = @event.Start;
                break;
            case AudioPlaybackKind.First:
                startTime = @event.Start;
                endTime = Time.Min(@event.Start + dms, @event.End);
                break;
            case AudioPlaybackKind.Last:
                startTime = Time.Max(@event.End - dms, @event.Start);
                endTime = @event.End;
                break;
            case AudioPlaybackKind.After:
                startTime = @event.End;
                endTime = Time.Min(@event.End + dms, Time.FromMillis(AudioInfo.Duration));
                break;
            case AudioPlaybackKind.Surround:
                startTime = Time.Max(@event.Start - dms, Time.Zero);
                endTime = Time.Min(@event.End + dms, Time.FromMillis(AudioInfo.Duration));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        _logger.LogDebug("Playing audio selection [{StartTime}, {EndTime}]", startTime, endTime);

        var startFrame = VideoInfo.FrameFromTime(startTime);
        var endFrame = VideoInfo.FrameFromTime(endTime); // Stop on the last frame including the selection (not -1)

        _currentAudioFrame = startFrame;
        _destinationFrame = endFrame;
        _audioPlayback.IntervalIndex = _currentAudioFrame;

        var e = new PlaybackStartEventArgs(
            PlaybackTarget.Audio,
            VideoInfo.MillisecondsFromFrame(_currentAudioFrame),
            VideoInfo.MillisecondsFromFrame(_destinationFrame)
        );
        PlaybackStarted?.Invoke(this, e);

        _audioPlayback.Start();
        IsAudioPlaying = true;
        IsPaused = false;
    }

    /// <summary>
    /// Open a video file
    /// </summary>
    /// <param name="filePath">Path to the video to open</param>
    /// <param name="progressCallback">Indexing progress callback (optional)</param>
    /// <returns><see langword="true"/> if successful</returns>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public async Task<bool> OpenVideoAsync(
        string filePath,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");

        _logger.LogInformation("Opening video {FilePath}", filePath);
        IsLoadingFile = true;

        if (IsVideoLoaded)
            CloseVideo();

        return await Task.Run(() =>
        {
            var loadResult = _provider.LoadVideo(filePath, progressCallback);
            if (loadResult != 0)
            {
                // TODO: Handle error
                _logger.LogError("Load video returned error code {ErrorCode}", loadResult);
                IsLoadingFile = false;
                return false;
            }

            if (_provider.AllocateBuffers(64, 32) != 0)
            {
                IsLoadingFile = false;
                return false;
            }

            unsafe
            {
                var testFrame = _provider.GetFrame(0, 0, true);
                if (testFrame is null)
                {
                    // TODO: Handle error
                }

                VideoInfo = new VideoInfo(
                    path: filePath,
                    frameCount: _provider.FrameCount,
                    sar: new Rational { Numerator = 1, Denominator = 1 },
                    frameTimes: _provider.GetTimecodes(),
                    frameMidpoints: _provider.GetMidcodes(),
                    frameIntervals: _provider.GetFrameIntervals(),
                    keyframes: _provider.GetKeyframes(),
                    testFrame->VideoFrame->Width,
                    testFrame->VideoFrame->Height
                );

                _videoPlayback.Intervals = VideoInfo.FrameIntervals;
                _audioPlayback.Intervals = VideoInfo.FrameIntervals;
            }

            ScaleFactor = _persistence.GetScaleForRes(VideoInfo.Height);

            DisplayWidth = VideoInfo.Width / _screenScaleFactor;
            DisplayHeight = VideoInfo.Height / _screenScaleFactor;

            IsVideoLoaded = true;

            // Re-fetch frame 0 with subtitles
            unsafe
            {
                _lastFrame = _provider.GetFrame(0, 0, false);
                FrameReady?.Invoke();
            }

            IsLoadingFile = false;
            return true;
        });
    }

    /// <summary>
    /// Open an audio file
    /// </summary>
    /// <param name="filePath">Path to the audio to open</param>
    /// <param name="trackNumber">Track number to load</param>
    /// <param name="totalTracks">Total number of audio tracks</param>
    /// <param name="progressCallback">Indexing progress callback (optional)</param>
    /// <returns><see langword="true"/> if successful</returns>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public async Task<bool> OpenAudioAsync(
        string filePath,
        int trackNumber,
        int totalTracks,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");

        if (IsAudioLoaded)
            CloseAudio();

        _logger.LogInformation("Opening audio {FilePath}", filePath);
        IsLoadingFile = true;

        return await Task.Run(() =>
        {
            if (_provider.LoadAudio(filePath, trackNumber) != 0)
            {
                // TODO: Handle error
                IsLoadingFile = false;
                return false;
            }

            // Audio time
            if (_provider.AllocateAudioBuffer() != 0)
            {
                IsLoadingFile = false;
                return false; // ??
            }

            unsafe
            {
                _audioFrame = _provider.GetAudio(progressCallback);
                if (_audioFrame->Valid != 1)
                {
                    IsLoadingFile = false;
                    return false; // ??
                }

                AudioInfo = new AudioInfo(
                    path: filePath,
                    trackCount: totalTracks,
                    channelCount: _provider.GetChannelCount(),
                    sampleRate: _provider.GetSampleRate(),
                    sampleCount: _provider.GetSampleCount()
                );

                IsAudioLoaded = true;

                _lastVizFrame = _provider.GetVisualization(
                    VisualizerWidth,
                    VisualizerHeight,
                    VisualizerScaleX,
                    VisualizerScaleY,
                    0,
                    0,
                    -1,
                    null,
                    0,
                    0
                );
            }

            IsLoadingFile = false;
            return true;
        });
    }

    /// <summary>
    /// Open a keyframes file
    /// </summary>
    /// <param name="filePath">Path to the keyframes file</param>
    /// <returns><see langword="true"/> if successful</returns>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public bool OpenKeyframes(string filePath)
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        if (!IsVideoLoaded)
            return false;

        var result = _provider.LoadKeyframes(filePath);
        if (result != 0)
            return false;

        VideoInfo.Keyframes = _provider.GetKeyframes();
        RequestFrame(CurrentFrame);
        return true;
    }

    /// <summary>
    /// Get information about the audio tracks in a file
    /// </summary>
    /// <param name="filePath">Path to the file potentially containing audio tracks</param>
    /// <returns>Array of track information</returns>
    /// <exception cref="InvalidOperationException">If the provider is not initialized</exception>
    public async Task<TrackInfo[]> GetAudioTrackInfoAsync(string filePath)
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        _logger.LogInformation("Getting audio track information for {FilePath}", filePath);

        return await Task.Run(() => _provider.GetAudioTrackInfo(filePath));
    }

    /// <summary>
    /// Close the open video (includes audio)
    /// </summary>
    /// <returns><see langword="true"/> if successful</returns>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public bool CloseVideo()
    {
        if (!IsVideoLoaded)
            return true;

        Stop();
        IsVideoLoaded = false;
        IsAudioLoaded = false;

        // Close
        _logger.LogInformation("Closing video {FilePath}", VideoInfo?.Path);
        var result = _provider.CloseVideo() == 0;

        // Reset
        RotationalFactor = RotationalFactor.Default;

        // Reset the slider without triggering frame fetch
        _currentVideoFrame = 0;
        RaisePropertyChanged(nameof(CurrentFrame));
        RaisePropertyChanged(nameof(CurrentTime));

        return result;
    }

    /// <summary>
    /// Close the open audio
    /// </summary>
    /// <returns><see langword="true"/> if successful</returns>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public bool CloseAudio()
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");

        if (!IsAudioLoaded)
            return true;

        Stop();
        IsAudioLoaded = false;

        _logger.LogInformation("Closing audio {FilePath}", AudioInfo?.Path);
        return _provider.CloseAudio() == 0;
    }

    /// <summary>
    /// Advance the current video frame, or stop if the <see cref="_destinationFrame"/> has been reached
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AdvanceVideoFrame(object? sender, HighResolutionTimerElapsedEventArgs e)
    {
        if (_currentVideoFrame < _destinationFrame)
            CurrentFrame++;
        else
            Stop();
    }

    /// <summary>
    /// Advance the current frame for audio playback, or stop if the <see cref="_destinationFrame"/> has been reached
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AdvanceAudioFrame(object? sender, HighResolutionTimerElapsedEventArgs e)
    {
        if (_currentAudioFrame < _destinationFrame)
        {
            _currentAudioFrame++;
            RequestFrame(CurrentFrame);
        }
        else
            Stop();
    }

    /// <summary>
    /// Get the current frame
    /// </summary>
    /// <returns>Pointer to the frame</returns>
    /// <exception cref="InvalidOperationException">If there is no frame</exception>
    public unsafe FrameGroup* GetCurrentFrame()
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        if (!IsVideoLoaded)
            throw new InvalidOperationException("Video is not loaded");

        lock (_frameLock)
        {
            if (_nextFrame is not null)
            {
                Interlocked.Increment(ref _nextFrame->Refcount);

                _lastFrame = _nextFrame;
                _nextFrame = null;
            }
            else if (_lastFrame is not null)
            {
                Interlocked.Increment(ref _lastFrame->Refcount);
            }
        }

        if (_lastFrame is null)
            throw new InvalidOperationException("Frame is unavailable");

        return _lastFrame;
    }

    /// <summary>
    /// Get the current visualization frame
    /// </summary>
    /// <returns>Pointer to the frame</returns>
    /// <exception cref="InvalidOperationException">If there is no frame</exception>
    public unsafe Bitmap* GetCurrentVizFrame()
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        if (!IsAudioLoaded)
            throw new InvalidOperationException("Audio is not loaded");

        lock (_frameLock)
        {
            if (_nextVizFrame is not null)
            {
                Interlocked.Increment(ref _nextVizFrame->Refcount);

                _lastVizFrame = _nextVizFrame;
                _nextVizFrame = null;
            }
            else if (_lastVizFrame is not null)
            {
                Interlocked.Increment(ref _lastVizFrame->Refcount);
            }
        }

        if (_lastVizFrame is null)
            throw new InvalidOperationException("Visualization frame is unavailable");

        return _lastVizFrame;
    }

    /// <summary>
    /// Get the audio frame
    /// </summary>
    /// <returns>Pointer to the frame</returns>
    /// <exception cref="InvalidOperationException">If there is no frame</exception>
    public unsafe AudioFrame* GetAudioFrame()
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        if (!IsAudioLoaded)
            throw new InvalidOperationException("Audio is not loaded");
        return _audioFrame;
    }

    /// <summary>
    /// Set the currently-active subtitle
    /// </summary>
    /// <param name="event"></param>
    public void SetActiveSubtitle(Event @event)
    {
        _activeEventIndex = @event.Index - 1;
        RequestFrame(CurrentFrame);
    }

    /// <summary>
    /// Set the subtitles to be displayed
    /// </summary>
    /// <param name="document">Document being displayed</param>
    /// <exception cref="InvalidOperationException">If the provider isn't initialized</exception>
    public void SetSubtitles(Document document)
    {
        if (!_provider.IsInitialized || !IsVideoLoaded || IsLocked)
            return;

        lock (_requestLock)
        {
            var content = _assWriter.Write(document);
            _provider.SetSubtitles(content, null);
            _subtitlesChanged = true;
        }

        var events = document.EventManager.Events;

        lock (_boundsLock)
        {
            _eventBounds = new long[events.Count * 2];
            for (int i = 0, j = 0; i < events.Count; i++)
            {
                _eventBounds[j++] = events[i].Start.TotalMilliseconds;
                _eventBounds[j++] = events[i].End.TotalMilliseconds;
            }
        }

        RequestFrame(CurrentFrame);
    }

    /// <summary>
    /// Profile a <paramref name="selection"/> of the <paramref name="document"/> at a specific resolution
    /// </summary>
    /// <param name="document">Document to profile</param>
    /// <param name="selection">Selected events in the document</param>
    /// <param name="viewWidth">Width to render at, defaults to video width</param>
    /// <param name="viewHeight">Height to render at, defaults to video height</param>
    /// <param name="target">Target to profile, defaults to entire document</param>
    /// <param name="fontDirectories">Font directories to preload</param>
    /// <param name="progressCallback">Profiling progress callback (optional)</param>
    /// <returns>Graph-friendly profile data</returns>
    /// <exception cref="InvalidOperationException">If the provider is not initialized</exception>
    public async Task<ProfileResult> ProfileSubtitlesAsync(
        Document document,
        IList<Event> selection,
        int viewWidth = -1,
        int viewHeight = -1,
        ProfileTarget target = ProfileTarget.All,
        Uri[]? fontDirectories = null,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        if (!_provider.IsInitialized)
            throw new InvalidOperationException("Provider is not initialized");
        if (!IsVideoLoaded)
            throw new InvalidOperationException("Video is not loaded");

        if (_isVideoPlaying || _isAudioPlaying)
            Stop();
        IsLocked = true;

        viewWidth = viewWidth >= 0 ? viewWidth : VideoInfo.Width;
        viewHeight = viewHeight >= 0 ? viewHeight : VideoInfo.Height;

        var selectionDoc = document;
        if (target is ProfileTarget.SelectedEvents)
        {
            selectionDoc = new Document(false);
            foreach (var style in document.StyleManager.Styles)
                selectionDoc.StyleManager.AddOrReplace(style);
            foreach (var @event in selection)
                selectionDoc.EventManager.AddLast(@event);
        }

        // Load fonts
        foreach (var fontDir in fontDirectories ?? [])
        {
            _provider.PreloadFontDirectory(fontDir.LocalPath);
        }

        // Set subtitles
        lock (_requestLock)
        {
            var content = _assWriter.Write(selectionDoc);
            _provider.SetSubtitles(content, null);
            _subtitlesChanged = true;
        }

        var fromTime =
            target switch
            {
                ProfileTarget.All => document.EventManager.Events.Select(e => e.Start).Min(),
                _ => selection.Select(e => e.Start).Min(),
            } ?? Time.Zero;
        var toTime =
            target switch
            {
                ProfileTarget.All => document.EventManager.Events.Select(e => e.End).Max(),
                _ => selection.Select(e => e.End).Max(),
            } ?? Time.Zero;

        var fromFrame = VideoInfo.FrameFromTime(fromTime);
        var toFrame = VideoInfo.FrameFromTime(toTime);

        // Do the profiling
        var points = await Task.Run(() =>
            _provider.ProfileSubtitles(fromFrame, toFrame, viewWidth, viewHeight, progressCallback)
        );

        IsLocked = false;

        // Reset subtitles
        SetSubtitles(document);
        return ComputeProfileResult(points);
    }

    /// <summary>
    /// Construct a <see cref="ProfileResult"/> object from a list of <see cref="ProfilePoint"/>s
    /// </summary>
    /// <param name="points">Values for each frame</param>
    /// <returns>Graph-friendly data</returns>
    private static ProfileResult ComputeProfileResult(ProfilePoint[] points)
    {
        var frames = new List<double>(points.Length);
        var renderTimes = new List<double>(points.Length);
        var imageSizes = new List<double>(points.Length);
        var imageCounts = new List<double>(points.Length);

        foreach (var r in points)
        {
            frames.Add(r.Frame);
            renderTimes.Add(r.RenderTimeMs);
            imageSizes.Add(r.ImageSize / 1000.0d);
            imageCounts.Add(r.ImageCount);
        }

        return new ProfileResult
        {
            Frames = frames.ToArray(),
            RenderTimeMs = renderTimes.ToArray(),
            ImageSizeKp = imageSizes.ToArray(),
            ImageCount = imageCounts.ToArray(),
        };
    }

    /// <summary>
    /// Queue a request for a frame
    /// </summary>
    /// <param name="fetchingFrame">Frame number to fetch</param>
    private void RequestFrame(int fetchingFrame)
    {
        lock (_frameLock)
        {
            _pendingFrame = fetchingFrame;
            if (_fetchTask is null || _fetchTask.IsCompleted)
            {
                _fetchTask = Task.Run(FetchFrame);
            }
        }
    }

    /// <summary>
    /// Fetch a frame
    /// </summary>
    private unsafe void FetchFrame()
    {
        if (!IsVideoLoaded)
            return;

        int frameToFetch;
        lock (_frameLock)
        {
            frameToFetch = _pendingFrame;
            _pendingFrame = -1;
            _subtitlesChanged = false;
        }

        var videoMid = VideoInfo.MidpointFromFrame(frameToFetch); // For audio drawing
        var videoTime = VideoInfo.MillisecondsFromFrame(frameToFetch);
        var audioTime =
            _currentAudioFrame >= 0 ? VideoInfo.MillisecondsFromFrame(_currentAudioFrame) : -1;

        // Get audio visualization
        Bitmap* vizFrame = null;

        // Sanity checking so we're not updating for stupid small viewports
        if (IsAudioLoaded && VisualizerWidth > 10 && VisualizerHeight > 10)
        {
            lock (_boundsLock)
            {
                fixed (long* ptr = _eventBounds)
                {
                    vizFrame = _provider.GetVisualization(
                        VisualizerWidth,
                        VisualizerHeight,
                        VisualizerScaleX,
                        VisualizerScaleY,
                        VisualizerPositionMs,
                        videoMid,
                        audioTime,
                        ptr,
                        _eventBounds.Length,
                        _activeEventIndex
                    );
                }
            }
        }

        lock (_requestLock)
        {
            var frame = _provider.GetFrame(frameToFetch, videoTime, false);

            // Release previous _nextFrames
            if (_nextFrame is not null && _nextFrame != frame)
                Interlocked.Decrement(ref _nextFrame->Refcount);
            if (_nextVizFrame is not null && _nextVizFrame != vizFrame)
                Interlocked.Decrement(ref _nextVizFrame->Refcount);

            _nextFrame = frame;
            _nextVizFrame = vizFrame;

            FrameReady?.Invoke();

            if (_pendingFrame != -1 || _subtitlesChanged)
                _fetchTask = Task.Run(FetchFrame);
        }
    }

    /// <summary>
    /// Controls playback
    /// </summary>
    /// <param name="provider">Source Provider to use</param>
    /// <param name="logger">Logger to use</param>
    /// <param name="persistence">Persistence</param>
    public MediaController(
        ISourceProvider provider,
        ILogger<MediaController> logger,
        IPersistence persistence
    )
    {
        _provider = provider;
        _logger = logger;
        _persistence = persistence;

        _videoPlayback = new HighResolutionTimer();
        _audioPlayback = new HighResolutionTimer();
        _videoPlayback.Elapsed += AdvanceVideoFrame;
        _audioPlayback.Elapsed += AdvanceAudioFrame;

        VisualizerScaleX = persistence.VisualizationScaleX;
        VisualizerScaleY = persistence.VisualizationScaleY;

        var initResult = _provider.Initialize();
        IsEnabled = initResult == 0;
        if (!IsEnabled)
        {
            _logger.LogWarning("Source provider initialization failed! Disabling media playback.");
        }
    }

    /// <summary>
    /// Event raised when a frame is ready to be rendered
    /// </summary>
    public event Action? FrameReady;

    /// <summary>
    /// Event raised when playback starts
    /// </summary>
    public event EventHandler<PlaybackStartEventArgs>? PlaybackStarted;

    /// <summary>
    /// Event raised when playback ends
    /// </summary>
    public event EventHandler<EventArgs>? PlaybackStopped;
}
