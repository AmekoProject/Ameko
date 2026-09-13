// SPDX-License-Identifier: MPL-2.0

/// C bindings
pub const c = @cImport({
    // FFmpegSource 2
    @cInclude("ffms.h");

    // libass
    @cInclude("ass.h");

    // TekuPlus font
    @cInclude("tekuplus.h");
});
