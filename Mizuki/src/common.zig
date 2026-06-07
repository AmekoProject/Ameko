// SPDX-License-Identifier: MPL-2.0

const std = @import("std");

pub var gpa: std.heap.DebugAllocator(.{ .safety = true }) = undefined;
pub var allocator: std.mem.Allocator = undefined;
pub var io: std.Io = undefined;

/// Initialize the global allocator
pub fn InitAllocator() void {
    gpa = .init;
    allocator = gpa.allocator();

    var threaded: std.Io.Threaded = .init(allocator, .{});
    io = threaded.io();
}

/// Version of a backing library
pub const BackingVersion = extern struct {
    major: c_int,
    minor: c_int,
    patch: c_int,
};

/// A frame of a video
pub const VideoFrame = extern struct {
    isKeyFrame: bool,
    width: c_int,
    height: c_int,
    data: [4][*c]const u8,
    lineSize: [4]c_int,
};

/// Interoperable int array
pub const IntArray = extern struct {
    ptr: [*c]c_int,
    len: usize,
};

/// Interoperable long array
pub const LongArray = extern struct {
    ptr: [*c]c_longlong,
    len: usize,
};

/// Basic information about a track
pub const TrackInfo = extern struct {
    index: usize,
    codec: [*c]const u8,
    // language: [*c]const u8,
    // title: [*c]const u8,
};

// Interopable TrackInfo array
pub const TrackInfoArray = extern struct {
    ptr: [*c]TrackInfo,
    len: usize,
};

/// Indexing progress callback
pub const ProgressCallback = ?*const fn (
    current: i64,
    total: i64,
    userdata: ?*anyopaque,
) callconv(.c) c_int;
