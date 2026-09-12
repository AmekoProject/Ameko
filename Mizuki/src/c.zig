// SPDX-License-Identifier: MPL-2.0

/// C bindings
pub const c = @cImport({
    // FFmpegSource 2
    @cInclude("ffms.h");

    // libass
    @cInclude("ass.h");

    // misaki font
    @cInclude("misaki_font.h");
});
