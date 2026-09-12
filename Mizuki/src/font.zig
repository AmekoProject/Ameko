// SPDX-License-Identifier: MPL-2.0

const std = @import("std");
const c = @import("c.zig").c;

pub const glyph_height: u32 = 8;

pub const Glyph = struct {
    width: u32,
    columns: [8]u8,
};

fn HalfGlyph(bytes: [3]u8) Glyph {
    return .{ .width = 3, .columns = .{ bytes[0], bytes[1], bytes[2], 0, 0, 0, 0, 0 } };
}

fn WideGlyph(bytes: [8]u8) Glyph {
    return .{ .width = 8, .columns = bytes };
}

const tofu_half = HalfGlyph(c.MIZUKI_TOFU_HALF_GLYPH);
const tofu_wide = WideGlyph(c.MIZUKI_TOFU_WIDE_GLYPH);

fn IsInRun(cp: u21, base: u32, count: u32) bool {
    return cp >= base and cp < base + count;
}

fn LookupHalf(cp: u21) ?Glyph {
    if (IsInRun(cp, c.MIZUKI_ASCII_BASE, c.MIZUKI_ASCII_COUNT)) {
        return HalfGlyph(c.MIZUKI_ASCII_GLYPHS[@as(usize, cp) - c.MIZUKI_ASCII_BASE]);
    }
    if (IsInRun(cp, c.MIZUKI_HALFWIDTH_KANA_BASE, c.MIZUKI_HALFWIDTH_KANA_COUNT)) {
        return HalfGlyph(c.MIZUKI_HALFWIDTH_KANA_GLYPHS[@as(usize, cp) - c.MIZUKI_HALFWIDTH_KANA_BASE]);
    }

    return null;
}

fn LookupWide(cp: u21) ?Glyph {
    if (IsInRun(cp, c.MIZUKI_HIRAGANA_BASE, c.MIZUKI_HIRAGANA_COUNT)) {
        return WideGlyph(c.MIZUKI_HIRAGANA_GLYPHS[@as(usize, cp) - c.MIZUKI_HIRAGANA_BASE]);
    }
    if (IsInRun(cp, c.MIZUKI_KATAKANA_BASE, c.MIZUKI_KATAKANA_COUNT)) {
        return WideGlyph(c.MIZUKI_KATAKANA_GLYPHS[@as(usize, cp) - c.MIZUKI_KATAKANA_BASE]);
    }
    if (IsInRun(cp, c.MIZUKI_FULLWIDTH_ASCII_BASE, c.MIZUKI_FULLWIDTH_ASCII_COUNT)) {
        return WideGlyph(c.MIZUKI_FULLWIDTH_ASCII_GLYPHS[@as(usize, cp) - c.MIZUKI_FULLWIDTH_ASCII_BASE]);
    }

    var lo: usize = 0;
    var hi: usize = c.MIZUKIWIDESPARSE_COUNT;
    while (lo < hi) {
        const mid = lo + (hi - lo) / 2;
        const entry = c.MIZUKIWIDESPARSE[mid];
        if (entry.codepoint == cp) return WideGlyph(entry.glyph);
        if (entry.codepoint < cp) lo = mid + 1 else hi = mid;
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
