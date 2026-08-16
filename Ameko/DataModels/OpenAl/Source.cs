// SPDX-License-Identifier: GPL-3.0-only

using System;
using Silk.NET.OpenAL;

namespace Ameko.DataModels.OpenAl;

public class Source : IDisposable
{
    private readonly uint _handle;
    private readonly AL _al;

    /// <summary>
    /// Create a source
    /// </summary>
    /// <param name="al">OpenGL instance</param>
    public Source(AL al)
    {
        _al = al;
        _handle = _al.GenSource();
    }

    public void QueueBuffer(Buffer buffer)
    {
        _al.SourceQueueBuffers(_handle, [buffer.Handle]);
    }

    public void DequeueBuffer(Buffer buffer)
    {
        _al.SourceUnqueueBuffers(_handle, [buffer.Handle]);
    }

    public void Play()
    {
        _al.SourcePlay(_handle);
    }

    public void Stop()
    {
        _al.SourceStop(_handle);
    }

    /// <summary>
    /// Dispose of the source
    /// </summary>
    public void Dispose()
    {
        _al.DeleteSource(_handle);
        GC.SuppressFinalize(this);
    }
}
