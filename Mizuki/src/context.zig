// SPDX-License-Identifier: MPL-2.0

//! Contextualization

const std = @import("std");
const c = @import("c.zig").c;
const frames = @import("frames.zig");
const common = @import("common.zig");
const logger = @import("logger.zig");

pub const FFMSContext = struct {
    // Allegedly Private
    index: ?*c.FFMS_Index,
    video_source: ?*c.FFMS_VideoSource,
    audio_source: ?*c.FFMS_AudioSource,
    color_space_buffer: [128]u8,
    color_space: *const u8,
    video_color_space: c_int,
    video_color_range: c_int,

    // Public
    keyframes: ?[]c_int,
    timecodes: ?[]c_longlong,
    kf_timecodes: ?[]c_longlong,
    frame_intervals: ?[]c_longlong,
    frame_count: c_int,

    frame_width: usize,
    frame_height: usize,
    frame_pitch: usize,

    has_audio: bool,
    track_info_arr: ?[]common.TrackInfo,
    channel_count: c_int,
    sample_rate: c_int,
    sample_count: i64,

    pub fn Init() FFMSContext {
        var out: FFMSContext = .{
            .index = null,
            .video_source = null,
            .audio_source = null,
            .color_space_buffer = std.mem.zeroes([128]u8),
            .color_space = undefined,
            .video_color_space = -1,
            .video_color_range = -1,
            .keyframes = null,
            .timecodes = null,
            .kf_timecodes = null,
            .frame_intervals = null,
            .frame_count = undefined,
            .frame_width = 0,
            .frame_height = 0,
            .frame_pitch = 0,
            .has_audio = false,
            .track_info_arr = null,
            .channel_count = -1,
            .sample_rate = -1,
            .sample_count = -1,
        };
        out.color_space = &out.color_space_buffer[0];
        return out;
    }
};

pub const LibassContext = struct {
    renderer: ?*c.ass_renderer,
    track: ?*c.ass_track,
    current_hash: u32,

    pub fn Init() LibassContext {
        return LibassContext{
            .renderer = null,
            .track = null,
            .current_hash = 0,
        };
    }
};

pub const BuffersContext = struct {
    max_size: i64,
    total_size: i64,
    frame_buffers: std.ArrayList(*frames.FrameGroup),
    audio_buffer: ?[]i16,
    audio_frame: ?*frames.AudioFrame,
    viz_buffers: ?std.ArrayList(*frames.Bitmap),
    max_viz_buffers: usize = 16,

    pub fn Init() BuffersContext {
        return BuffersContext{
            .max_size = 0,
            .total_size = 0,
            .frame_buffers = undefined,
            .audio_buffer = null,
            .audio_frame = null,
            .viz_buffers = null,
        };
    }
};

pub const GlobalContext = struct {
    ffms: FFMSContext,
    libass: LibassContext,
    buffers: BuffersContext,

    pub fn Init() GlobalContext {
        return GlobalContext{
            .ffms = FFMSContext.Init(),
            .libass = LibassContext.Init(),
            .buffers = BuffersContext.Init(),
        };
    }

    pub fn Destroy(ctx: *GlobalContext) void {
        FFMSContext.Destroy(ctx.ffms);
        LibassContext.Destroy(ctx.libass);
        BuffersContext.Destroy(ctx.buffers);
    }
};

/// Create a context
pub fn CreateContext() !*GlobalContext {
    const context = try common.allocator.create(GlobalContext);
    context.* = GlobalContext.Init();
    return context;
}

/// Destroy a context
pub fn DestroyContext(ctx: *GlobalContext) void {
    common.allocator.destroy(ctx);
}
