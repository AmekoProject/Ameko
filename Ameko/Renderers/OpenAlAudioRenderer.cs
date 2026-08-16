// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Diagnostics.CodeAnalysis;
using Ameko.DataModels.OpenAl;
using Holo;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.EXT;
using Buffer = Ameko.DataModels.OpenAl.Buffer;

namespace Ameko.Renderers;

public class OpenAlAudioRenderer(MediaController mediaController) : IAudioRenderer
{
    [MemberNotNullWhen(true, nameof(_al))]
    [MemberNotNullWhen(true, nameof(_alc))]
    [MemberNotNullWhen(true, nameof(_source))]
    private bool IsInitialized { get; set; } = false;

    [MemberNotNullWhen(true, nameof(_buffer))]
    private bool IsPlaying { get; set; } = false;

    private AL? _al;
    private ALContext? _alc;
    private unsafe Device* _device;
    private unsafe Context* _context;

    private Source? _source;
    private Buffer? _buffer;

    /// <inheritdoc />
    public unsafe void Initialize()
    {
        if (_context is not null)
            return;

        // Use OpenAL Soft on macOS to work around buffer memory leak
        _al = OperatingSystem.IsMacOS() ? AL.GetApi(true) : AL.GetApi();
        _alc = OperatingSystem.IsMacOS() ? ALContext.GetApi(true) : ALContext.GetApi();

        _device = _alc.OpenDevice(null);
        if (_device is null)
            throw new AudioDeviceException("Failed to open device");

        _context = _alc.CreateContext(_device, null);
        _alc.MakeContextCurrent(_context);
        OpenAlException.ThrowIfError(_alc, _device);

        _source = new Source(_al);
        OpenAlException.ThrowIfError(_alc, _device);

        IsInitialized = true;
    }

    /// <inheritdoc />
    public void Play(long start, long end)
    {
        if (!IsInitialized)
            throw new OpenAlException("OpenAL is not initialized");
        if (IsPlaying)
        {
            _source.Stop();
            _source.DequeueBuffer(_buffer);
            _buffer.Dispose();
            _buffer = null;
        }

        if (!mediaController.IsAudioLoaded)
            return;

        unsafe
        {
            var frame = mediaController.GetAudioFrame();
            start = Math.Clamp(start, 0, frame->DurationMillis);
            end = Math.Clamp(end, start, frame->DurationMillis);

            var duration = end - start;
            var format = frame->ChannelCount == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

            _buffer = new Buffer(_al);
            _buffer.LoadData(
                frame->Data,
                start * frame->SampleRate / 1000,
                duration * frame->SampleRate / 1000,
                frame->SampleRate,
                frame->ChannelCount,
                format
            );

            _source.QueueBuffer(_buffer);
            _source.Play();
            IsPlaying = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!IsInitialized)
            throw new OpenAlException("OpenAL is not initialized");
        if (!IsPlaying)
            return;

        _source.Stop();
        _source.DequeueBuffer(_buffer);
        _buffer.Dispose();
        _buffer = null;
        IsPlaying = false;
    }

    private unsafe void Dispose(bool disposing)
    {
        Stop();

        if (_context is not null)
        {
            _source?.Dispose();

            _alc?.MakeContextCurrent(null);
            _alc?.DestroyContext(_context);
            _context = null;
        }

        if (_device is not null)
        {
            _alc?.CloseDevice(_device);
            _device = null;
        }

        if (disposing)
        {
            _al?.Dispose();
            _alc?.Dispose();
        }

        IsInitialized = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    ~OpenAlAudioRenderer()
    {
        Dispose(false);
    }
}
