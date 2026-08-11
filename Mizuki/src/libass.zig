// SPDX-License-Identifier: MPL-2.0

//! Libass Subtitle Provider

const std = @import("std");
const builtin = @import("builtin");

const c = @import("c.zig").c;
const fnv = @import("fnv.zig");
const frames = @import("frames.zig");
const common = @import("common.zig");
const logger = @import("logger.zig");
const context = @import("context.zig");

pub const LibassError = error{
    NotInitialized,
};

// Global
var library: ?*c.ASS_Library = null;

/// Get the current libass version
///
/// Requires libass to be initialized.
pub fn GetVersion() common.BackingVersion {
    const version = c.ass_library_version(); // hex
    return .{
        .major = version >> 28,
        .minor = (version >> 20) & 0xFF,
        .patch = (version >> 12) & 0xFF,
    };
}

/// Initialize libass
pub fn Initialize() void {
    if (library == null) {
        library = c.ass_library_init();
    }
    // c.ass_set_message_cb(library, &AssLogCallback, null); // TODO: When va_args is supported
}

/// Deinitialize libass
pub fn Deinitialize(g_ctx: *context.GlobalContext) void {
    var ctx = &g_ctx.*.libass;
    c.ass_renderer_done(ctx.renderer);
    c.ass_library_done(library);
    if (ctx.track != null) {
        c.ass_free_track(ctx.track);
        ctx.track = null;
    }
}

/// Load subtitles in from memory
pub fn SetSubtitles(g_ctx: *context.GlobalContext, data: [*c]u8, data_len: c_int, code_page: [*c]u8) void {
    var ctx = &g_ctx.*.libass;
    if (ctx.track != null) {
        c.ass_free_track(ctx.track);
        ctx.track = null;
    }
    ctx.track = c.ass_read_memory(library, data, @intCast(data_len), code_page);
    ctx.current_hash = fnv.fnv1a_32(data, data_len);
}

/// Set up renderer
pub fn LoadVideo(g_ctx: *context.GlobalContext, frame_width: c_int, frame_height: c_int) void {
    var ctx = &g_ctx.*.libass;
    if (ctx.renderer) |renderer| {
        c.ass_renderer_done(renderer);
        ctx.renderer = null;
    }

    ctx.renderer = c.ass_renderer_init(library);
    c.ass_set_frame_size(ctx.renderer, frame_width, frame_height);
    c.ass_set_storage_size(ctx.renderer, frame_width, frame_height);
    c.ass_set_font_scale(ctx.renderer, 1.0);

    c.ass_set_fonts(ctx.renderer, null, "Sans", 1, null, 1);
}

pub fn CloseVideo(g_ctx: *context.GlobalContext) void {
    var ctx = &g_ctx.*.libass;
    if (ctx.renderer) |renderer| {
        c.ass_renderer_done(renderer);
        ctx.renderer = null;
    }
    if (ctx.track) |track| {
        c.ass_free_track(track);
        ctx.track = null;
    }
    if (ctx.profile_points) |profile_points| {
        common.allocator.free(profile_points);
        ctx.profile_points = null;
    }
}

/// Verify a frame's hash
pub fn VerifyHash(g_ctx: *context.GlobalContext, frame: ?*frames.SubtitleFrame) bool {
    const ctx = &g_ctx.*.libass;
    return ctx.current_hash != 0 and ctx.current_hash == frame.?.*.hash;
}

/// Get the subtitles (On a transparent frame)
pub fn GetFrame(g_ctx: *context.GlobalContext, timestamp: c_longlong, out: *frames.SubtitleFrame) !void {
    const ctx = &g_ctx.*.libass;
    const frame_w: i32 = @intCast(out.*.width);
    const frame_h: i32 = @intCast(out.*.height);
    const buffer_size: usize = @intCast(out.*.pitch * out.*.height);

    // Set the frame's hash
    out.*.hash = ctx.current_hash;

    // Reset buffer to transparent
    const buf = out.data[0..buffer_size];
    @memset(buf, 0);

    if (ctx.track == null) {
        return;
    }

    c.ass_set_frame_size(ctx.renderer, out.*.width, out.*.height);
    c.ass_set_storage_size(ctx.renderer, out.*.width, out.*.height);

    const detect_change = 0;
    var img: ?*c.ASS_Image = c.ass_render_frame(ctx.renderer, ctx.track, timestamp, detect_change);

    while (img) |i| {
        const o: u32 = (255 - (i.*.color & 0xFF)); // opacity
        const r: u32 = @intCast(i.*.color >> 24);
        const g: u32 = @intCast((i.*.color >> 16) & 0xFF);
        const b: u32 = @intCast((i.*.color >> 8) & 0xFF);

        var y: i32 = 0;
        while (y < i.*.h) : (y += 1) {
            var x: i32 = 0;
            while (x < i.*.w) : (x += 1) {
                const src_index: usize = @intCast(y * i.*.stride + x);
                const dest_x: i32 = x + i.*.dst_x;
                const dest_y: i32 = y + i.*.dst_y;

                if (dest_x >= 0 and dest_x < frame_w and dest_y >= 0 and dest_y < frame_h) {
                    const dest_index: usize = @intCast((dest_y * frame_w + dest_x) * 4);

                    const src_alpha: u8 = i.*.bitmap[src_index];
                    const k: u32 = src_alpha * o / 255;

                    buf[dest_index + 0] = @intCast((k * b + (255 - k) * buf[dest_index + 0]) / 255);
                    buf[dest_index + 1] = @intCast((k * g + (255 - k) * buf[dest_index + 1]) / 255);
                    buf[dest_index + 2] = @intCast((k * r + (255 - k) * buf[dest_index + 2]) / 255);
                    buf[dest_index + 3] = @intCast(k + (255 - k) * buf[dest_index + 3] / 255);
                }
            }
        }

        img = i.*.next;
    }
}

// Profile the current subtitle file
//
// Assumes the frame range is valid
pub fn ProfileSubtitles(
    g_ctx: *context.GlobalContext,
    from_frame: usize,
    to_frame: usize,
    frame_width: usize,
    frame_height: usize,
    progress_cb: common.ProgressCallback,
) common.AssProfilePointArray {
    const ctx = &g_ctx.*.libass;

    // Set up a dedicated ass_renderer
    const renderer = c.ass_renderer_init(library);
    c.ass_set_frame_size(renderer, @intCast(frame_width), @intCast(frame_height));
    c.ass_set_storage_size(renderer, @intCast(frame_width), @intCast(frame_height));
    c.ass_set_font_scale(renderer, 1.0);

    c.ass_set_fonts(renderer, null, "Sans", 1, null, 1);

    // Reset previous data if it exists
    if (ctx.profile_points) |profile_points| {
        common.allocator.free(profile_points);
        ctx.profile_points = null;
    }

    var profile_point_list: std.ArrayList(common.AssProfilePoint) = .empty;

    var progress: i64 = 0;
    const max_progress: i64 = @intCast(to_frame - from_frame);

    var frame = from_frame;
    while (frame < to_frame) : ({
        frame += 1;
        progress += 1;
    }) {
        const timestamp = g_ctx.*.ffms.timecodes.?[frame];
        var image_count: c_int = 0;
        var image_size: c_int = 0;

        const start_time = std.Io.Clock.real.now(common.io).toMicroseconds();

        // Uses the global track from SetSubtitles
        var img: ?*c.ASS_Image = c.ass_render_frame(renderer, ctx.track, timestamp, 0);

        const finish_time = std.Io.Clock.real.now(common.io).toMicroseconds();
        const elapsed_ms: f64 = @as(f64, @floatFromInt(finish_time - start_time)) / std.time.us_per_ms;

        // TODO: Do we want to draw this to a frame and add a time for that as well?
        while (img) |i| {
            image_count += 1;
            image_size += (i.w * i.h);
            img = i.*.next;
        }

        profile_point_list.append(common.allocator, .{
            .frame = @intCast(frame),
            .timestamp = timestamp,
            .render_ms = elapsed_ms,
            .image_size = image_size,
            .image_count = image_count,
        }) catch unreachable;

        // Call progress callback, if provided
        if (progress_cb) |cb| {
            _ = cb(progress, max_progress, null);
        }
    }

    // Dispose of the dedicated renderer
    c.ass_renderer_done(renderer);

    ctx.profile_points = profile_point_list.toOwnedSlice(common.allocator) catch unreachable;
    return .{ .ptr = ctx.profile_points.?.ptr, .len = ctx.profile_points.?.len };
}

/// Callback for handling logs emitted by libass
///
/// Currently unused since Zig doesn't support va_list yet
pub export fn AssLogCallback(
    level: c_int,
    fmt: [*c]const u8,
    args: [*c]u8,
    data: ?*anyopaque,
) callconv(.c) void {
    _ = args;
    _ = data;
    if (level < 5) {
        // Note: Would need to work with the va_list
        // but it's not implemented yet(?)
        // https://github.com/ziglang/zig/blob/0d0f09fb0ee60b5fa42f51732bde2a4db43453a8/test/behavior/var_args.zig#L234
        const fmt_slice = std.mem.sliceTo(fmt, 0);
        logger.Info(fmt_slice);
    }
}
