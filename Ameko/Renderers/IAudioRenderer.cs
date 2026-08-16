// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace Ameko.Renderers;

public interface IAudioRenderer : IDisposable
{
    /// <summary>
    /// Initialize the renderer
    /// </summary>
    void Initialize();

    /// <summary>
    /// Play a portion of audio
    /// </summary>
    /// <param name="start">Start time in milliseconds</param>
    /// <param name="end">End time in milliseconds</param>
    void Play(long start, long end);

    /// <summary>
    /// Stop playing audio
    /// </summary>
    void Stop();
}
