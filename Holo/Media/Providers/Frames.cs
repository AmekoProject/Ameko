// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Holo.Media.Providers;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FrameGroup
{
    public VideoFrame* VideoFrame;
    public SubtitleFrame* SubtitleFrame;
    public int Refcount;
}

[StructLayout(LayoutKind.Sequential)]
public struct VideoFrame
{
    public int FrameNumber;
    public long Timestamp;
    public int Width;
    public int Height;
    public int Pitch;
    public int Flipped;
    public unsafe byte* Data;
    public int Valid;
}

[StructLayout(LayoutKind.Sequential)]
public struct SubtitleFrame
{
    public int FrameNumber;
    public long Timestamp;
    public int Width;
    public int Height;
    public int Pitch;
    public int Flipped;
    public unsafe byte* Data;
    public int Valid;
}

[StructLayout(LayoutKind.Sequential)]
public struct AudioFrame
{
    public unsafe short* Data;
    public long Length;
    public int ChannelCount;
    public long SampleCount;
    public int SampleRate;
    public long DurationMillis;
    public int Valid;
}

[StructLayout(LayoutKind.Sequential)]
public struct TrackInfo
{
    public int Index;
    public string Codec;
    // public string Language;
    // public string Title;
}

[StructLayout(LayoutKind.Sequential)]
public struct Bitmap
{
    public int Width;
    public int Height;
    public int Pitch;
    public nuint Capacity;
    public unsafe byte* Data;
    public int Refcount;
}
