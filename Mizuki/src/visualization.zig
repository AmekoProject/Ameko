// SPDX-License-Identifier: MPL-2.0

const std = @import("std");
const font = @import("font.zig");
const common = @import("common.zig");
const frames = @import("frames.zig");
const logger = @import("logger.zig");
const context = @import("context.zig");

// Colors
const color_waveform: u32 = 0xff00ff00;
const color_v_playhead: u32 = 0xffff0000;
const color_a_playhead: u32 = 0xff00aeff;
const color_qseconds: u32 = 0xffd0d0d0;
const color_seconds: u32 = 0xfff85797;
const color_event: u32 = 0xff937df8;
const color_kf: u32 = 0xffe1e1e1;

/// Render a waveform representation of the audio
pub fn RenderWaveform(
    g_ctx: *context.GlobalContext,
    bmp: *frames.Bitmap,
    pixels_per_ms: f64,
    amplitude_scale: f64,
    start_time: f64,
    v_playhead_ms: f64,
    a_playhead_ms: f64,
    event_bounds: [*]i64,
    event_bounds_len: usize,
) void {
    const audio_data = g_ctx.*.buffers.audio_buffer;
    const is_stereo = g_ctx.*.ffms.channel_count == 2;
    const sample_rate: f64 = @floatFromInt(g_ctx.*.ffms.sample_rate);

    const bmp_width_u: usize = @intCast(bmp.*.width);
    const bmp_height_u: usize = @intCast(bmp.*.height);
    const bmp_pitch_u: usize = @intCast(bmp.*.pitch);
    const bmp_width_f: f64 = @floatFromInt(bmp.*.width);
    const bmp_height: u32 = @intCast(bmp_height_u);
    const bmp_mid: u32 = @divFloor(bmp_height, 2);
    const bmp_mid_i: i32 = @intCast(bmp_mid);
    const wfv_height: u32 = @divFloor(bmp_height * 8, 10); // 80% height
    const wvf_mid: u32 = @divFloor(wfv_height, 2);
    const gutter_height: u32 = @divFloor(bmp_height, 10); // 10% height
    const gutter_half: u32 = @divFloor(gutter_height, 2);
    const gutter_quarter: u32 = @divFloor(gutter_height, 4);

    if (bmp_height_u < 1 or bmp_width_u < 1 or bmp_pitch_u < 1) return;

    if (audio_data) |audio| {
        const samples_per_pixel: usize = @intFromFloat(pixels_per_ms * sample_rate / 1000.0);
        const effective_arr_len: usize = if (is_stereo) audio.len / 2 else audio.len;
        const total_duration_ms: f64 = (@as(f64, @floatFromInt(effective_arr_len)) * 1000.0) / sample_rate;
        const visible_duration_ms = bmp_width_f * pixels_per_ms;

        // Calculate start and end times
        var start_ms = start_time;

        if (total_duration_ms > visible_duration_ms) {
            const max_start_ms = total_duration_ms - visible_duration_ms;
            if (start_ms > max_start_ms) {
                start_ms = max_start_ms;
            }
        } else {
            start_ms = 0.0; // Audio is shorter than the viewport
        }

        const end_ms = start_ms + visible_duration_ms;

        // Clear the bitmap
        const bmp_total_bytes = bmp_height_u * bmp_pitch_u;
        @memset(bmp.*.data[0..bmp_total_bytes], 0);

        // Draw hashes for seconds and quarter-seconds in the gutter
        DrawTimeScale(
            bmp,
            bmp_width_f,
            bmp_height,
            gutter_height,
            gutter_half,
            gutter_quarter,
            pixels_per_ms,
            start_ms,
            end_ms,
        );

        // Draw keyframe indicators behind the spectrum
        if (g_ctx.*.ffms.kf_timecodes) |timecodes| {
            DrawKeyframes(
                bmp,
                bmp_width_f,
                bmp_height,
                pixels_per_ms,
                start_ms,
                end_ms,
                timecodes,
            );
        }

        // Draw the waveform
        var current_sample: usize = @intFromFloat(start_ms * sample_rate / 1000.0);
        for (0..bmp_width_u) |x| {
            const s0: usize = current_sample;
            const s1: usize = @min(s0 + samples_per_pixel, effective_arr_len);
            current_sample += samples_per_pixel;

            if (s0 >= effective_arr_len) break;

            var peak_min: i32 = 0;
            var peak_max: i32 = 0;

            // Compute peaks
            var i = s0;
            if (is_stereo) { // Stereo, need to downmix
                while (i < s1) : (i += 1) {
                    const left = audio[2 * i];
                    const right = audio[2 * i + 1];
                    const mono: i32 = (@as(i32, left) + @as(i32, right)) >> 1;

                    if (mono > peak_max) peak_max = mono;
                    if (mono < peak_min) peak_min = mono;
                }
            } else { // Mono
                while (i < s1) : (i += 1) {
                    const mono = @as(i32, audio[i]);

                    if (mono > peak_max) peak_max = mono;
                    if (mono < peak_min) peak_min = mono;
                }
            }

            const mid_f: f64 = @floatFromInt(wvf_mid);
            const min_f: f64 = @floatFromInt(peak_min);
            const max_f: f64 = @floatFromInt(peak_max);

            // Scale according to the amplitude
            const scaled_min_f: f64 = (min_f * amplitude_scale * mid_f) / 0x8000;
            const scaled_max_f: f64 = (max_f * amplitude_scale * mid_f) / 0x8000;

            // Clamp
            const clamped_min_i: i32 = @intFromFloat(std.math.clamp(scaled_min_f, -mid_f, mid_f));
            const clamped_max_i: i32 = @intFromFloat(std.math.clamp(scaled_max_f, -mid_f, mid_f));

            const min_y: u32 = @intCast(bmp_mid_i - clamped_min_i);
            const max_y: u32 = @intCast(bmp_mid_i - clamped_max_i);

            DrawLine(bmp, @intCast(x), min_y, max_y, color_waveform);
        }

        // Draw event bounds over the spectrum
        DrawEventBounds(
            bmp,
            bmp_width_f,
            bmp_height,
            gutter_half,
            pixels_per_ms,
            start_ms,
            end_ms,
            event_bounds,
            event_bounds_len,
        );

        // Draw playheads over everything
        DrawVideoPlayhead(
            bmp,
            bmp_width_f,
            bmp_height,
            pixels_per_ms,
            start_ms,
            end_ms,
            v_playhead_ms,
        );

        if (a_playhead_ms >= 0 and a_playhead_ms != v_playhead_ms) {
            DrawAudioPlayhead(
                bmp,
                bmp_width_f,
                bmp_height,
                pixels_per_ms,
                start_ms,
                end_ms,
                a_playhead_ms,
            );
        }
    }
}

/// Draw hashes for seconds and quarter-seconds in the gutter
fn DrawTimeScale(
    bmp: *frames.Bitmap,
    bmp_width_f: f64,
    bmp_height: u32,
    gutter_height: u32,
    gutter_half: u32,
    gutter_quarter: u32,
    pixels_per_ms: f64,
    start_ms: f64,
    end_ms: f64,
) void {
    // Draw seconds hashes
    const pixels_per_sec = 1000.0 / pixels_per_ms;
    if (pixels_per_sec < 5.0) return;

    // Only draw if there's enough room
    const can_draw_qsecs = pixels_per_sec > 50;
    const can_draw_labels = gutter_height > font.glyph_height;
    const can_draw_qlabels = can_draw_labels and pixels_per_sec > 115;

    var label_buf: [16]u8 = undefined;

    var t = std.math.ceil(start_ms / 1000.0) * 1000.0;
    while (t <= end_ms) : (t += 1000.0) {
        const delta = t - start_ms;
        const x_f = delta / pixels_per_ms;
        if (x_f >= 0 and x_f < bmp_width_f) {
            const x: u32 = @intFromFloat(x_f);
            DrawLine(bmp, x, gutter_half, gutter_height, color_seconds);
            DrawLine(bmp, x, bmp_height - gutter_height, bmp_height - gutter_half, color_seconds);

            if (can_draw_labels) {
                const label = FormatSeconds(&label_buf, t);
                const lx = LabelXPos(x, label);
                DrawText(bmp, lx, 0, label, color_seconds);
            }
        }
    }

    // Draw quarter-seconds hashes
    if (!can_draw_qsecs) return;

    t = std.math.ceil(start_ms / 250.0) * 250.0;
    while (t <= end_ms) : (t += 250.0) {
        if (@rem(t, 1000.0) == 0) continue;
        const delta = t - start_ms;
        const x_f = delta / pixels_per_ms;
        if (x_f >= 0 and x_f < bmp_width_f) {
            const x: u32 = @intFromFloat(x_f);
            DrawLine(bmp, x, gutter_half, gutter_half + gutter_quarter, color_qseconds);
            DrawLine(bmp, x, bmp_height - gutter_half - gutter_quarter, bmp_height - gutter_half, color_qseconds);

            if (can_draw_qlabels) {
                const label = FormatQuarter(&label_buf, t);
                const lx = LabelXPos(x, label);
                DrawText(bmp, lx, 0, label, color_qseconds);
            }
        }
    }
}

/// Draw keyframe indicators
fn DrawKeyframes(
    bmp: *frames.Bitmap,
    bmp_width_f: f64,
    bmp_height: u32,
    pixels_per_ms: f64,
    start_ms: f64,
    end_ms: f64,
    timecodes: []c_longlong,
) void {
    for (timecodes) |kf| {
        const t: f64 = @floatFromInt(kf);
        if (t < start_ms or t > end_ms) continue;

        const delta = t - start_ms;
        const x_f = delta / pixels_per_ms;
        if (x_f >= 0 and x_f < bmp_width_f) {
            const x: u32 = @intFromFloat(x_f);
            DrawLine(bmp, x, 0, bmp_height, color_kf);
        }
    }
}

/// Draw event bounding boxes
fn DrawEventBounds(
    bmp: *frames.Bitmap,
    bmp_width_f: f64,
    bmp_height: u32,
    gutter_half: u32,
    pixels_per_ms: f64,
    start_ms: f64,
    end_ms: f64,
    event_bounds: [*]i64,
    event_bounds_len: usize,
) void {
    // Assert that we have a list of pairs
    std.debug.assert((event_bounds_len & 1) == 0);

    var ei: usize = 0;
    while (ei < event_bounds_len) : (ei += 2) {
        const evt_start_ms: f64 = @floatFromInt(event_bounds[ei]);
        const evt_end_ms: f64 = @floatFromInt(event_bounds[ei + 1]);

        if (evt_end_ms < evt_start_ms)
            continue;

        if ((evt_start_ms < start_ms and evt_end_ms < start_ms) or (evt_start_ms > end_ms and evt_end_ms > end_ms))
            continue;

        const start_x = (evt_start_ms - start_ms) / pixels_per_ms;
        const end_x = (evt_end_ms - start_ms) / pixels_per_ms;

        // Draw posts
        if (start_x >= 0) {
            DrawLine(bmp, @intFromFloat(start_x), gutter_half, bmp_height - gutter_half, color_event);
        }

        if (evt_start_ms == evt_end_ms) continue; // Stop here if 0-duration

        if (end_x < bmp_width_f) {
            DrawLine(bmp, @intFromFloat(end_x), gutter_half, bmp_height - gutter_half, color_event);
        }

        const start_x_u: usize = @intFromFloat(@max(0, start_x));
        const end_x_u: usize = @intFromFloat(@min(bmp_width_f, end_x));

        // Draw border
        for (start_x_u..end_x_u) |x| {
            DrawLine(bmp, @intCast(x), gutter_half, gutter_half + 1, color_event);
            DrawLine(bmp, @intCast(x), bmp_height - gutter_half - 1, bmp_height - gutter_half, color_event);
        }
    }
}

/// Draw the current video time
fn DrawVideoPlayhead(
    bmp: *frames.Bitmap,
    bmp_width_f: f64,
    bmp_height: u32,
    pixels_per_ms: f64,
    start_ms: f64,
    end_ms: f64,
    playhead_ms: f64,
) void {
    if (playhead_ms < start_ms or playhead_ms > end_ms) return;

    const delta = playhead_ms - start_ms;
    const x_f = delta / pixels_per_ms;
    if (x_f >= 0 and x_f < bmp_width_f) {
        const x: u32 = @intFromFloat(x_f);
        DrawLine(bmp, x, 0, bmp_height, color_v_playhead);
    }
}

/// Draw the current autio time
fn DrawAudioPlayhead(
    bmp: *frames.Bitmap,
    bmp_width_f: f64,
    bmp_height: u32,
    pixels_per_ms: f64,
    start_ms: f64,
    end_ms: f64,
    playhead_ms: f64,
) void {
    if (playhead_ms < start_ms or playhead_ms > end_ms) return;

    const delta = playhead_ms - start_ms;
    const x_f = delta / pixels_per_ms;
    if (x_f >= 0 and x_f < bmp_width_f) {
        const x: u32 = @intFromFloat(x_f);
        DrawLine(bmp, x, 0, bmp_height, color_a_playhead);
    }
}

/// Draw a 1-px wide vertical line at position x
fn DrawLine(
    bmp: *frames.Bitmap,
    x: u32,
    y1_in: u32,
    y2_in: u32,
    color: u32,
) void {
    const w: usize = @intCast(bmp.width);
    const h: u32 = @intCast(bmp.height);
    const pitch: usize = @intCast(bmp.pitch);

    if (x >= w) return;

    var y1 = y1_in;
    var y2 = y2_in;

    if (y1 > y2) {
        const tmp = y1;
        y1 = y2;
        y2 = tmp;
    }

    if (y1 >= h) return;
    if (y2 >= h) y2 = h - 1;

    const pixel_size = 4; // 32-bit
    var y = y1;

    while (y <= y2) : (y += 1) {
        const row_ptr = bmp.data + (@as(usize, @intCast(y)) * pitch);
        const px_ptr: *u32 = @ptrCast(@alignCast(row_ptr + x * pixel_size));
        px_ptr.* = color;
    }
}

/// Draw a null-terminated ASCII string at pixel position (x, y)
fn DrawText(bmp: *frames.Bitmap, x: u32, y: u32, text: []const u8, color: u32) void {
    const bmp_w: u32 = @intCast(bmp.width);
    var cx = x;
    for (text) |c| {
        if (cx >= bmp_w) break;
        DrawGlyph(bmp, cx, y, c, color);
        cx += font.glyph_width + 1; // 4px glyph + 1px gap
    }
}

/// Draw a single glyph at pixel position (x, y), clipped to bitmap bounds.
fn DrawGlyph(bmp: *frames.Bitmap, x: u32, y: u32, char: u8, color: u32) void {
    const idx = font.IndexOf(char) orelse return;
    const glyph = font.glyphs[idx];
    const bmp_w: u32 = @intCast(bmp.width);
    const bmp_h: u32 = @intCast(bmp.height);
    const pitch: usize = @intCast(bmp.pitch);

    for (0..font.glyph_height) |row| {
        const py = y + @as(u32, @intCast(row));
        if (py >= bmp_h) break;
        const bits = glyph[row];
        for (0..font.glyph_width) |col| {
            const px = x + @as(u32, @intCast(col));
            if (px >= bmp_w) break;
            // Bits are stored in the high nibble, MSB = leftmost column.
            const mask: u8 = @as(u8, 0b1000_0000) >> @intCast(col);
            if ((bits & mask) != 0) {
                const row_ptr = bmp.data + (@as(usize, py) * pitch);
                const px_ptr: *u32 = @ptrCast(@alignCast(row_ptr + px * 4));
                px_ptr.* = color;
            }
        }
    }
}

/// Format a timestamp in milliseconds as "MM:SS" or "H:MM:SS" (if >= 60 m)
/// Writes into `buf`, returns the used slice.
fn FormatSeconds(buf: []u8, t_ms: f64) []u8 {
    const t_s: u32 = @intFromFloat(@round(t_ms / 1000.0));
    const hours = t_s / 3600;
    const minutes = (t_s % 3600) / 60;
    const secs = t_s % 60;
    if (hours > 0) {
        return std.fmt.bufPrint(buf, "{}:{d:0>2}:{d:0>2}", .{ hours, minutes, secs }) catch buf[0..0];
    } else {
        return std.fmt.bufPrint(buf, "{}:{d:0>2}", .{ minutes, secs }) catch buf[0..0];
    }
}

/// Format a quarter-second offset as ".25", ".50", or ".75"
fn FormatQuarter(buf: []u8, t_ms: f64) []u8 {
    const rem = @rem(t_ms, 1000.0);
    const label: []const u8 = if (rem < 375.0)
        ".25"
    else if (rem < 625.0)
        ".50"
    else
        ".75";
    @memcpy(buf[0..label.len], label);
    return buf[0..label.len];
}

/// Compute the left x to draw `text` centered on tick position `x`
fn LabelXPos(x: u32, text: []const u8) u32 {
    const n: u32 = @intCast(text.len);
    const w = if (n == 0) 0 else n * font.glyph_width + (n - 1);
    const half = w / 2;
    return if (half > x) 0 else x - half;
}
