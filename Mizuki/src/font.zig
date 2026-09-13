// SPDX-License-Identifier: MPL-2.0

const std = @import("std");
const c = @import("c.zig").c;

pub const glyph_height: u32 = c.MIZUKI_GLYPH_HEIGHT;
const wide_glyph_width: u32 = c.MIZUKI_WIDE_GLYPH_WIDTH;
const half_glyph_width: u32 = c.MIZUKI_HALF_GLYPH_WIDTH;

pub const Glyph = struct {
    width: u32,
    columns: [wide_glyph_width]u16,
};

fn HalfGlyph(bytes: [half_glyph_width]u16) Glyph {
    var columns: [wide_glyph_width]u16 = [_]u16{0} ** wide_glyph_width;
    @memcpy(columns[0..half_glyph_width], &bytes);
    return .{ .width = half_glyph_width, .columns = columns };
}

fn WideGlyph(bytes: [wide_glyph_width]u16) Glyph {
    return .{ .width = wide_glyph_width, .columns = bytes };
}

const tofu_half = HalfGlyph(c.MIZUKI_TOFU_HALF_GLYPH);
const tofu_wide = WideGlyph(c.MIZUKI_TOFU_WIDE_GLYPH);

// Iterate a generic run-descriptor table, falling back to sparse binary search
fn LookupHalf(cp: u21) ?Glyph {
    const cp32: u32 = @intCast(cp);
    if (@hasDecl(c, "MIZUKI_HALF_RUNS_COUNT")) {
        for (c.MIZUKI_HALF_RUNS[0..c.MIZUKI_HALF_RUNS_COUNT]) |run| {
            if (cp32 >= run.base and cp32 < run.base + run.count) {
                return HalfGlyph(run.glyphs[@as(usize, cp32 - run.base)]);
            }
        }
    }
    if (@hasDecl(c, "MIZUKIHALFSPARSE_COUNT")) {
        var lo: usize = 0;
        var hi: usize = c.MIZUKIHALFSPARSE_COUNT;
        while (lo < hi) {
            const mid = lo + (hi - lo) / 2;
            const entry = c.MIZUKIHALFSPARSE[mid];
            if (entry.codepoint == cp32) return HalfGlyph(entry.glyph);
            if (entry.codepoint < cp32) lo = mid + 1 else hi = mid;
        }
    }
    return null;
}

fn LookupWide(cp: u21) ?Glyph {
    const cp32: u32 = @intCast(cp);
    if (@hasDecl(c, "MIZUKI_WIDE_RUNS_COUNT")) {
        for (c.MIZUKI_WIDE_RUNS[0..c.MIZUKI_WIDE_RUNS_COUNT]) |run| {
            if (cp32 >= run.base and cp32 < run.base + run.count) {
                return WideGlyph(run.glyphs[@as(usize, cp32 - run.base)]);
            }
        }
    }
    if (@hasDecl(c, "MIZUKIWIDESPARSE_COUNT")) {
        var lo: usize = 0;
        var hi: usize = c.MIZUKIWIDESPARSE_COUNT;
        while (lo < hi) {
            const mid = lo + (hi - lo) / 2;
            const entry = c.MIZUKIWIDESPARSE[mid];
            if (entry.codepoint == cp32) return WideGlyph(entry.glyph);
            if (entry.codepoint < cp32) lo = mid + 1 else hi = mid;
        }
    }
    return null;
}

/// Look up the glyph for a Unicode codepoint,
/// falling back to a tofu box if the font doesn't cover it.
pub fn Lookup(cp: u21) Glyph {
    if (LookupHalf(cp)) |g| return g;
    if (LookupWide(cp)) |g| return g;
    return if (cp < 0x2E80) tofu_half else tofu_wide; // CJK heuristic
}
