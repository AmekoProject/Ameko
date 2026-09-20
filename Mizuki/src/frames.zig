// SPDX-License-Identifier: MPL-2.0

const c = @import("c.zig").c;

/// Frame containing the base video
pub const VideoFrame = extern struct {
    frame_number: c_int = -1,
    timestamp: c_longlong = 0,
    width: c_int,
    height: c_int,
    pitch: c_int,
    flipped: c_int,
    data: [*]u8, // ffms_Frame.Data[0]
    valid: c_int = 0, // bool
};

/// Frame containing rendered subtitles on a transparent background
pub const SubtitleFrame = extern struct {
    hash: u32 = 0,
    timestamp: c_longlong = 0,
    width: c_int,
    height: c_int,
    pitch: c_int,
    flipped: c_int,
    data: [*]u8,
    valid: c_int = 0, // bool
};

/// Frame containing audio samples
pub const AudioFrame = extern struct {
    data: [*]i16,
    length: i64,
    channel_count: c_int,
    sample_count: i64,
    sample_rate: c_int,
    duration_ms: i64,
    valid: c_int = 0, // bool
};

/// Collection of the VideoFrame and SubtitleFrame
pub const FrameGroup = extern struct {
    video_frame: *VideoFrame,
    subtitle_frame: *SubtitleFrame,
    refcount: c_int = 0,
};

/// Generic bitmap
pub const Bitmap = extern struct {
    width: c_int,
    height: c_int,
    pitch: c_int,
    capacity: usize,
    data: [*]u8,
    refcount: c_int = 0,
};

/// Syllable information
pub const SyllableInfo = extern struct {
    duration: i64,
    text: [48]u8,
};
