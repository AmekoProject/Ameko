// SPDX-License-Identifier: MPL-2.0

const c = @import("c.zig").c;

const std = @import("std");
const ffms = @import("ffms.zig");
const libass = @import("libass.zig");
const frames = @import("frames.zig");
const common = @import("common.zig");
const errors = @import("errors.zig");
const logger = @import("logger.zig");
const buffers = @import("buffers.zig");
const context = @import("context.zig");
const keyframes = @import("keyframes.zig");

var is_initialized = false;

/// Initialize the library
pub export fn Initialize() c_int {
    if (is_initialized) {
        return 0;
    }
    logger.Debug("Initializing Mizuki...");

    common.InitAllocator();

    ffms.Initialize();
    libass.Initialize();

    is_initialized = true;
    logger.Debug("Done!");

    return 0;
}

/// Create a context
pub export fn CreateContext() ?*context.GlobalContext {
    const g_ctx = context.CreateContext() catch {
        return null;
    };
    return g_ctx;
}

/// Destroy a context
pub export fn DestroyContext(g_ctx: *context.GlobalContext) void {
    context.DestroyContext(g_ctx);
}

/// Open a video file
pub export fn LoadVideo(
    g_ctx: *context.GlobalContext,
    file_name: [*c]u8,
    cache_file_name: [*c]u8,
    color_matrix: [*c]u8,
    progress_cb: common.ProgressCallback,
) c_int {
    const ffms_ctx = g_ctx.*.ffms;
    ffms.LoadVideo(
        g_ctx,
        file_name,
        cache_file_name,
        color_matrix,
        progress_cb,
    ) catch |err| {
        return errors.IntFromFfmsError(err);
    };
    libass.LoadVideo(
        g_ctx,
        @intCast(ffms_ctx.frame_width),
        @intCast(ffms_ctx.frame_height),
    );
    return 0;
}

/// Open an audio file
pub export fn LoadAudio(
    g_ctx: *context.GlobalContext,
    file_name: [*c]u8,
    cache_file_name: [*c]u8,
    audio_track_number: c_int, // Nullable
) c_int {
    ffms.LoadAudio(g_ctx, file_name, cache_file_name, audio_track_number) catch |err| {
        return errors.IntFromFfmsError(err);
    };
    return 0;
}

/// Open a keyframes file
pub export fn LoadKeyframes(g_ctx: *context.GlobalContext, file_name: [*c]u8) c_int {
    keyframes.Load(file_name, g_ctx) catch |err| {
        return errors.IntFromKeyframeError(err);
    };
    return 0;
}

// Get audio track information
pub export fn GetAudioTrackInfo(
    g_ctx: *context.GlobalContext,
    file_name: [*c]u8,
) common.TrackInfoArray {
    return ffms.GetAudioTrackInfo(g_ctx, file_name) catch return .{
        .ptr = null,
        .len = 0,
    };
}

/// Close the open video file (includes audio)
pub export fn CloseVideo(g_ctx: *context.GlobalContext) c_int {
    libass.CloseVideo(g_ctx);
    ffms.CloseVideo(g_ctx);
    buffers.Deinit(g_ctx);
    return 0;
}

/// Close the open audio file
pub export fn CloseAudio(g_ctx: *context.GlobalContext) c_int {
    buffers.FreeAudioBuffer(g_ctx);
    return 0;
}

pub export fn SetSubtitles(g_ctx: *context.GlobalContext, data: [*c]u8, data_len: c_int, code_page: [*c]u8) c_int {
    libass.SetSubtitles(g_ctx, data, data_len, code_page);
    return 0;
}

/// Allocate frame buffers
pub export fn AllocateBuffers(g_ctx: *context.GlobalContext, num_buffers: c_int, max_cache_mb: c_int) c_int {
    const ffms_ctx = g_ctx.*.ffms;
    buffers.InitVideo(
        g_ctx,
        @intCast(num_buffers),
        max_cache_mb,
        ffms_ctx.frame_width,
        ffms_ctx.frame_height,
        ffms_ctx.frame_pitch,
    ) catch |err| {
        return errors.IntFromFfmsError(err);
    };
    return 0;
}

pub export fn AllocateAudioBuffer(g_ctx: *context.GlobalContext) c_int {
    buffers.InitAudio(g_ctx) catch |err| {
        return errors.IntFromFfmsError(err);
    };
    buffers.InitVisualization(g_ctx);
    return 0;
}

/// Free frame buffers
pub export fn FreeBuffers(g_ctx: *context.GlobalContext) c_int {
    buffers.Deinit(g_ctx);
    return 0;
}

/// Get a frame
pub export fn GetFrame(g_ctx: *context.GlobalContext, frame_number: c_int, timestamp: c_longlong, raw: c_int) ?*frames.FrameGroup {
    return buffers.ProcVideoFrame(g_ctx, frame_number, timestamp, raw) catch {
        return null;
    };
}

/// Get the audio
pub export fn GetAudio(g_ctx: *context.GlobalContext, progress_cb: common.ProgressCallback) ?*frames.AudioFrame {
    return buffers.GetAudio(g_ctx, progress_cb) catch {
        return null;
    };
}

/// Get an audio visualization bitmap
pub export fn GetVisualization(
    g_ctx: *context.GlobalContext,
    width: c_int,
    height: c_int,
    pixel_ms: f64,
    amplitude_scale: f64,
    start_time: i64,
    video_time: i64,
    audio_time: i64,
    event_bounds: [*]i64,
    event_bounds_len: c_int,
) ?*frames.Bitmap {
    return buffers.ProcVisualizationFrame(
        g_ctx,
        width,
        height,
        pixel_ms,
        amplitude_scale,
        @floatFromInt(start_time),
        @floatFromInt(video_time),
        @floatFromInt(audio_time),
        event_bounds,
        @intCast(event_bounds_len),
    ) catch {
        return null;
    };
}

/// Get the number of frames in the video
pub export fn GetFrameCount(g_ctx: *context.GlobalContext) c_int {
    return g_ctx.*.ffms.frame_count;
}

/// Get array of keyframes
pub export fn GetKeyframes(g_ctx: *context.GlobalContext) common.IntArray {
    return .{
        .ptr = g_ctx.*.ffms.keyframes.?.ptr,
        .len = g_ctx.*.ffms.keyframes.?.len,
    };
}

/// Get array of timecodes
pub export fn GetTimecodes(g_ctx: *context.GlobalContext) common.LongArray {
    return .{
        .ptr = g_ctx.*.ffms.timecodes.?.ptr,
        .len = g_ctx.*.ffms.timecodes.?.len,
    };
}

/// Get array of frame intervals
pub export fn GetFrameIntervals(g_ctx: *context.GlobalContext) common.LongArray {
    return .{
        .ptr = g_ctx.*.ffms.frame_intervals.?.ptr,
        .len = g_ctx.*.ffms.frame_intervals.?.len,
    };
}

/// Get the number of channels in the audio
pub export fn GetChannelCount(g_ctx: *context.GlobalContext) c_int {
    return g_ctx.*.ffms.channel_count;
}

/// Get the audio's sample rate
pub export fn GetSampleRate(g_ctx: *context.GlobalContext) c_int {
    return g_ctx.*.ffms.sample_rate;
}

/// Get the number of samples in the audio
pub export fn GetSampleCount(g_ctx: *context.GlobalContext) c_longlong {
    return @intCast(g_ctx.*.ffms.sample_count);
}

/// Set the logging callback
pub export fn SetLoggerCallback(callback: logger.LogCallback) void {
    logger.SetCallback(callback);
}

/// Free an int array
///
/// Call this on Deinit
fn FreeIntArray(array: common.IntArray) void {
    const slice = @as([*]c_int, array.ptr)[0..array.len];
    common.allocator.free(slice);
}

/// Free a long array
///
/// Call this on Deinit
fn FreeLongArray(array: common.LongArray) void {
    const slice = @as([*]c_longlong, array.ptr)[0..array.len];
    common.allocator.free(slice);
}

pub fn main() !void {
    const ffms_version = ffms.GetVersion();
    std.debug.print("FFMS2 Version: {x}.{x}.{x}\n", .{
        ffms_version.major,
        ffms_version.minor,
        ffms_version.patch,
    });

    libass.Initialize();
    const libass_version = libass.GetVersion();
    std.debug.print("Libass Version: {x}.{x}.{x}\n", .{
        libass_version.major,
        libass_version.minor,
        libass_version.patch,
    });
}
